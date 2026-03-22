using System;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Cysharp.Threading.Tasks;
using TowerBreakers.UI.ViewModel;

namespace TowerBreakers.Tower.Service
{
    /// <summary>
    /// [설명]: 층 이동 트랜지션 연출 및 플랫폼 관리를 담당하는 서비스 클래스입니다.
    /// DOTween을 활용하여 플랫폼 하강 및 플레이어 대시 연출을 수행합니다.
    /// </summary>
    public class FloorTransitionService
    {
        #region 내부 필드
        private readonly Transform m_playerTransform;
        private readonly Transform m_cameraTransform;
        private readonly BattleUIViewModel m_uiViewModel;
        private readonly PlayerSpawnService m_playerSpawnService;
        private readonly Transform m_exitTransform;
        
        private float m_transitionDuration = 2f;
        private PlatformPool m_platformPool;
        private GameObject m_currentPlatform;
        private GameObject m_nextPlatform;
        private GameObject m_thirdPlatform;
        private float m_platformDropHeight = 10f;

        private int m_currentFloorNumber = 1;
        #endregion

        #region 프로퍼티
        public event Action OnTransitionComplete;
        public event Action OnTransitionStarted;
        public event Action<int> OnPlatformReady;

        /// <summary>
        /// [설명]: 트랜지션 연출 지속 시간입니다.
        /// </summary>
        public float TransitionDuration
        {
            get => m_transitionDuration;
            set => m_transitionDuration = value;
        }
        #endregion

        #region 초기화 로직
        /// <summary>
        /// [설명]: 서비스에 필요한 의존성을 주입받아 초기화합니다.
        /// </summary>
        public FloorTransitionService(
            Transform playerTransform, 
            Transform cameraTransform, 
            BattleUIViewModel uiViewModel,
            PlayerSpawnService playerSpawnService,
            Transform exitTransform,
            float transitionDuration)
        {
            m_playerTransform = playerTransform;
            m_cameraTransform = cameraTransform;
            m_uiViewModel = uiViewModel;
            m_playerSpawnService = playerSpawnService;
            m_exitTransform = exitTransform;
            m_transitionDuration = transitionDuration;
        }

        /// <summary>
        /// [설명]: 플랫폼 생성을 위한 풀을 설정하고 연출 파라미터를 동기화합니다.
        /// </summary>
        /// <param name="pool">플랫폼 오브젝트 풀</param>
        public void SetPlatformPool(PlatformPool pool)
        {
            m_platformPool = pool;
            
            if (m_platformPool != null)
            {
                m_platformDropHeight = m_platformPool.FloorSpacing;
            }
        }
        #endregion

        #region 공개 메서드
        /// <summary>
        /// [설명]: 현재 활성화된 플랫폼의 번호를 설정합니다.
        /// </summary>
        /// <param name="floorNumber">현재 층 번호</param>
        public void SetCurrentPlatform(int floorNumber)
        {
            m_currentFloorNumber = floorNumber;
        }

        /// <summary>
        /// [설명]: 현재 층 플랫폼의 트랜스폼을 반환합니다.
        /// </summary>
        public Transform GetCurrentPlatformTransform()
        {
            if (m_currentPlatform == null) return null;
            return m_currentPlatform.transform;
        }

        /// <summary>
        /// [설명]: 다음 층 플랫폼의 트랜스폼을 반환합니다.
        /// </summary>
        public Transform GetNextPlatformTransform()
        {
            if (m_nextPlatform == null) return null;
            return m_nextPlatform.transform;
        }

        /// <summary>
        /// [설명]: 다다음 층 플랫폼의 트랜스폼을 반환합니다.
        /// </summary>
        public Transform GetThirdPlatformTransform()
        {
            if (m_thirdPlatform == null) return null;
            return m_thirdPlatform.transform;
        }

        /// <summary>
        /// [설명]: 층 이동 트랜지션 시퀀스를 비동기로 실행합니다.
        /// </summary>
        public async UniTask PlayTransitionAsync()
        {
            OnTransitionStarted?.Invoke();

            // 1. UI 연출 및 클릭 대기
            if (m_uiViewModel != null)
            {
                m_uiViewModel.SetGoState(true);
            }

            // [추가]: 트랜지션 시작 시 플레이어 부모 해제 (Root로 복귀)
            if (m_playerTransform != null)
            {
                m_playerTransform.SetParent(null);
                m_playerTransform.SetParent(null);
            }

            await WaitForClickAsync();
            
            if (m_uiViewModel != null)
            {
                m_uiViewModel.SetGoState(false);
            }

            // 2. 플레이어 퇴장 연출
            await DashExitAsync();

            // 3. 지면 하강 및 플랫폼 교체
            await PlatformDropAnimationAsync();
            SwapPlatforms();

            // 4. 플레이어 입장 연출
            if (m_playerSpawnService != null)
            {
                await m_playerSpawnService.PlaySpawnAnimationAsync();
                
                // [핵심 추가]: 플레이어 입장 완료 후 현재 플랫폼의 자식으로 설정 (계층 구조 통합)
                if (m_playerTransform != null && m_currentPlatform != null)
                {
                    m_playerTransform.SetParent(m_currentPlatform.transform);
                }
            }

            OnTransitionComplete?.Invoke();
            OnPlatformReady?.Invoke(m_currentFloorNumber);
        }

