using UnityEngine;
using UnityEngine.Playables;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using System.Threading;
using TowerBreakers.Core.Events;

namespace TowerBreakers.Enemy.Boss.View
{
    /// <summary>
    /// [설명]: 보스 등장연출을 관리하는 클래스입니다.
    /// DOTween 애니메이션과 Timeline (PlayableDirector)을 모두 지원합니다.
    /// </summary>
    public class BossIntroCutscene : MonoBehaviour
    {
        #region 에디터 설정
        [Header("설정")]
        [SerializeField, Tooltip("등장연출 데이터")]
        private BossIntroData m_introData;

        [Header("Timeline")]
        [SerializeField, Tooltip("PlayableDirector (Timeline 사용 시)")]
        private PlayableDirector m_director;

        [SerializeField, Tooltip("Timeline 사용 여부")]
        private bool m_useTimeline;

        [Header("대상")]
        [SerializeField, Tooltip("대상 트랜스폼 (기본: 자기 자신)")]
        private Transform m_targetTransform;

        [SerializeField, Tooltip("대상 스프라이트 렌더러 (페이드인용)")]
        private SpriteRenderer[] m_targetRenderers;

        [Header("카메라")]
        [SerializeField, Tooltip("카메라 이동 사용 여부")]
        private bool m_useCameraMove;

        [SerializeField, Tooltip("이동할 카메라 위치")]
        private Vector3 m_cameraTargetPosition;

        [SerializeField, Tooltip("카메라 이동 시간")]
        private float m_cameraMoveDuration = 1f;

        [SerializeField, Tooltip("카메라 이징")]
        private Ease m_cameraEase = Ease.OutQuart;

        [Header("이벤트")]
        [SerializeField, Tooltip("이벤트 버스 (사운드/이벤트 재생을 위해)")]
        private IEventBus m_eventBus;
        #endregion

        #region 내부 필드
        private Vector3 m_originalScale;
        private Vector3 m_targetPosition;
        private Vector3 m_originalCameraPosition;
        private CancellationTokenSource m_cts;
        private bool m_isPlaying = false;
        private Camera m_mainCamera;
        #endregion

        #region 유니티 생명주기
        private void Awake()
        {
            if (m_targetTransform == null)
            {
                m_targetTransform = transform;
            }

            if (m_targetRenderers == null || m_targetRenderers.Length == 0)
            {
                m_targetRenderers = GetComponentsInChildren<SpriteRenderer>(true);
            }

            m_originalScale = m_targetTransform.localScale;
            m_targetPosition = m_targetTransform.localPosition;
            m_mainCamera = Camera.main;

            InitializeInternal();
        }

        private void OnDestroy()
        {
            StopAllIntro();
        }
        #endregion

        #region 초기화
        private void InitializeInternal()
        {
            Debug.Log("[BossIntroCutscene] EventBus는 외부에서 SetEventBus()를 통해 주입해주세요.");
        }

        /// <summary>
        /// [설명]: 외부에서 EventBus를 설정합니다.
        /// </summary>
        public void SetEventBus(IEventBus eventBus)
        {
            m_eventBus = eventBus;
        }

        /// <summary>
        /// [설명]: 외부에서 타겟을 설정합니다.
        /// </summary>
        public void SetTarget(Transform target)
        {
            m_targetTransform = target;
            if (target != null)
            {
                m_originalScale = target.localScale;
                m_targetPosition = target.localPosition;
            }
        }

        /// <summary>
        /// [설명]: 외부에서 렌더러를 설정합니다.
        /// </summary>
        public void SetRenderers(SpriteRenderer[] renderers)
        {
            m_targetRenderers = renderers;
        }

        /// <summary>
        /// [설명]: 카메라 목표 위치를 보스 위치로 자동 설정합니다.
        /// </summary>
        public void SetCameraToBoss()
        {
            if (m_targetTransform != null)
            {
                m_cameraTargetPosition = m_targetTransform.position + Vector3.back * 10f;
            }
        }
        #endregion

