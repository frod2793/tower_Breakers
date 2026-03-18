using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;

namespace TowerBreakers.UI.View
{
    /// <summary>
    /// [설명]: 클릭뿐만 아니라 누르고 있는(Hold) 상태에서도 지속적으로 이벤트를 발생시키는 커스텀 버튼 컴포넌트입니다.
    /// 핵앤슬래시의 연속 공격 구현을 위해 사용됩니다.
    /// </summary>
    public class HoldableButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        #region 에디터 설정
        [SerializeField, Tooltip("홀드 시 이벤트가 반복되는 주기 (초). 0이면 매 프레임 발생.")]
        private float m_repeatInterval = 0.05f;
        #endregion

        #region 내부 변수
        private bool m_isPressed = false;
        private float m_timer = 0f;
        
        /// <summary>
        /// [설명]: 버튼이 눌려 있거나 클릭될 때 호출되는 이벤트입니다.
        /// </summary>
        public event Action OnExecute;
        #endregion

        #region 유니티 생명주기
        private void Update()
        {
            if (!m_isPressed) return;

            if (m_repeatInterval <= 0)
            {
                OnExecute?.Invoke();
            }
            else
            {
                m_timer += Time.deltaTime;
                if (m_timer >= m_repeatInterval)
                {
                    OnExecute?.Invoke();
                    m_timer = 0f;
                }
            }
        }

        private void OnDisable()
        {
            ResetState();
        }
        #endregion

        #region 인터페이스 구현
        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            
            m_isPressed = true;
            m_timer = m_repeatInterval; // 즉시 첫 번째 실행을 위해 타이머 가득 채움
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            ResetState();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            // 버튼 영역 밖으로 나가면 즉시 중단
            ResetState();
        }
        #endregion

        #region 내부 로직
        private void ResetState()
        {
            m_isPressed = false;
            m_timer = 0f;
        }
        #endregion
    }
}
