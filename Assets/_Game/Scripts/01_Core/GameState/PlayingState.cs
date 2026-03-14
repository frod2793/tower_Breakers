using Cysharp.Threading.Tasks;
using TowerBreakers.Core.Events;
using UnityEngine;
using TowerBreakers.Enemy.Logic;
using TowerBreakers.Tower.Logic;
using TowerBreakers.Environment.Logic;
using TowerBreakers.Enemy.Data;
using DG.Tweening;
using TowerBreakers.Player.Data.Models;


namespace TowerBreakers.Core.GameState
{
    /// <summary>
    /// [설명]: 게임 플레이 중인 상태입니다.
    /// 타워의 각 층 진행, 플레이어 연출, 적 스폰 등을 관리합니다.
    /// </summary>
    public class PlayingState : IGameState
    {
        #region 내부 필드
        private readonly EnemySpawner m_enemySpawner;
        private readonly TowerManager m_towerManager;
        private readonly EnvironmentManager m_envManager;
        private readonly IEventBus m_eventBus;
        private readonly Player.View.PlayerView m_playerView;
        private readonly PlayerModel m_playerModel;
        private readonly Player.Logic.PlayerPushReceiver m_playerReceiver;
        
        private readonly Transform m_playerTransform;
        private System.Threading.CancellationTokenSource m_cts;
        private bool m_isWaitingForNextTouch = false;
        #endregion

        #region 초기화
        public PlayingState(
            EnemySpawner enemySpawner, 
            TowerManager towerManager, 
            EnvironmentManager envManager, 
            IEventBus eventBus,
            Player.View.PlayerView playerView,
            PlayerModel playerModel,
            Player.Logic.PlayerPushReceiver playerReceiver)
        {
            m_enemySpawner = enemySpawner;
            m_towerManager = towerManager;
            m_envManager = envManager;
            m_eventBus = eventBus;
            m_playerView = playerView;
            m_playerModel = playerModel;
            m_playerReceiver = playerReceiver;

            if (m_playerView != null)
            {
                m_playerTransform = m_playerView.transform;
            }
        }
        #endregion

        #region 생명주기
        public async UniTask OnEnter()
        {
            m_cts = new System.Threading.CancellationTokenSource();
            m_eventBus.Subscribe<OnFloorReadyForNext>(HandleFloorReadyForNext);

            int currentFloor = m_towerManager.CurrentFloorIndex;
            
            // 0. 초기 층 적 선스폰 및 등장 연출
            await PrepareNextFloor(currentFloor);
            await PlayPlayerEnterAsync(currentFloor, m_cts.Token);
            
            // [추가] 초기 층이 보스면 등장 연출 실행
            if (IsBossFloor(currentFloor))
            {
                await PlayBossIntroAsync(currentFloor, m_cts.Token);
            }

            // 1. 적 진격 트리거 및 다음 층 준비
            if (IsBossFloor(currentFloor))
            {
                var bossController = m_enemySpawner.GetBossController(currentFloor);
                if (bossController != null && bossController.BossPhaseState is TowerBreakers.Enemy.Boss.AI.FSM.BossFSM fsm)
                {
                    fsm.Resume();
                }
            }
            m_eventBus.Publish(new OnFloorStarted(currentFloor));
            await PrepareNextFloor(currentFloor + 1);

            Debug.Log($"[PlayingState] 게임 시작 (현재 층: {currentFloor})");
        }

        public UniTask OnExit()
        {
            m_eventBus.Unsubscribe<OnFloorReadyForNext>(HandleFloorReadyForNext);
            m_enemySpawner.StopSpawning();
            
            m_cts?.Cancel();
            m_cts?.Dispose();
            
            Debug.Log("[PlayingState] 게임 플레이 종료");
            return UniTask.CompletedTask;
        }

