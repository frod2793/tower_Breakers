using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer.Unity;
using TowerBreakers.Core.Events;
using TowerBreakers.Player.Service;
using TowerBreakers.Player.Stat;

namespace TowerBreakers.UI.ViewModel
{
    using TowerBreakers.UI.DTO;
    using TowerBreakers.Player.DTO;
    #region 뷰모델 (ViewModel)
    /// <summary>
    /// [설명]: 전투 UI의 상태 관리 및 쿨다운 로직을 담당하는 뷰모델입니다.
    /// 플레이어 체력과 적 수량을 아이콘 기반으로 표시하기 위한 데이터를 관리합니다.
    /// </summary>
    public class BattleUIViewModel : IInitializable, IDisposable
    {
        #region 내부 필드
        private readonly BattleUIDTO m_dto;
        private readonly PlayerConfigDTO m_playerConfig;
        private readonly IEventBus m_eventBus;
        private readonly IPlayerStatService m_playerStatService;
        private readonly Dictionary<string, float> m_cooldownRemaining = new Dictionary<string, float>();
        #endregion

        #region 프로퍼티 (상태값)
        public event Action<string, float> OnCooldownChanged;
        public event Action<string> OnSkillTriggered;
        public event Action<string> OnRewardMessageReceived;
        public event Action<bool> OnGoStateChanged; 
        public event Action<bool> OnInteractionChanged; 
        public event Action OnScreenClicked; 
        
        // [상태]: 정수형 체력 정보를 전달 (아이콘 개수 매칭용)
        public event Action<int, int> OnHealthChanged; 
        public event Action<int, int> OnRemainingEnemyChanged; 
        public event Action<OnEnemyCountChanged> OnDetailedEnemyCountChanged; 
        #endregion

        #region 초기화 및 바인딩 로직
        public BattleUIViewModel(
            BattleUIDTO dto, 
            PlayerConfigDTO playerConfig, 
            IEventBus eventBus,
            IPlayerStatService playerStatService)
        {
            m_dto = dto ?? new BattleUIDTO();
            m_playerConfig = playerConfig ?? new PlayerConfigDTO();
            m_eventBus = eventBus;
            m_playerStatService = playerStatService;
        }

        public void Initialize()
        {
            SubscribeEvents();
            RequestInitialState();
        }

        /// <summary>
        /// [설명]: View가 구독을 완료한 후, 초기 데이터를 즉시 받기 위해 호출합니다.
        /// </summary>
        public void RequestInitialState()
        {
            if (m_playerStatService != null)
            {
                OnHealthChanged?.Invoke(m_playerStatService.TotalHealth, m_playerStatService.TotalHealth);
            }
        }

        private void SubscribeEvents()
        {
            if (m_eventBus == null) return;
            
            m_eventBus.Subscribe<OnPlayerDamaged>(HandlePlayerDamaged);
            m_eventBus.Subscribe<OnEnemyCountChanged>(HandleEnemyCountChanged);
            m_eventBus.Subscribe<OnEnemyKilled>(HandleEnemyKilled);
        }

        public void Dispose()
        {
            UnsubscribeEvents();
        }

        private void UnsubscribeEvents()
        {
            if (m_eventBus == null) return;
            m_eventBus.Unsubscribe<OnPlayerDamaged>(HandlePlayerDamaged);
            m_eventBus.Unsubscribe<OnEnemyCountChanged>(HandleEnemyCountChanged);
            m_eventBus.Unsubscribe<OnEnemyKilled>(HandleEnemyKilled);
        }
        #endregion

        #region 이벤트 핸들러
        private void HandlePlayerDamaged(OnPlayerDamaged evt)
        {
            OnHealthChanged?.Invoke(evt.CurrentHealth, evt.MaxHealth);
        }

        private void HandleEnemyCountChanged(OnEnemyCountChanged evt)
        {
            OnRemainingEnemyChanged?.Invoke(evt.NormalRemaining + evt.EliteRemaining + evt.BossRemaining, evt.TotalTotal);
            OnDetailedEnemyCountChanged?.Invoke(evt);
        }

        private void HandleEnemyKilled(OnEnemyKilled evt)
        {
            // 적 처치 연출 등
        }
        #endregion

        #region 공개 메서드
        public void SetGoState(bool active)
        {
            OnGoStateChanged?.Invoke(active);
            SetInteractionEnabled(!active);
        }

        public void SetInteractionEnabled(bool enabled)
        {
            OnInteractionChanged?.Invoke(enabled);
        }

        public void NotifyScreenClicked()
        {
            OnScreenClicked?.Invoke();
        }

        public void ExecuteSkill(string skillName)
        {
            if (IsOnCooldown(skillName)) return;

            float cooldown = GetCooldownTime(skillName);
            if (cooldown > 0)
            {
                m_cooldownRemaining[skillName] = cooldown;
                OnSkillTriggered?.Invoke(skillName);
                OnCooldownChanged?.Invoke(skillName, 1f);
            }
        }

        public void Update(float deltaTime)
        {
            var keys = new List<string>(m_cooldownRemaining.Keys);
            foreach (var key in keys)
            {
                if (m_cooldownRemaining[key] > 0)
                {
                    m_cooldownRemaining[key] -= deltaTime;
                    float ratio = Mathf.Clamp01(m_cooldownRemaining[key] / GetCooldownTime(key));
                    OnCooldownChanged?.Invoke(key, ratio);

                    if (m_cooldownRemaining[key] <= 0)
                    {
                        m_cooldownRemaining.Remove(key);
                        OnCooldownChanged?.Invoke(key, 0f);
                    }
                }
            }
        }

        public bool IsOnCooldown(string skillName)
        {
            return m_cooldownRemaining.ContainsKey(skillName) && m_cooldownRemaining[skillName] > 0;
        }

        public void ShowRewardMessage(string message)
        {
            OnRewardMessageReceived?.Invoke(message);
        }
        #endregion

        #region 내부 로직
        private float GetCooldownTime(string skillName)
        {
            if (skillName == m_dto.DashSkill.Name) return m_playerConfig.DashCooldown;
            if (skillName == m_dto.ParrySkill.Name) return m_playerConfig.ParryCooldown;
            if (skillName == m_dto.AttackSkill.Name) return m_playerConfig.AttackCooldown;
            
            if (skillName == m_dto.Skill1.Name) return m_dto.Skill1.CooldownTime;
            if (skillName == m_dto.Skill2.Name) return m_dto.Skill2.CooldownTime;
            if (skillName == m_dto.Skill3.Name) return m_dto.Skill3.CooldownTime;
            return 0f;
        }
        #endregion
    }
    #endregion
}