        #region 공개 메서드
        /// <summary>
        /// [설명]: 보스 등장연출을 실행합니다.
        /// </summary>
        /// <param name="onComplete">등장연출 완료 시 호출될 콜백</param>
        /// <returns>UniTask</returns>
        public async UniTaskVoid PlayIntroAsync(System.Action onComplete = null)
        {
            if (m_isPlaying)
            {
                onComplete?.Invoke();
                return;
            }

            m_isPlaying = true;
            m_cts = new CancellationTokenSource();
            var ct = m_cts.Token;

            try
            {
                Debug.Log($"[BossIntroCutscene] 등장연출 시작: {gameObject.name}");

                if (m_useTimeline && m_director != null)
                {
                    await PlayTimelineAsync(ct);
                }
                else
                {
                    await PlayDOTweenIntroAsync(ct);
                }

                Debug.Log($"[BossIntroCutscene] 등장연출 완료: {gameObject.name}");
                onComplete?.Invoke();
            }
            catch (System.OperationCanceledException)
            {
                Debug.Log($"[BossIntroCutscene] 등장연출 취소: {gameObject.name}");
            }
            finally
            {
                m_isPlaying = false;
                m_cts?.Dispose();
                m_cts = null;
            }
        }

        /// <summary>
        /// [설명]: Timeline 기반 등장연출을 실행합니다.
        /// </summary>
        private async UniTask PlayTimelineAsync(CancellationToken ct)
        {
            if (m_director == null) return;

            PublishIntroStartEvent();

            DisablePlayerControls();

            if (m_useCameraMove)
            {
                await MoveCameraAsync(ct);
            }

            m_director.Play();
            double duration = m_director.duration;

            await UniTask.Delay((int)(duration * 1000), cancellationToken: ct);

            PlayIntroSound();
            EnablePlayerControls();
            PublishIntroEndEvent();
        }

        /// <summary>
        /// [설명]: DOTween 기반 등장연출을 실행합니다.
        /// </summary>
        private async UniTask PlayDOTweenIntroAsync(CancellationToken ct)
        {
            if (m_introData == null)
            {
                Debug.LogWarning($"[BossIntroCutscene] IntroData가 없습니다: {gameObject.name}");
                return;
            }

            PublishIntroStartEvent();

            DisablePlayerControls();

            SetupInitialState();

            if (m_useCameraMove)
            {
                await MoveCameraAsync(ct);
            }

            await UniTask.WhenAll(
                PlayScaleAnimationAsync(ct),
                PlayPositionAnimationAsync(ct),
                PlayFadeInAnimationAsync(ct),
                PlayShakeEffectAsync(ct)
            );

            PlayIntroSound();
            EnablePlayerControls();
            PublishIntroEndEvent();
        }

        /// <summary>
        /// [설명]: 보스 등장연출을 중단합니다.
        /// </summary>
        public void StopAllIntro()
        {
            m_cts?.Cancel();
            m_cts?.Dispose();
            m_cts = null;

            if (m_useTimeline && m_director != null)
            {
                m_director.Stop();
            }

            DOTween.Kill(this);
            RestoreCamera();
            EnablePlayerControls();
            m_isPlaying = false;
        }

        /// <summary>
        /// [설명]: 현재 재생 중인지 확인합니다.
        /// </summary>
        public bool IsPlaying => m_isPlaying;

        /// <summary>
        /// [설명]: Timeline 사용 여부입니다.
        /// </summary>
        public bool UseTimeline => m_useTimeline;

        /// <summary>
        /// [설명]: 카메라 이동 사용 여부입니다.
        /// </summary>
        public bool UseCameraMove => m_useCameraMove;
        #endregion

