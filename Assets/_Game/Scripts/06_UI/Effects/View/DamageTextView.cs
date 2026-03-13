using UnityEngine;
using TMPro;
using DG.Tweening;

namespace TowerBreakers.UI.Effects.View
{
    /// <summary>
    /// [설명]: 화면에 떠오르는 데미지 텍스트의 연출과 표시를 담당하는 뷰 클래스입니다.
    /// DOTween을 사용하여 애니메이션을 처리하며, 완료 시 오브젝트 풀로 반환될 수 있도록 설계되었습니다.
    /// </summary>
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
        #endregion

        #region 유니티 생명주기
        private void Awake()
        {
            m_canvasGroup = GetComponent<CanvasGroup>();
            if (m_canvasGroup == null)
            {
                m_canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
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
            m_damageText.text = damage.ToString();
            m_damageText.color = isCritical ? m_criticalColor : m_normalColor;
            
            // 텍스트 크기 설정
            float targetScale = isCritical ? m_criticalScale : m_normalScale;
            transform.localScale = Vector3.one * targetScale;

            // 크리티컬일 경우 역동적인 Punch 연출 추가
            if (isCritical)
            {
                transform.DOPunchScale(Vector3.one * 0.2f, 0.3f, 10, 1f);
            }

            PlayAnimation();
        }
        #endregion

        #region 내부 로직
        /// <summary>
        /// [설명]: DOTween을 사용하여 위로 떠오르며 사라지는 애니메이션을 실행합니다.
        /// </summary>
        private void PlayAnimation()
        {
            // 이전 트윈 중단
            transform.DOKill();
            if (m_canvasGroup != null)
            {
                m_canvasGroup.DOKill();
                m_canvasGroup.alpha = 1f;
            }
            Vector3 targetPos = transform.position + Vector3.up * m_floatHeight;

            // 시퀀스 생성
            Sequence seq = DOTween.Sequence();
            
            // 위로 이동
            seq.Join(transform.DOMove(targetPos, m_duration).SetEase(Ease.OutBack));
            
            // 서서히 사라짐 (지속 시간의 절반 이후에 시작)
            seq.Insert(m_duration * 0.5f, m_canvasGroup.DOFade(0f, m_duration * 0.5f));

            // 완료 시 콜백 호출
            seq.OnComplete(() =>
            {
                m_onComplete?.Invoke(this);
            });
        }
        #endregion
    }
}
