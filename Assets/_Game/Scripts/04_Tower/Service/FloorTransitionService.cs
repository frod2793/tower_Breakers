using System;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Cysharp.Threading.Tasks;
using TowerBreakers.UI.ViewModel;

namespace TowerBreakers.Tower.Service
{
    /// <summary>
    /// [기능]: 층 이동 트랜지션 서비스 (DOTween 활용)
    /// </summary>
    public class FloorTransitionService
    {
        private readonly Transform m_playerTransform;
        private readonly Transform m_cameraTransform;
        private readonly BattleUIViewModel m_uiViewModel;
        private readonly PlayerSpawnService m_playerSpawnService;
        
        [Header("설정")]
        private float m_transitionDuration = 2f;
        private float m_waitDuration = 1f;
        private PlatformPool m_platformPool;
        private GameObject m_currentPlatform;
        private GameObject m_nextPlatform;
        private GameObject m_thirdPlatform;

        [Header("위치 설정")]
        private float m_platformDropHeight = 10f;

        public event Action OnTransitionComplete;
        public event Action OnTransitionStarted;
        public event Action<int> OnPlatformReady;

        public float TransitionDuration
        {
            get => m_transitionDuration;
            set => m_transitionDuration = value;
        }

        public FloorTransitionService(
            Transform playerTransform, 
            Transform cameraTransform, 
            BattleUIViewModel uiViewModel,
            PlayerSpawnService playerSpawnService,
            float transitionDuration)
        {
            m_playerTransform = playerTransform;
            m_cameraTransform = cameraTransform;
            m_uiViewModel = uiViewModel;
            m_playerSpawnService = playerSpawnService;
            m_transitionDuration = transitionDuration;
        }

        public void SetPlatformPool(PlatformPool pool)
        {
            m_platformPool = pool;
            
            if (m_platformPool != null)
            {
                // [수정]: Pool 내부의 자동 이벤트 대신 서비스에서 명시적으로 상태를 제어함
            }
        }

        // [삭제]: Pool 이벤트를 직접 구독하는 대신 필요한 시점에 수동으로 호출함

        private int m_currentFloorNumber = 1;
        private int m_nextFloorNumber = 2;

        public void SetCurrentPlatform(int floorNumber)
        {
            m_currentFloorNumber = floorNumber;
            m_nextFloorNumber = floorNumber + 1;
            
            Debug.Log($"[FloorTransitionService] 플랫폼 번호 설정 - 현재: {floorNumber}, 다음: {floorNumber + 1}");
        }

        public Transform GetCurrentPlatformTransform()
        {
            return m_currentPlatform != null ? m_currentPlatform.transform : null;
        }

        public Transform GetNextPlatformTransform()
        {
            return m_nextPlatform != null ? m_nextPlatform.transform : null;
        }

        public Transform GetThirdPlatformTransform()
        {
            return m_thirdPlatform != null ? m_thirdPlatform.transform : null;
        }

        public async UniTask PlayTransitionAsync()
        {
            OnTransitionStarted?.Invoke();

            Debug.Log("[FloorTransitionService] ===== 트랜지션 시작 =====");

            // 1. GO 이미지 점멸 및 버튼 비활성화
            if (m_uiViewModel != null)
            {
                m_uiViewModel.SetGoState(true);
            }

            // 2. 사용자 클릭 대기
            Debug.Log("[FloorTransitionService] 사용자 클릭 대기 중...");
            await WaitForClickAsync();
            
            if (m_uiViewModel != null)
            {
                m_uiViewModel.SetGoState(false); // GO 이미지 끔
            }

            // 3. 플레이어 오른쪽으로 대시 퇴장
            Debug.Log("[FloorTransitionService] 플레이어 퇴장 (오른쪽 대시)");
            if (m_playerSpawnService != null)
            {
                // DashExitAsync 구현이 필요함 (가칭)
                await DashExitAsync();
            }

            // 4. 지면 하강 연출
            Debug.Log("[FloorTransitionService] 플랫폼 하강 애니메이션");
            await PlatformDropAnimationAsync();

            // 5. 플랫폼 교체
            SwapPlatforms();

            // 6. 플레이어 왼쪽 스폰 지점에서 대시 등장
            Debug.Log("[FloorTransitionService] 플레이어 입장 (왼쪽 대시)");
            if (m_playerSpawnService != null)
            {
                await m_playerSpawnService.PlaySpawnAnimationAsync();
            }

            OnTransitionComplete?.Invoke();
            
            Debug.Log($"[FloorTransitionService] 플랫폼 활성화 알림 - 층: {m_currentFloorNumber}");
            OnPlatformReady?.Invoke(m_currentFloorNumber);

            Debug.Log("[FloorTransitionService] ===== 트랜지션 완료 =====");
        }