        #region 카메라 이동
        private async UniTask MoveCameraAsync(CancellationToken ct)
        {
            if (m_mainCamera == null) return;

            m_originalCameraPosition = m_mainCamera.transform.position;

            if (m_cameraTargetPosition == Vector3.zero)
            {
                SetCameraToBoss();
            }

            await m_mainCamera.transform.DOMove(m_cameraTargetPosition, m_cameraMoveDuration)
                .SetEase(m_cameraEase)
                .SetTarget(this)
                .ToUniTask(cancellationToken: ct);
        }

        private void RestoreCamera()
        {
            if (m_mainCamera != null && m_useCameraMove)
            {
                m_mainCamera.transform.position = m_originalCameraPosition;
            }
        }
        #endregion

        #region 플레이어 컨트롤 관리 (레거시 Castle 참고)
        private void DisablePlayerControls()
        {
            if (m_eventBus != null)
            {
                m_eventBus.Publish(new OnBossIntroStarted());
                Debug.Log("[BossIntroCutscene] 플레이어 컨트롤 비활성화");
            }
        }

        private void EnablePlayerControls()
        {
            if (m_eventBus != null)
            {
                m_eventBus.Publish(new OnBossIntroEnded());
                Debug.Log("[BossIntroCutscene] 플레이어 컨트롤 활성화");
            }
        }
        #endregion

        #region 이벤트 发布 (통합됨)
        private void PublishIntroStartEvent()
        {
            // DisablePlayerControls에서 발행하므로 여기서는 빈 메서드 처리(또는 삭제 가능하지만 기존 호출부 유지를 위해 내부 비움)
        }

        private void PublishIntroEndEvent()
        {
            // EnablePlayerControls에서 발행하므로 여기서는 빈 메서드 처리
        }
        #endregion

        #region 내부 애니메이션 (DOTween)
        private void SetupInitialState()
        {
            if (m_introData == null) return;

            Vector3 startPos = m_targetPosition + m_introData.StartPositionOffset;
            m_targetTransform.localPosition = startPos;
            m_targetTransform.localScale = m_introData.StartScale;

            if (m_introData.UseFadeIn && m_targetRenderers != null)
            {
                for (int i = 0; i < m_targetRenderers.Length; i++)
                {
                    if (m_targetRenderers[i] != null)
                    {
                        Color c = m_targetRenderers[i].color;
                        c.a = 0f;
                        m_targetRenderers[i].color = c;
                    }
                }
            }
        }

        private async UniTask PlayScaleAnimationAsync(CancellationToken ct)
        {
            if (m_introData == null || m_targetTransform == null) return;

            var tween = m_targetTransform.DOScale(
                m_introData.EndScale,
                m_introData.ScaleDuration
            )
            .SetEase(m_introData.ScaleEase)
            .SetTarget(this);

            await tween.ToUniTask(cancellationToken: ct);
        }

        private async UniTask PlayPositionAnimationAsync(CancellationToken ct)
        {
            if (m_introData == null || m_targetTransform == null) return;

            if (m_introData.StartPositionOffset == Vector3.zero)
            {
                return;
            }

            Vector3 startPos = m_targetPosition + m_introData.StartPositionOffset;
            Vector3 endPos = m_targetPosition;

            var tween = m_targetTransform.DOLocalMove(endPos, m_introData.PositionDuration)
                .SetEase(m_introData.PositionEase)
                .SetTarget(this);

            await tween.ToUniTask(cancellationToken: ct);
        }

        private async UniTask PlayFadeInAnimationAsync(CancellationToken ct)
        {
            if (m_introData == null || !m_introData.UseFadeIn || m_targetRenderers == null) return;

            int len = m_targetRenderers.Length;
            if (len == 0) return;

            var tasks = new UniTask[len];
            for (int i = 0; i < len; i++)
            {
                var renderer = m_targetRenderers[i];
                if (renderer == null) continue;

                int index = i;
                tasks[index] = DOTween.To(
                    () => 0f,
                    val =>
                    {
                        if (m_targetRenderers[index] != null)
                        {
                            Color c = m_targetRenderers[index].color;
                            c.a = val;
                            m_targetRenderers[index].color = c;
                        }
                    },
                    1f,
                    m_introData.FadeInDuration
                )
                .SetEase(Ease.Linear)
                .SetTarget(this)
                .ToUniTask(cancellationToken: ct);
            }

            await UniTask.WhenAll(tasks);
        }

