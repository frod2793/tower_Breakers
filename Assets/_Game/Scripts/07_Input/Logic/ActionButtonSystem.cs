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
        private TowerBreakers.Core.Events.IEventBus m_eventBus;
        private bool m_isInputLocked = false;
        #endregion

        #region 초기화
        [Inject]
        public void Construct(PlayerActionHandler actionHandler, TowerBreakers.Core.Events.IEventBus eventBus)
        {
            m_actionHandler = actionHandler;
            m_eventBus = eventBus;
            BindButtons();
        }

        private void BindButtons()
        {
            if (m_attackButton != null)
                m_attackButton.onClick.AddListener(() => TryExecuteAction(PlayerActionType.Attack));

            if (m_skill1Button != null)
                m_skill1Button.onClick.AddListener(() => TryExecuteAction(PlayerActionType.Skill1));

            if (m_skill2Button != null)
                m_skill2Button.onClick.AddListener(() => TryExecuteAction(PlayerActionType.Skill2));

            if (m_skill3Button != null)
                m_skill3Button.onClick.AddListener(() => TryExecuteAction(PlayerActionType.Skill3));

            if (m_leapButton != null)
                m_leapButton.onClick.AddListener(() => TryExecuteAction(PlayerActionType.Leap));

            if (m_defendButton != null)
            {
                m_defendButton.onClick.AddListener(() => TryExecuteAction(PlayerActionType.Defend));
            }
        }
        
        private void TryExecuteAction(PlayerActionType actionType)
        {
            if (m_isInputLocked || m_actionHandler == null) return;
            m_actionHandler.ExecuteAction(actionType);
        }
        #endregion

        #region 유니티 생명주기
        private void OnEnable()
        {
            m_eventBus?.Subscribe<TowerBreakers.Core.Events.OnBossIntroStarted>(OnBossIntroStarted);
            m_eventBus?.Subscribe<TowerBreakers.Core.Events.OnBossIntroEnded>(OnBossIntroEnded);
            m_eventBus?.Subscribe<TowerBreakers.Core.Events.OnGamePause>(OnGamePause);
            m_eventBus?.Subscribe<TowerBreakers.Core.Events.OnGameResume>(OnGameResume);
        }

        private void OnDisable()
        {
            m_eventBus?.Unsubscribe<TowerBreakers.Core.Events.OnBossIntroStarted>(OnBossIntroStarted);
            m_eventBus?.Unsubscribe<TowerBreakers.Core.Events.OnBossIntroEnded>(OnBossIntroEnded);
            m_eventBus?.Unsubscribe<TowerBreakers.Core.Events.OnGamePause>(OnGamePause);
            m_eventBus?.Unsubscribe<TowerBreakers.Core.Events.OnGameResume>(OnGameResume);
        }

        private void OnBossIntroStarted(TowerBreakers.Core.Events.OnBossIntroStarted evt) { m_isInputLocked = true; }
        private void OnBossIntroEnded(TowerBreakers.Core.Events.OnBossIntroEnded evt) { m_isInputLocked = false; }
        
        private void OnGamePause(TowerBreakers.Core.Events.OnGamePause evt) { m_isInputLocked = true; }
        private void OnGameResume(TowerBreakers.Core.Events.OnGameResume evt) { m_isInputLocked = false; }

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
