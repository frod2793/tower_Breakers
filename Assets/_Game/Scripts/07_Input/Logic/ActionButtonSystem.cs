using UnityEngine;
using UnityEngine.UI;
using VContainer;
using TowerBreakers.Player.Logic;

namespace TowerBreakers.Input.Logic
{
    /// <summary>
    /// [설명]: UI 버튼 입력을 플레이어 액션으로 연결하는 컴포넌트입니다.
    /// 6개의 액션 버튼을 관리합니다.
    /// </summary>
    public class ActionButtonSystem : MonoBehaviour
    {
        #region 에디터 설정
        [Header("액션 버튼")]
        [SerializeField] private Button m_attackButton;
        [SerializeField] private Button m_skill1Button;
        [SerializeField] private Button m_skill2Button;
        [SerializeField] private Button m_skill3Button;
        [SerializeField] private Button m_leapButton;
        [SerializeField] private Button m_defendButton;
        #endregion

        #region 내부 변수
        private PlayerActionHandler m_actionHandler;
        #endregion

        #region 초기화
        [Inject]
        public void Construct(PlayerActionHandler actionHandler)
        {
            m_actionHandler = actionHandler;
            BindButtons();
        }

        private void BindButtons()
        {
            if (m_attackButton != null)
                m_attackButton.onClick.AddListener(() => m_actionHandler.ExecuteAction(PlayerActionType.Attack));

            if (m_skill1Button != null)
                m_skill1Button.onClick.AddListener(() => m_actionHandler.ExecuteAction(PlayerActionType.Skill1));

            if (m_skill2Button != null)
                m_skill2Button.onClick.AddListener(() => m_actionHandler.ExecuteAction(PlayerActionType.Skill2));

            if (m_skill3Button != null)
                m_skill3Button.onClick.AddListener(() => m_actionHandler.ExecuteAction(PlayerActionType.Skill3));

            if (m_leapButton != null)
                m_leapButton.onClick.AddListener(() => m_actionHandler.ExecuteAction(PlayerActionType.Leap));

            if (m_defendButton != null)
            {
                m_defendButton.onClick.AddListener(() => 
                {
                    if (m_actionHandler != null)
                    {
                        m_actionHandler.ExecuteAction(PlayerActionType.Defend);
                    }
                });
            }
        }
        #endregion

        #region 유니티 생명주기
        private void OnDestroy()
        {
            if (m_attackButton != null) m_attackButton.onClick.RemoveAllListeners();
            if (m_skill1Button != null) m_skill1Button.onClick.RemoveAllListeners();
            if (m_skill2Button != null) m_skill2Button.onClick.RemoveAllListeners();
            if (m_skill3Button != null) m_skill3Button.onClick.RemoveAllListeners();
            if (m_leapButton != null) m_leapButton.onClick.RemoveAllListeners();
            if (m_defendButton != null) m_defendButton.onClick.RemoveAllListeners();
        }
        #endregion
    }
}
