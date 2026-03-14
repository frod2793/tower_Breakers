using UnityEngine;
using TMPro;
using DG.Tweening;

namespace TowerBreakers.UI.Effects.View
{
    /// <summary>
    /// [설명]: 화면에 떠오르는 데미지 텍스트의 연출과 표시를 담당하는 뷰 클래스입니다.
    /// DOTween을 사용하여 애니메이션을 처리하며, 완료 시 오브젝트 풀로 반환될 수 있도록 설계되었습니다.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class DamageTextView : MonoBehaviour
    {
        #region 에디터 설정
        [SerializeField, Tooltip("데미지 숫자를 표시할 TextMeshPro 컴포넌트")]
        private TextMeshProUGUI m_damageText;

        [Header("연출 설정")]
        [SerializeField, Tooltip("텍스트가 떠오르는 높이")]
        private float m_floatHeight = 1.0f;

        [SerializeField, Tooltip("연출 지속 시간")]
        private float m_duration = 0.8f;
        
        [SerializeField, Tooltip("일반 데미지 색상")]
        private Color m_normalColor = Color.white;

        [SerializeField, Tooltip("크리티컬 데미지 색상")]
        private Color m_criticalColor = Color.yellow;

        [Header("크기 설정")]
        [SerializeField, Tooltip("일반 데미지 기본 크기")]
        private float m_normalScale = 1.0f;

        [SerializeField, Tooltip("크리티컬 데미지 기본 크기")]
        private float m_criticalScale = 1.5f;
        #endregion

        #region 내부 변수
        private System.Action<DamageTextView> m_onComplete;
        private CanvasGroup m_canvasGroup;

        // [최적화]: 데미지 수치 문자열 변환 시 GC 할당 방지를 위한 정적 캐시
        private static readonly System.Collections.Generic.Dictionary<int, string> s_stringCache = new System.Collections.Generic.Dictionary<int, string>();

        private static string GetCachedString(int value)
        {
            if (!s_stringCache.TryGetValue(value, out string str))
            {
                str = value.ToString();
                s_stringCache[value] = str;
            }
            return str;
        }
        #endregion

        #region 유니티 생명주기
        private void Awake()
        {
            m_canvasGroup = GetComponent<CanvasGroup>();
        }

        private void OnDisable()
        {
            // [최적화]: 객체 비활성화 시 연출 일괄 종료 (풀 반환 시 안전장치)
            DOTween.Kill(this);
        }
        #endregion

        #region 공개 API
        /// <summary>
        /// [설명]: 데미지 텍스트를 초기화하고 연출을 시작합니다.
        /// </summary>
        /// <param name="damage">표시할 데미지 수치</param>
        /// <param name="isCritical">크리티컬 박부</param>
        /// <param name="onComplete">연출 완료 시 호출될 콜백 (풀 반환용)</param>
        public void Show(int damage, bool isCritical, System.Action<DamageTextView> onComplete)
        {
            if (m_damageText == null) return;

            m_onComplete = onComplete;
            // [최적화]: 캐싱된 문자열 데이터 사용하여 ToString 할당 방지
            m_damageText.text = GetCachedString(damage);
            m_damageText.color = isCritical ? m_criticalColor : m_normalColor;
            
            // 텍스트 크기 설정
            float targetScale = isCritical ? m_criticalScale : m_normalScale;
            transform.localScale = Vector3.one * targetScale;

            // 크리티컬일 경우 역동적인 Punch 연출 추가
            if (isCritical)
            {
                transform.DOPunchScale(Vector3.one * 0.2f, 0.3f, 10, 1f).SetTarget(this);
            }

            PlayAnimation();
        }
        #endregion

        #region 내부 로직
        /// <summary>
        /// [설명]: 단 하나의 가상 트윈을 사용하여 위치와 투명도를 동시에 애니메이션합니다.
        /// [최적화]: Sequence + DOMove + DOFade (3개 객체)를 1개의 Float 트윈으로 통합
        /// </summary>
        private void PlayAnimation()
        {
            // 모든 연출 일괄 정리
            DOTween.Kill(this);
            
            if (m_canvasGroup != null)
            {
                m_canvasGroup.alpha = 1f;
            }

            Vector3 startPos = transform.position;

            // 단 하나의 가상 트윈으로 모든 변화 제어 (객체 생성 최소화)
            DOVirtual.Float(0f, 1f, m_duration, (progress) =>
            {
                // 1. 위치 업데이트 (Ease.OutBack 근사치 또는 선형 이동 후 감속)
                // 복잡한 Ease 계산 대신 단순화된 곡선 적용 가능 (여기서는 OutBack 느낌을 위해 0.0~1.0을 활용)
                // 실제 EaseManager 활용 시: float moveEval = DG.Tweening.Core.Easing.EaseManager.Evaluate(Ease.OutBack, null, progress, 1f, 1.70158f, 0f);
                
                // 간단한 OutBack 수식 (C# 구현)
                float c1 = 1.70158f;
                float c3 = c1 + 1f;
                float moveEval = 1f + c3 * Mathf.Pow(progress - 1f, 3f) + c1 * Mathf.Pow(progress - 1f, 2f);
                
                transform.position = startPos + Vector3.up * (m_floatHeight * moveEval);

                // 2. 투명도 업데이트 (지속 시간의 50% 이후부터 FadeOut)
                if (progress > 0.5f)
                {
                    float alphaProgress = (progress - 0.5f) / 0.5f;
                    if (m_canvasGroup != null)
                    {
                        m_canvasGroup.alpha = 1f - alphaProgress;
                    }
                }
            })
            .SetTarget(this)
            .OnComplete(() =>
            {
                m_onComplete?.Invoke(this);
            });
        }
        #endregion
    }
}