        public void OnUpdate()
        {
            if (!m_isWaitingForNextTouch) return;

            // 터치(마우스 클릭) 감지 시 다음 층 시퀀스 시작
            var pointer = UnityEngine.InputSystem.Pointer.current;
            if (pointer != null && pointer.press.wasPressedThisFrame)
            {
                m_isWaitingForNextTouch = false;
                PerformTransitionSequenceAsync(m_cts.Token).Forget();
            }
        }
        #endregion

        #region 내부 메서드
        /// <summary>
        /// [설명]: 다음 층으로 넘어가는 연출 시퀀스를 실행합니다.
        /// </summary>
        private async UniTask PerformTransitionSequenceAsync(System.Threading.CancellationToken ct)
        {
            int nextFloorIndex = m_towerManager.CurrentFloorIndex + 1;

            // [안전 검사]: 다음 층이 존재하지 않으면 전환 중단
            var floors = m_towerManager.GetFloorsList();
            if (nextFloorIndex >= floors.Count)
            {
                Debug.LogWarning($"[PlayingState] 다음 층({nextFloorIndex})이 존재하지 않습니다. 전환 중단.");
                return;
            }

            // 1. 플레이어 퇴장 및 지면 하강
            await PlayPlayerExitAsync(ct);
            m_towerManager.NextFloor(); 

            // 지면 하강 연출 대기
            await UniTask.Delay(500, cancellationToken: ct);

            // 2. 플레이어 위치 동기화 (다음 층 스폰 지점)
            if (m_playerReceiver != null && m_playerTransform != null)
            {
                Vector2 nextSpawnPos = m_envManager.GetPlayerSpawnPosition(nextFloorIndex);
                m_playerReceiver.IsClampingEnabled = false;
                m_playerTransform.position = new Vector3(nextSpawnPos.x, nextSpawnPos.y, 0f);
                m_playerModel.Position = nextSpawnPos;
            }

            // 3. 다음 층 등장 및 적 활성화
            await PlayPlayerEnterAsync(nextFloorIndex, ct);
            
            // [추가] 보스 층이면 등장 연출 실행
            if (IsBossFloor(nextFloorIndex))
            {
                await PlayBossIntroAsync(nextFloorIndex, ct);
            }

            await UniTask.Delay(1000, cancellationToken: ct);
            
            if (IsBossFloor(nextFloorIndex))
            {
                var bossController = m_enemySpawner.GetBossController(nextFloorIndex);
                if (bossController != null && bossController.BossPhaseState is TowerBreakers.Enemy.Boss.AI.FSM.BossFSM fsm)
                {
                    fsm.Resume();
                }
            }
            m_eventBus.Publish(new OnFloorStarted(nextFloorIndex));
            await PrepareNextFloor(nextFloorIndex + 1);
        }

        /// <summary>
        /// [설명]: 플레이어의 화면 밖 퇴장 연출을 수행합니다.
        /// </summary>
        private async UniTask PlayPlayerExitAsync(System.Threading.CancellationToken ct)
        {
            if (m_playerView == null || m_playerTransform == null) return;

            m_playerView.PlayAnimation(global::PlayerState.MOVE, 0);

            await m_playerTransform.DOMoveX(12f, 0.6f)
                .SetEase(Ease.InQuad)
                .OnUpdate(() => m_playerModel.Position = (Vector2)m_playerTransform.position)
                .ToUniTask(cancellationToken: ct);
        }

