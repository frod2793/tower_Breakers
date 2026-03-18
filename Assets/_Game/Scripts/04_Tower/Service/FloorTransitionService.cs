using System;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Cysharp.Threading.Tasks;

namespace TowerBreakers.Tower.Service
{
    /// <summary>
    /// [기능]: 층 이동 트랜지션 서비스 (DOTween 활용)
    /// </summary>
    public class FloorTransitionService
    {
        private readonly Transform m_playerTransform;
        private readonly Transform m_cameraTransform;
        private readonly Image m_goImage;
        
        [Header("설정")]
        [Tooltip("트랜지션 재생 시간 (초)")]
        [SerializeField] private float m_transitionDuration = 2f;

        [Tooltip("대기 시간 (초)")]
        [SerializeField] private float m_waitDuration = 1f;

        [Header("플랫폼 풀")]
        [Tooltip("플랫폼 풀")]
        [SerializeField] private PlatformPool m_platformPool;

        [Tooltip("현재 플랫폼")]
        [SerializeField] private GameObject m_currentPlatform;

        [Tooltip("다음 층 플랫폼")]
        [SerializeField] private GameObject m_nextPlatform;

        [Header("위치 설정")]
        [Tooltip("플레이어 오른쪽 시작 위치 (화면 밖)")]
        [SerializeField] private float m_playerExitX = 15f;

        [Tooltip("플레이어 왼쪽 시작 위치 (화면 밖)")]
        [SerializeField] private float m_playerEnterX = -15f;

        [Tooltip("플랫폼 하강 높이")]
        [SerializeField] private float m_platformDropHeight = 5f;

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
            Image goImage, 
            float transitionDuration)
        {
            m_playerTransform = playerTransform;
            m_cameraTransform = cameraTransform;
            m_goImage = goImage;
            m_transitionDuration = transitionDuration;
        }

        public void SetPlatformPool(PlatformPool pool)
        {
            m_platformPool = pool;
            
            if (m_platformPool != null)
            {
                m_platformPool.OnPlatformActivated += OnPlatformActivated;
            }
        }

        private void OnPlatformActivated(int floorNumber)
        {
            Debug.Log($"[FloorTransitionService] 플랫폼 활성화 완료 - 층: {floorNumber}");
            OnPlatformReady?.Invoke(floorNumber);
        }

        public void SetPlayerTransform(Transform player)
        {
        }

        public void SetGoImage(Image goImage)
        {
        }

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

        public async UniTask PlayTransitionAsync()
        {
            OnTransitionStarted?.Invoke();

            Debug.Log("[FloorTransitionService] ===== 트랜지션 시작 =====");

            if (m_goImage != null)
            {
                Debug.Log("[FloorTransitionService] GO 이미지 애니메이션 재생");
                await PlayGoImageAnimationAsync();
            }

            Debug.Log("[FloorTransitionService] 사용자 입력 대기 중... (클릭하세요)");
            await ClickToProceedAsync();
            Debug.Log("[FloorTransitionService] 클릭 감지 - 계속 진행");

            Debug.Log("[FloorTransitionService] 플레이어 퇴장 애니메이션");
            await PlayerExitAnimationAsync();

            Debug.Log("[FloorTransitionService] 플랫폼 하강 애니메이션");
            await PlatformDropAnimationAsync();

            Debug.Log("[FloorTransitionService] 플레이어 입장 애니메이션");
            await PlayerEnterAnimationAsync();

            OnTransitionComplete?.Invoke();

            if (m_platformPool != null)
            {
                if (m_currentPlatform != null)
                {
                    m_platformPool.ReturnPlatform(m_currentPlatform);
                }

                if (m_nextPlatform != null)
                {
                    m_platformPool.ReturnPlatform(m_nextPlatform);
                }

                Debug.Log($"[FloorTransitionService] 플랫폼 활성화 - 현재: {m_currentFloorNumber}, 다음: {m_nextFloorNumber}");
                m_currentPlatform = m_platformPool.GetPlatform(m_currentFloorNumber);
                m_nextPlatform = m_platformPool.GetPlatform(m_nextFloorNumber);
            }

            Debug.Log($"[FloorTransitionService] 플랫폼 활성화 알림 - 층: {m_currentFloorNumber}");
            OnPlatformReady?.Invoke(m_currentFloorNumber);

            Debug.Log("[FloorTransitionService] ===== 트랜지션 완료 =====");
        }

