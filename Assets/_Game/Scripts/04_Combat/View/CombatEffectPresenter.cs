using UnityEngine;
using DG.Tweening;
using TowerBreakers.Core.Events;
using TowerBreakers.Effects;
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
        #endregion

        #region 내부 필드
        private IEventBus m_eventBus;
        private EffectManager m_effectManager;
        private CancellationTokenSource m_hitStopCts;
        #endregion

        #region 초기화
        /// <summary>
        /// [설명]: 의존성을 주입받아 이벤트를 구독하고 카메라를 설정합니다.
        /// </summary>
        [Inject]
        public void Initialize(IEventBus eventBus, EffectManager effectManager)
        {
            m_eventBus = eventBus;
            m_effectManager = effectManager;
            m_eventBus.Subscribe<OnHitEffectRequested>(OnHitEffectRequested);
        }
        #endregion

        #region 비즈니스 로직
        /// <summary>
        /// [설명]: 타격 연출 이벤트를 수신했을 때 실행됩니다.
        /// </summary>
        private void OnHitEffectRequested(OnHitEffectRequested evt)
        {
            float intensity = evt.ShakeIntensity;
            float duration = evt.ShakeDuration;
            float hitStop = evt.HitStopDuration;

            // [추가]: 이벤트 수치가 기본값(-1.0f)인 경우 EffectManager에서 중앙 설정값 획득
            if (m_effectManager != null && (intensity < 0f || duration < 0f))
            {
                if (m_effectManager.GetHitFeedbackSettings(evt.HitEffectType, out var defIntensity, out var defDuration, out var defHitStop))
                {
                    if (intensity < 0f) intensity = defIntensity;
                    if (duration < 0f) duration = defDuration;
                    if (hitStop < 0f) hitStop = defHitStop;
                }
            }

            // 1. 카메라 쉐이크
            if (m_effectManager != null && intensity > 0f)
            {
                m_effectManager.ShakeCamera(intensity, duration);
            }

            // 2. 역경직 발동
            if (hitStop > 0f)
            {
                ApplyHitStop(hitStop).Forget();
            }
 
            // 3. 시각적 타격 이펙트 재생
            if (m_effectManager != null && evt.HitEffectType != EffectType.None)
            {
                m_effectManager.PlayEffect(evt.HitEffectType, evt.Position);
            }

            // 4. 타격 사운드 재생
            m_eventBus?.Publish(new OnSoundRequested("Hit"));
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
