using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer.Unity;
using TowerBreakers.Core.Events;

namespace TowerBreakers.UI.ViewModel
{
    using TowerBreakers.UI.DTO;
    using TowerBreakers.Player.DTO;
    #region 뷰모델 (ViewModel)
    /// <summary>
    /// [설명]: 전투 UI의 상태 관리 및 쿨다운 로직을 담당하는 뷰모델입니다.
    /// </summary>
    public class BattleUIViewModel : IInitializable, IDisposable
    {
        #region 내부 필드
        private readonly BattleUIDTO m_dto;
        private readonly PlayerConfigDTO m_playerConfig;
        private readonly IEventBus m_eventBus;
        private readonly Dictionary<string, float> m_cooldownRemaining = new Dictionary<string, float>();
        #endregion

        #region 프로퍼티 (상태값)
        public event Action<string, float> OnCooldownChanged;
        public event Action<string> OnSkillTriggered;
        public event Action<string> OnRewardMessageReceived;
        public event Action<bool> OnGoStateChanged; 
        public event Action<bool> OnInteractionChanged; 
        public event Action OnScreenClicked; 
        
        // [추가]: 체력바 동기화를 위한 프로퍼티
        public event Action<float> OnHpRatioChanged;
        #endregion

        #region 초기화 및 바인딩 로직
        public BattleUIViewModel(BattleUIDTO dto, PlayerConfigDTO playerConfig, IEventBus eventBus)
        {
            m_dto = dto ?? new BattleUIDTO();
            m_playerConfig = playerConfig ?? new PlayerConfigDTO();
            m_eventBus = eventBus;
        }

        public void Initialize()
        {
            SubscribeEvents();
        }

        private void SubscribeEvents()
        {
            if (m_eventBus == null) return;
            
            // [리팩토링]: 이벤트 버스 구독을 통해 상태 동기화
            m_eventBus.Subscribe<OnPlayerDamaged>(HandlePlayerDamaged);
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
            m_eventBus.Unsubscribe<OnEnemyKilled>(HandleEnemyKilled);
        }
        #endregion

        #region 이벤트 핸들러
        private void HandlePlayerDamaged(OnPlayerDamaged evt)
        {
            float ratio = (float)evt.CurrentHealth / evt.MaxHealth;
            OnHpRatioChanged?.Invoke(ratio);
        }

        private void HandleEnemyKilled(OnEnemyKilled evt)
        {
            // 적 처치 시 간단한 메시지 연출 (예시)
            // ShowRewardMessage($"적 처치! (+골드)");
        }
        #endregion

        #region 공개 메서드
        /// <summary>
        /// [설명]: GO 이미지의 점멸 상태를 설정하고 모든 인터랙션을 제어합니다.
        /// </summary>
        public void SetGoState(bool active)
        {
            OnGoStateChanged?.Invoke(active);
            SetInteractionEnabled(!active);
        }

        /// <summary>
        /// [설명]: 모든 버튼의 활성/비활성 상태를 설정합니다.
        /// </summary>
        public void SetInteractionEnabled(bool enabled)
        {
            OnInteractionChanged?.Invoke(enabled);
        }

        /// <summary>
        /// [설명]: 화면 클릭이 발생했음을 알립니다. (FloorTransitionService에서 대기용)
        /// </summary>
        public void NotifyScreenClicked()
        {
            OnScreenClicked?.Invoke();
        }
        /// <summary>
        /// [설명]: 외부에서 스킬 사용을 시도할 때 호출합니다.
        /// </summary>
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

        /// <summary>
        /// [설명]: 매 프로레임마다 호출되어 쿨다운 시간을 업데이트합니다.
        /// </summary>
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
            // [개선]: 로직(PlayerConfigDTO)과 UI의 쿨타임 설정을 단일화하여 정합성 보장
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
