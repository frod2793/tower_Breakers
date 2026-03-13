using Cysharp.Threading.Tasks;
using TowerBreakers.Core.Events;
using UnityEngine;
using TowerBreakers.Enemy.Logic;
using TowerBreakers.Tower.Logic;
using TowerBreakers.Environment.Logic;
using TowerBreakers.Enemy.Data;
using DG.Tweening;


namespace TowerBreakers.Core.GameState
{
    /// <summary>
    /// [설명]: 게임 플레이 중인 상태입니다.
    /// </summary>
    public class PlayingState : IGameState
    {
        private readonly EnemySpawner m_enemySpawner;
        private readonly TowerManager m_towerManager;
        private readonly EnvironmentManager m_envManager;
        private readonly IEventBus m_eventBus;
        private readonly Player.View.PlayerView m_playerView;
        private readonly Player.Data.PlayerModel m_playerModel;
        private readonly Player.Logic.PlayerPushReceiver m_playerReceiver;
        private System.Threading.CancellationTokenSource m_cts;
        private bool m_isWaitingForNextTouch = false;

        public PlayingState(
            EnemySpawner enemySpawner, 
            TowerManager towerManager, 
            EnvironmentManager envManager, 
            IEventBus eventBus,
            Player.View.PlayerView playerView,
            Player.Data.PlayerModel playerModel,
            Player.Logic.PlayerPushReceiver playerReceiver)
        {
            m_enemySpawner = enemySpawner;
            m_towerManager = towerManager;
            m_envManager = envManager;
            m_eventBus = eventBus;
            m_playerView = playerView;
            m_playerModel = playerModel;
            m_playerReceiver = playerReceiver;
        }

        public async UniTask OnEnter()
        {
            Debug.Log("[PlayingState] 진입: 게임 시작");
            
            m_cts = new System.Threading.CancellationTokenSource();
            m_eventBus.Subscribe<OnFloorReadyForNext>(HandleFloorReadyForNext);

            // 0. 초기 층 적 선스폰
            int currentFloor = m_towerManager.CurrentFloorIndex;
            await PrepareNextFloor(currentFloor);

            // 1. 초기 층 등장 연출 실행 (게임 시작 시에는 퇴장 과정 없이 바로 위에서 내려오듯 등장)
            await PlayPlayerEnterAsync(currentFloor, m_cts.Token);

            // 2. 적 진격 트리거 (초기 층)
            m_eventBus.Publish(new OnFloorStarted(currentFloor));
            
            // 3. 다음 층 미리 스폰
            await PrepareNextFloor(currentFloor + 1);
        }


        public UniTask OnExit()
        {
            Debug.Log("[PlayingState] 퇴출: 구독 해제 및 스폰 중단");
            m_eventBus.Unsubscribe<OnFloorReadyForNext>(HandleFloorReadyForNext);
            m_enemySpawner.StopSpawning();
            return UniTask.CompletedTask;
        }

        private void HandleFloorReadyForNext(OnFloorReadyForNext evt)
        {
            Debug.Log("[PlayingState] 다음 층 이동 대기 중... (터치 입력 대기)");
            m_isWaitingForNextTouch = true;
        }

        /// <summary>
        /// [설명]: 다음 층으로 넘어가는 5단계 연출 시퀀스를 통합 관리합니다.
        /// </summary>
        private async UniTask PerformTransitionSequenceAsync(System.Threading.CancellationToken ct)
        {
            m_isWaitingForNextTouch = false;
            int nextFloorIndex = m_towerManager.CurrentFloorIndex + 1;

            // ③ 플레이어 오른쪽 퇴장 연출 (이동 애니메이션과 함께 화면 밖으로)
            await PlayPlayerExitAsync(ct);

            // ④-a 지면 하강 트리거 (TowerManager를 통해 이벤트 발행)
            m_towerManager.NextFloor(); 

            // [추가]: 지면 하강 연출(0.5s) 완료를 기다림
            await UniTask.Delay(500, cancellationToken: ct);

            // [수정]: 지면 하강이 완료된 후, 플레이어를 다음 층의 시작 지점(화면 왼쪽 밖)으로 옮김
            // 이전에는 지연 시간(500ms) 전에 옮겨서 플레이어가 공중에 떠 있는 것처럼 보였음
            if (m_playerReceiver != null)
            {
                Vector2 nextSpawnPos = m_envManager.GetPlayerSpawnPosition(nextFloorIndex);
                
                // 대시 시작점은 벽 밖이므로 Clamping을 잠시 끔
                m_playerReceiver.IsClampingEnabled = false;
                
                m_playerView.transform.position = new Vector3(nextSpawnPos.x, nextSpawnPos.y, 0f);
                m_playerModel.Position = (Vector2)m_playerView.transform.position;
                Debug.Log($"[PlayingState] 지면 하강 완료 후 위치 동기화: {nextSpawnPos}");
            }

            // ④-b 플레이어 왼쪽 등장 연출 (도약하듯 들어와서 안착)
            await PlayPlayerEnterAsync(nextFloorIndex, ct);

            // ⑤ 1초 대기 후 적 진격 시작
            Debug.Log("[PlayingState] ⑤ 1초 대기 후 적 진격 시작");
            await UniTask.Delay(1000, cancellationToken: ct);
            
            // 선스폰된 적 활성화
            m_eventBus.Publish(new OnFloorStarted(nextFloorIndex));

            // 다음 위층 미리 준비 (선스폰)
            await PrepareNextFloor(nextFloorIndex + 1);
        }

