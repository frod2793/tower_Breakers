using UnityEngine;
using DG.Tweening;
using TowerBreakers.Core.Events;
using VContainer;
using Cysharp.Threading.Tasks;
using System.Threading;

namespace TowerBreakers.Combat.View
{
    /// <summary>
    /// [설명]: 타격 연출(카메라 쉐이크, 역경직)을 담당하는 클래스입니다.
    /// EventBus를 통해 타격 이벤트를 수신하여 실행합니다.
    /// </summary>
    public class CombatEffectPresenter : MonoBehaviour
    {
        #region 에디터 설정
        [SerializeField, Tooltip("메인 카메라 참조 (쉐이크 연출용)")]
        private Camera m_mainCamera;
        #endregion

        #region 내부 필드
        private IEventBus m_eventBus;
        private Tweener m_shakeTweener;
        private Vector3 m_originalCameraPos;
        private CancellationTokenSource m_hitStopCts;
        private TweenCallback m_onShakeComplete; // [최적화]: 람다 할당 방지용 캐싱
        #endregion

        #region 초기화
        /// <summary>
        /// [설명]: 의존성을 주입받아 이벤트를 구독하고 카메라를 설정합니다.
        /// </summary>
        [Inject]
        public void Initialize(IEventBus eventBus)
        {
            m_eventBus = eventBus;
            m_eventBus.Subscribe<OnHitEffectRequested>(OnHitEffectRequested);

            if (m_mainCamera == null)
            {
                m_mainCamera = Camera.main;
            }

            if (m_mainCamera != null)
            {
                m_originalCameraPos = m_mainCamera.transform.localPosition;
            }

            // [최적화]: 콜백 미리 캐싱
            m_onShakeComplete = ResetCameraPosition;
        }
        #endregion

        #region 비즈니스 로직
        /// <summary>
        /// [설명]: 타격 연출 이벤트를 수신했을 때 실행됩니다.
        /// </summary>
        private void OnHitEffectRequested(OnHitEffectRequested evt)
        {
            ApplyCameraShake(evt.ShakeIntensity, evt.ShakeDuration);
            ApplyHitStop(evt.HitStopDuration).Forget();
        }

        /// <summary>
        /// [설명]: 카메라 쉐이크 연출을 적용합니다.
        /// </summary>
        private void ApplyCameraShake(float intensity, float duration)
        {
            if (m_mainCamera == null) return;

            // 기존 쉐이크 중지 후 초기화
            if (m_shakeTweener != null && m_shakeTweener.IsActive())
            {
                m_shakeTweener.Kill(true);
            }
            m_mainCamera.transform.localPosition = m_originalCameraPos;

            // [최적화]: OnComplete에 캐싱된 메서드 전달 (람다 할당 제거)
            m_shakeTweener = m_mainCamera.transform.DOShakePosition(duration, intensity, 10, 90, false, true)
                .OnComplete(m_onShakeComplete);
        }

        private void ResetCameraPosition()
        {
            if (m_mainCamera != null)
            {
                m_mainCamera.transform.localPosition = m_originalCameraPos;
            }
        }

        /// <summary>
        /// [설명]: 역경직(Hit Stop) 연출을 적용합니다. 
        /// 연타 시 발생하는 부하를 방지하기 위해 쓰로틀링 및 CTS 관리를 최적화합니다.
        /// </summary>
        private async UniTaskVoid ApplyHitStop(float duration)
        {
            if (duration <= 0) return;

            // [최적화]: 역경직이 이미 진행 중이면 새 요청 무시 (쓰로틀링)
            if (Time.timeScale < 1.0f && m_hitStopCts != null) return;

            // [최적화]: CTS 재사용 패턴 적용
            if (m_hitStopCts == null)
            {
                m_hitStopCts = new CancellationTokenSource();
            }
            else
            {
                m_hitStopCts.Cancel();
                m_hitStopCts.Dispose();
                m_hitStopCts = new CancellationTokenSource();
            }

            Time.timeScale = 0.05f;
            try
            {
                await UniTask.Delay((int)(duration * 1000), ignoreTimeScale: true, cancellationToken: m_hitStopCts.Token);
            }
            catch (System.OperationCanceledException)
            {
                return;
            }
            finally
            {
                if (m_hitStopCts != null && !m_hitStopCts.IsCancellationRequested)
                {
                    Time.timeScale = 1.0f;
                }
            }
        }
        #endregion

        #region 유니티 생명주기
        private void OnDestroy()
        {
            if (m_eventBus != null)
            {
                m_eventBus.Unsubscribe<OnHitEffectRequested>(OnHitEffectRequested);
            }

            if (m_shakeTweener != null)
            {
                m_shakeTweener.Kill();
            }

            // Hit Stop CTS 정리
            m_hitStopCts?.Cancel();
            m_hitStopCts?.Dispose();
            m_hitStopCts = null;

            // 타임 스케일 정규화 (파괴 시 안전장치)
            if (Time.timeScale < 1.0f)
            {
                Time.timeScale = 1.0f;
            }
        }
        #endregion
    }
}