        public async UniTask ActivateFirstFloorPlatformAsync()
        {
            OnTransitionStarted?.Invoke();
            Debug.Log("[FloorTransitionService] ===== 첫 번째 층 플랫폼 활성화 =====");

            if (m_platformPool != null)
            {
            m_currentPlatform = m_platformPool.GetPlatform(m_currentFloorNumber);
                m_nextPlatform = m_platformPool.GetPlatform(m_currentFloorNumber + 1);
                m_thirdPlatform = m_platformPool.GetPlatform(m_currentFloorNumber + 2); // [추가]: n+2 층
            }

            // [추가]: 모든 플랫폼 참조 변수가 할당된 "후"에 이벤트를 발생시켜 GameController의 참조 오류 방지
            OnTransitionComplete?.Invoke();
            Debug.Log($"[FloorTransitionService] 모든 초기 플랫폼 준비 완료 알림 - 현재 층: {m_currentFloorNumber}");
            OnPlatformReady?.Invoke(m_currentFloorNumber);

            Debug.Log("[FloorTransitionService] ===== 첫 번째 층 플랫폼 활성화 완료 =====");
        }

        private async UniTask WaitForClickAsync()
        {
            bool clicked = false;
            Action onClick = () => clicked = true;
            
            if (m_uiViewModel != null)
            {
                m_uiViewModel.OnScreenClicked += onClick;
            }

            await UniTask.WaitUntil(() => clicked);

            if (m_uiViewModel != null)
            {
                m_uiViewModel.OnScreenClicked -= onClick;
            }
        }

        private async UniTask DashExitAsync()
        {
            if (m_playerTransform == null) return;

            // 오른쪽으로 멀리 대시하여 화면 밖으로 나감
            Vector3 targetPos = m_playerTransform.position + Vector3.right * 15f;
            await m_playerTransform.DOMove(targetPos, 0.5f).SetEase(Ease.InQuad).ToUniTask();
        }

        private void SwapPlatforms()
        {
            if (m_platformPool == null) return;

            if (m_currentPlatform != null) m_platformPool.ReturnPlatform(m_currentPlatform);
            
            // 한 단계씩 앞으로 당김
            m_currentPlatform = m_nextPlatform;
            m_nextPlatform = m_thirdPlatform;
            
            // 새로운 n+2 층 플랫폼 획득 (스폰 시 기본적으로 +20 위치에 생성됨)
            m_thirdPlatform = m_platformPool.GetPlatform(m_currentFloorNumber + 2);
            
            Debug.Log($"[FloorTransitionService-Diag] SwapPlatforms 완료. Current: {m_currentPlatform?.name} (Y:{m_currentPlatform?.transform.position.y}), Next: {m_nextPlatform?.name} (Y:{m_nextPlatform?.transform.position.y}), Third: {m_thirdPlatform?.name} (Y:{m_thirdPlatform?.transform.position.y})");
        }

        private async UniTask PlatformDropAnimationAsync()
        {
            var dropTasks = new System.Collections.Generic.List<UniTask>();
            
            Debug.Log($"[FloorTransitionService-Diag] PlatformDropAnimation 시작. DropHeight: {m_platformDropHeight}");

            // 현재 플랫폼 하강 (화면 밖으로 사라짐)
            if (m_currentPlatform != null)
            {
                var pt = m_currentPlatform.transform;
                Debug.Log($"[FloorTransitionService-Diag] Current Platform ({pt.name}) Drop: {pt.position.y} -> {pt.position.y - m_platformDropHeight}");
                dropTasks.Add(pt.DOMoveY(pt.position.y - m_platformDropHeight, 0.8f).SetEase(Ease.InCubic).ToUniTask());
            }

            // 다음 플랫폼 하강 (화면으로 들어와 Y=0 이 됨)
            if (m_nextPlatform != null)
            {
                var pt = m_nextPlatform.transform;
                Debug.Log($"[FloorTransitionService-Diag] Next Platform ({pt.name}) Drop: {pt.position.y} -> {pt.position.y - m_platformDropHeight}");
                dropTasks.Add(pt.DOMoveY(pt.position.y - m_platformDropHeight, 0.8f).SetEase(Ease.InCubic).ToUniTask());
            }

            // 다다음 플랫폼 하강 (+20 에서 +10 으로)
            if (m_thirdPlatform != null)
            {
                var pt = m_thirdPlatform.transform;
                Debug.Log($"[FloorTransitionService-Diag] Third Platform ({pt.name}) Drop: {pt.position.y} -> {pt.position.y - m_platformDropHeight}");
                dropTasks.Add(pt.DOMoveY(pt.position.y - m_platformDropHeight, 0.8f).SetEase(Ease.InCubic).ToUniTask());
            }

            await UniTask.WhenAll(dropTasks);
            
            Debug.Log("[FloorTransitionService-Diag] PlatformDropAnimation 종료.");
        }
    }
}