        private async UniTask PlayShakeEffectAsync(CancellationToken ct)
        {
            if (m_introData == null || !m_introData.UseShake || m_targetTransform == null) return;

            Vector3 originalPos = m_targetTransform.localPosition;
            float elapsed = 0f;

            await UniTask.WaitForSeconds(0.1f, cancellationToken: ct);

            while (elapsed < m_introData.ShakeDuration)
            {
                if (ct.IsCancellationRequested) break;

                float x = Random.Range(-m_introData.ShakeIntensity, m_introData.ShakeIntensity);
                float y = Random.Range(-m_introData.ShakeIntensity, m_introData.ShakeIntensity);
                m_targetTransform.localPosition = originalPos + new Vector3(x, y, 0);

                elapsed += Time.deltaTime;
                await UniTask.Yield(ct);
            }

            if (!ct.IsCancellationRequested)
            {
                m_targetTransform.localPosition = originalPos;
            }
        }

        private void PlayIntroSound()
        {
            if (m_introData == null) return;

            if (!string.IsNullOrEmpty(m_introData.IntroSoundKey))
            {
                if (m_eventBus == null)
                {
                    Debug.LogWarning($"[BossIntroCutscene] EventBus가 null입니다. 사운드 재생 불가: {m_introData.IntroSoundKey}");
                }
                else
                {
                    m_eventBus.Publish(new OnSoundRequested(m_introData.IntroSoundKey, 1f, 1f));
                    Debug.Log($"[BossIntroCutscene] 사운드 재생: {m_introData.IntroSoundKey}");
                }
            }

            if (!string.IsNullOrEmpty(m_introData.BgmKey))
            {
                if (m_eventBus == null)
                {
                    Debug.LogWarning($"[BossIntroCutscene] EventBus가 null입니다. BGM 재생 불가: {m_introData.BgmKey}");
                }
                else
                {
                    m_eventBus.Publish(new OnBGMRequested(m_introData.BgmKey, m_introData.BgmFadeInDuration));
                    Debug.Log($"[BossIntroCutscene] BGM 재생: {m_introData.BgmKey}");
                }
            }
        }
        #endregion

        #region Timeline 시그널 (레거시 SE_* 메서드 참고)
        /// <summary>
        /// [설명]: Timeline에서 호출되는 시작 시그널입니다.
        /// </summary>
        public void SE_Start()
        {
            Debug.Log("[BossIntroCutscene] Timeline SE_Start");
            DisablePlayerControls();

            if (m_useCameraMove)
            {
                SetCameraToBoss();
            }
        }

        /// <summary>
        /// [설명]: Timeline에서 호출되는 카메라 위치 설정 시그널입니다.
        /// </summary>
        public void SE_CameraSetPosition()
        {
            Debug.Log("[BossIntroCutscene] Timeline SE_CameraSetPosition");
            if (m_mainCamera != null && m_useCameraMove)
            {
                m_originalCameraPosition = m_mainCamera.transform.position;
                m_mainCamera.transform.position = m_cameraTargetPosition;
            }
        }

        /// <summary>
        /// [설명]: Timeline에서 호출되는 완료 시그널입니다.
        /// </summary>
        public void SE_Finish()
        {
            Debug.Log("[BossIntroCutscene] Timeline SE_Finish");
            PlayIntroSound();
            EnablePlayerControls();
            RestoreCamera();
            PublishIntroEndEvent();
        }
        #endregion
    }
}