        /// <summary>
        /// [설명]: 플레이어의 화면 안 등장을 위한 대시 연출을 수행합니다.
        /// </summary>
        private async UniTask PlayPlayerEnterAsync(int floorIndex, System.Threading.CancellationToken ct)
        {
            if (m_playerView == null || m_playerTransform == null) return;

            Vector2 landingPos = m_envManager.GetPlayerLandingPosition(floorIndex);
            Vector2 spawnPos = m_envManager.GetPlayerSpawnPosition(floorIndex);
            
            m_playerTransform.DOKill();
            m_playerTransform.position = new Vector3(spawnPos.x, spawnPos.y, 0f);
            m_playerModel.Position = spawnPos;

            // [수정]: 애니메이션 인덱스 안전 검사 (인덱스 1이 없을 경우 0으로 폴백하여 경고 로그 방지)
            int moveAnimIndex = (m_playerView.SpumPrefabs != null && m_playerView.SpumPrefabs.MOVE_List.Count > 1) ? 1 : 0;
            m_playerView.PlayAnimation(global::PlayerState.MOVE, moveAnimIndex);

            await m_playerTransform.DOMove(landingPos, 0.4f)
                .SetEase(Ease.OutQuad)
                .OnUpdate(() => m_playerModel.Position = (Vector2)m_playerTransform.position)
                .ToUniTask(cancellationToken: ct);

            if (m_playerReceiver != null)
            {
                m_playerReceiver.IsClampingEnabled = true;
                m_playerModel.Position = (Vector2)m_playerTransform.position;
            }

            m_playerView.PlayAnimation(global::PlayerState.IDLE, 0);
        }

        /// <summary>
        /// [설명]: 다음 층에 등장할 적군을 미리 생성(POOLING)합니다.
        /// 또한 보상 테이블이 존재하면 보상 상자를 스폰합니다.
        /// </summary>
        private async UniTask PrepareNextFloor(int nextFloorIndex)
        {
            var floors = m_towerManager.GetFloorsList();
            if (nextFloorIndex >= floors.Count) return;
            
            var floorData = floors[nextFloorIndex];
            Vector2 spawnPos = m_envManager.GetSpawnPosition(nextFloorIndex);
            Transform floorParent = m_envManager.GetSegmentTransform(nextFloorIndex);
            
            // 적 스폰
            await m_enemySpawner.SpawnFloorEnemiesAsync(floorData, spawnPos, nextFloorIndex, floorParent, true);
            
            // 보상 테이블이 존재하면 보상 상자 스폰
            if (floorData.RewardTable != null)
            {
                m_envManager.SpawnRewardChest(nextFloorIndex, floorData.RewardTable);
            }
        }

        private void HandleFloorReadyForNext(OnFloorReadyForNext evt)
        {
            m_isWaitingForNextTouch = true;
        }

        private bool IsBossFloor(int floorIndex)
        {
            var floors = m_towerManager.GetFloorsList();
            if (floorIndex >= floors.Count) return false;

            var floorData = floors[floorIndex];
            // FloorData의 EnemyPrefabData 또는 SpawnPackets에서 Boss 타입 확인
            if (floorData.EnemyPrefabData != null && floorData.EnemyPrefabData.Type == EnemyType.Boss)
            {
                return true;
            }
            if (floorData.SpawnPackets != null)
            {
                foreach (var packet in floorData.SpawnPackets)
                {
                    if (packet.EnemyPrefabData != null && packet.EnemyPrefabData.Type == EnemyType.Boss)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private async UniTask PlayBossIntroAsync(int floorIndex, System.Threading.CancellationToken ct)
        {
            // 해당 층에 스폰된 보스에서 BossIntroCutscene 컴포넌트 탐색
            var bossIntro = m_enemySpawner.GetBossIntroCutscene(floorIndex);
            if (bossIntro == null)
            {
                Debug.Log($"[PlayingState] 보스 등장연출 없음 (층: {floorIndex})");
                return;
            }

            // EventBus 주입 (DI 미적용 컴포넌트이므로)
            bossIntro.SetEventBus(m_eventBus);

            Debug.Log($"[PlayingState] 보스 등장연출 시작 (층: {floorIndex})");

            // 등장연출 실행 및 완료 대기
            var tcs = new UniTaskCompletionSource();
            bossIntro.PlayIntroAsync(() => tcs.TrySetResult()).Forget();
            await tcs.Task;

            Debug.Log($"[PlayingState] 보스 등장연출 완료 (층: {floorIndex})");
        }
        #endregion
    }
}
