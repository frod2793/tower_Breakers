using System;
using System.Collections.Generic;
using UnityEngine;

namespace TowerBreakers.UI.ViewModel
{
    using TowerBreakers.UI.DTO;
    using TowerBreakers.Player.DTO;
    #region 뷰모델 (ViewModel)
    /// <summary>
    /// [설명]: 전투 UI의 상태 관리 및 쿨다운 로직을 담당하는 뷰모델입니다.
    /// </summary>
    public class BattleUIViewModel
    {
        #region 내부 필드
        private readonly BattleUIDTO m_dto;
        private readonly Dictionary<string, float> m_cooldownRemaining = new Dictionary<string, float>();
        #endregion

        #region 프로퍼티 (상태값)
        public event Action<string, float> OnCooldownChanged;
        public event Action<string> OnSkillTriggered;
        #endregion

        #region 초기화 및 바인딩 로직
        public BattleUIViewModel(BattleUIDTO dto)
        {
            m_dto = dto ?? new BattleUIDTO();
        }
        #endregion

        #region 공개 메서드
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
        #endregion

        #region 내부 로직
        private float GetCooldownTime(string skillName)
        {
            if (skillName == m_dto.DashSkill.Name) return m_dto.DashSkill.CooldownTime;
            if (skillName == m_dto.ParrySkill.Name) return m_dto.ParrySkill.CooldownTime;
            if (skillName == m_dto.AttackSkill.Name) return m_dto.AttackSkill.CooldownTime;
            if (skillName == m_dto.Skill1.Name) return m_dto.Skill1.CooldownTime;
            if (skillName == m_dto.Skill2.Name) return m_dto.Skill2.CooldownTime;
            if (skillName == m_dto.Skill3.Name) return m_dto.Skill3.CooldownTime;
            return 0f;
        }
        #endregion
    }
    #endregion
}