        /// <summary>
        /// [설명]: 플레이어가 오른쪽 화면 밖으로 나가는 연출을 수행합니다.
        /// </summary>
        private async UniTask PlayPlayerExitAsync(System.Threading.CancellationToken ct)
        {
            if (m_playerView == null) return;

            Debug.Log("[PlayingState] ③ 플레이어 퇴장 연출 시작 (오른쪽으로 이동)");
            m_playerView.PlayAnimation(global::PlayerState.MOVE, 0);

            // 화면 오른쪽 밖(+12f)으로 이동
            await m_playerView.transform.DOMoveX(12f, 0.6f)
                .SetEase(Ease.InQuad)
                .OnUpdate(() => m_playerModel.Position = (Vector2)m_playerView.transform.position)
                .ToUniTask(cancellationToken: ct);
            
            Debug.Log("[PlayingState] ③ 플레이어 퇴장 완료");
        }

        /// <summary>
        /// [설명]: 플레이어가 왼쪽 화면 밖에서 나타나 안착하는 연출을 수행합니다. (대시 연출로 수정)
        /// </summary>
        private async UniTask PlayPlayerEnterAsync(int floorIndex, System.Threading.CancellationToken ct)
        {
            if (m_playerView == null) return;

            Debug.Log($"[PlayingState] ④ 플레이어 등장 연출 시작 (대시): 층 {floorIndex}");
            
            Vector2 landingPos = m_envManager.GetPlayerLandingPosition(floorIndex);
            Vector2 spawnPos = m_envManager.GetPlayerSpawnPosition(floorIndex);
            
            // [수정]: 연출 시작 전 확실하게 스폰 위치로 강제 이동 (잔상 및 간섭 방지)
            m_playerView.transform.DOKill();
            m_playerView.transform.position = new Vector3(spawnPos.x, spawnPos.y, 0f);
            m_playerModel.Position = (Vector2)m_playerView.transform.position;

            // [변경]: MOVE 애니메이션의 1번 인덱스(Run/Dash) 사용 시도
            m_playerView.PlayAnimation(global::PlayerState.MOVE, 1);

            // [변경]: 대시 느낌을 살리기 위해 Ease 타입을 OutQuad로 변경하고 신속하게 이동 (0.4s)
            // [수정]: 시작점과 도착점의 Y축 미세 오차까지 완벽히 보정하기 위해 DOMove 사용
            await m_playerView.transform.DOMove(landingPos, 0.4f)
                .SetEase(Ease.OutQuad)
                .OnUpdate(() => m_playerModel.Position = (Vector2)m_playerView.transform.position)
                .ToUniTask(cancellationToken: ct);

            // [추가]: 안착 완료 후 위치 보정(Clamping) 재활성화
            if (m_playerReceiver != null)
            {
                m_playerReceiver.IsClampingEnabled = true;
                // 현재 위치에서 한 번 더 강제 클램프 수행 (안착 지점이 벽 밖일 경우 대비)
                m_playerModel.Position = (Vector2)m_playerView.transform.position;
            }

            m_playerView.PlayAnimation(global::PlayerState.IDLE, 0);
            Debug.Log("[PlayingState] ④ 플레이어 등장(대시) 완료");
        }

        private async UniTask PrepareNextFloor(int nextFloorIndex)
        {
            var floors = m_towerManager.GetFloorsList();
            if (nextFloorIndex >= floors.Count) return;

            Debug.Log($"[PlayingState] 위층({nextFloorIndex}) 적 선스폰 시작");
            var floorData = floors[nextFloorIndex];
            Vector2 spawnPos = m_envManager.GetSpawnPosition(nextFloorIndex);
            
            // 대기 상태(waiting)로 미리 생성
            await m_enemySpawner.SpawnFloorEnemiesAsync(floorData, spawnPos, nextFloorIndex, true);
        }

        public void OnUpdate()
        {
            // 'GO' 상태일 때 화면 터치(마우스 클릭) 감지
            if (m_isWaitingForNextTouch && 
                UnityEngine.InputSystem.Pointer.current != null && 
                UnityEngine.InputSystem.Pointer.current.press.wasPressedThisFrame)
            {
                m_isWaitingForNextTouch = false;
                Debug.Log("[PlayingState] 터치 감지! 5단계 상승 시퀀스 시작");
                PerformTransitionSequenceAsync(m_cts.Token).Forget();
            }
        }
    }
}
