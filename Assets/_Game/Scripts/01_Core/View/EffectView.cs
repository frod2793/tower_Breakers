using UnityEngine;
using System;

namespace TowerBreakers.Core.View
{
    /// <summary>
    /// [설명]: 개별 스프라이트 이펙트의 애니메이션 재생 및 풀 반환을 담당하는 컴포넌트입니다.
    /// </summary>
    public class EffectView : MonoBehaviour
    {
        #region 내부 필드
        private Animator m_animator;
        private Action<EffectView> m_onComplete;
        private string m_effectId;
        #endregion

        #region 프로퍼티
        public string EffectId => m_effectId;
        #endregion

        #region 유니티 생명주기
        private void Awake()
        {
            m_animator = GetComponent<Animator>();
        }
        #endregion

        #region 공개 메서드
        /// <summary>
        /// [설명]: 이펙트를 초기화하고 재생을 시작합니다.
        /// </summary>
        /// <param name="effectId">이펙트 식별자</param>
        /// <param name="onComplete">종료 시 호출될 콜백 (풀 반환용)</param>
        public void Init(string effectId, Action<EffectView> onComplete)
        {
            m_effectId = effectId;
            m_onComplete = onComplete;
            
            if (m_animator != null)
            {
                // 애니메이션 첫 프레임부터 재생
                m_animator.Play(0, -1, 0f);
            }
        }
        #endregion

        #region 애니메이션 이벤트
        /// <summary>
        /// [설명]: 애니메이션 클립 끝에 배치된 이벤트에서 호출됩니다.
        /// </summary>
        public void OnAnimationComplete()
        {
            m_onComplete?.Invoke(this);
            gameObject.SetActive(false);
        }
        #endregion
    }
}