        /// <summary>
        /// [설명]: 게임 시작 시 첫 번째 층의 플랫폼들을 미리 생성하고 배치합니다.
        /// </summary>
        public async UniTask ActivateFirstFloorPlatformAsync()
        {
            OnTransitionStarted?.Invoke();

            if (m_platformPool != null)
            {
                m_currentPlatform = m_platformPool.GetPlatform(m_currentFloorNumber);
                m_nextPlatform = m_platformPool.GetPlatform(m_currentFloorNumber + 1);
                m_thirdPlatform = m_platformPool.GetPlatform(m_currentFloorNumber + 2);

                // [추가]: 초기 씬 로드 시에도 플레이어를 첫 번째 플랫폼의 자식으로 설정
                if (m_playerTransform != null && m_currentPlatform != null)
                {
                    m_playerTransform.SetParent(m_currentPlatform.transform);
                }
            }

            OnTransitionComplete?.Invoke();
            OnPlatformReady?.Invoke(m_currentFloorNumber);
        }
        #endregion

        #region 내부 로직
        /// <summary>
        /// [설명]: 화면 클릭이 발생할 때까지 대기합니다.
        /// </summary>
        private async UniTask WaitForClickAsync()
        {
            if (m_uiViewModel == null) return;

            var tcs = new UniTaskCompletionSource();
            Action onClick = () => 
            {
                tcs.TrySetResult();
            };
            
            m_uiViewModel.OnScreenClicked += onClick;

            try
            {
                await tcs.Task;
            }
            finally
            {
                m_uiViewModel.OnScreenClicked -= onClick;
            }
        }

        /// <summary>
        /// [설명]: 플레이어를 화면 밖(퇴장 지점)으로 이동시키고 비활성화합니다.
        /// </summary>
        private async UniTask DashExitAsync()
        {
            if (m_playerTransform == null) return;

            Vector3 targetPos = m_exitTransform != null 
                ? m_exitTransform.position 
                : m_playerTransform.position + Vector3.right * 15f;
            
            await m_playerTransform.DOMove(targetPos, 0.5f).SetEase(Ease.InQuad).ToUniTask();
            
            m_playerTransform.gameObject.SetActive(false);
        }

        /// <summary>
        /// [설명]: 플랫폼들의 참조를 한 단계씩 당기고 새로운 플랫폼을 보충합니다.
        /// </summary>
        private void SwapPlatforms()
        {
            if (m_platformPool == null) return;

            if (m_currentPlatform != null)
            {
                m_platformPool.ReturnPlatform(m_currentPlatform);
            }
            
            m_currentPlatform = m_nextPlatform;
            m_nextPlatform = m_thirdPlatform;
            
            // 새 플랫폼 획득 및 좌표 보정 (누적 오차 방지)
            m_thirdPlatform = m_platformPool.GetPlatform(m_currentFloorNumber + 2);
            if (m_thirdPlatform != null)
            {
                float targetY = 2 * m_platformPool.FloorSpacing;
                var pos = m_thirdPlatform.transform.position;
                m_thirdPlatform.transform.position = new Vector3(pos.x, targetY, pos.z);
            }
        }

        /// <summary>
        /// [설명]: 현재 활성화된 모든 플랫폼을 동시에 아래로 이동시킵니다.
        /// </summary>
        private async UniTask PlatformDropAnimationAsync()
        {
            var dropTasks = new System.Collections.Generic.List<UniTask>();
            
            if (m_currentPlatform != null)
                dropTasks.Add(m_currentPlatform.transform.DOMoveY(m_currentPlatform.transform.position.y - m_platformDropHeight, 0.8f).SetEase(Ease.InCubic).ToUniTask());

            if (m_nextPlatform != null)
                dropTasks.Add(m_nextPlatform.transform.DOMoveY(m_nextPlatform.transform.position.y - m_platformDropHeight, 0.8f).SetEase(Ease.InCubic).ToUniTask());

            if (m_thirdPlatform != null)
                dropTasks.Add(m_thirdPlatform.transform.DOMoveY(m_thirdPlatform.transform.position.y - m_platformDropHeight, 0.8f).SetEase(Ease.InCubic).ToUniTask());

            await UniTask.WhenAll(dropTasks);
        }
        #endregion
    }
}