        public async UniTask ActivateFirstFloorPlatformAsync()
        {
            OnTransitionStarted?.Invoke();

            Debug.Log("[FloorTransitionService] ===== 첫 번째 층 플랫폼 활성화 =====");

            if (m_goImage != null)
            {
                await PlayGoImageAnimationAsync();
            }

            OnTransitionComplete?.Invoke();

            if (m_platformPool != null)
            {
                Debug.Log($"[FloorTransitionService] 플랫폼 활성화 - 현재: {m_currentFloorNumber}");
                m_currentPlatform = m_platformPool.GetPlatform(m_currentFloorNumber);
                m_nextPlatform = m_platformPool.GetPlatform(m_currentFloorNumber + 1);
            }

            Debug.Log("[FloorTransitionService] ===== 첫 번째 층 플랫폼 활성화 완료 =====");
        }

        private async UniTask PlayGoImageAnimationAsync()
        {
            if (m_goImage == null)
            {
                return;
            }

            m_goImage.gameObject.SetActive(true);

            var canvasGroup = m_goImage.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = m_goImage.gameObject.AddComponent<CanvasGroup>();
            }

            canvasGroup.alpha = 0;

            await canvasGroup.DOFade(1f, 0.3f).SetEase(Ease.OutQuad).ToUniTask();

            await UniTask.Delay((int)(m_waitDuration * 1000));

            await canvasGroup.DOFade(0f, 0.3f).SetEase(Ease.InQuad).ToUniTask();

            m_goImage.gameObject.SetActive(false);
        }

        private async UniTask ClickToProceedAsync()
        {
#if ENABLE_INPUT_SYSTEM
            await UniTask.WaitUntil(() => UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame);
#else
            await UniTask.WaitUntil(() => Input.GetMouseButtonDown(0));
#endif
            Debug.Log("[FloorTransitionService] 클릭 감지");
        }

        private async UniTask PlayerExitAnimationAsync()
        {
            if (m_playerTransform == null)
            {
                return;
            }

            var originalPosition = m_playerTransform.position;

            await m_playerTransform.DOMoveX(m_playerExitX, m_transitionDuration)
                .SetEase(Ease.InQuad)
                .ToUniTask();

            m_playerTransform.position = new Vector3(m_playerEnterX, originalPosition.y, originalPosition.z);
        }

        private async UniTask PlatformDropAnimationAsync()
        {
            if (m_currentPlatform == null)
            {
                return;
            }

            var platformTransform = m_currentPlatform.transform;
            var originalPosition = platformTransform.position;

            await platformTransform.DOMoveY(originalPosition.y - m_platformDropHeight, m_transitionDuration * 0.5f)
                .SetEase(Ease.OutQuad)
                .ToUniTask();

            await platformTransform.DOMoveY(originalPosition.y, m_transitionDuration * 0.5f)
                .SetEase(Ease.InQuad)
                .ToUniTask();
        }

        private async UniTask PlayerEnterAnimationAsync()
        {
            if (m_playerTransform == null)
            {
                return;
            }

            var targetPosition = new Vector3(0f, m_playerTransform.position.y, m_playerTransform.position.z);

            await m_playerTransform.DOMoveX(targetPosition.x, m_transitionDuration)
                .SetEase(Ease.OutQuad)
                .ToUniTask();
        }

        public void SkipTransition()
        {
            DOTween.KillAll();

            if (m_goImage != null)
            {
                m_goImage.gameObject.SetActive(false);
            }

            OnTransitionComplete?.Invoke();
        }
    }
}
