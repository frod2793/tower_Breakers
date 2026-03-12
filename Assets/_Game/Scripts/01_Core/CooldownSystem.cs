using System;
using System.Collections.Generic;
using UnityEngine;

namespace TowerBreakers.Core
{
    /// <summary>
    /// [설명]: 액션별 쿨다운을 관리하는 시스템입니다.
    /// </summary>
    public class CooldownSystem
    {
        #region 내부 필드
        private readonly Dictionary<string, float> m_cooldowns = new Dictionary<string, float>();
        private readonly Dictionary<string, float> m_maxCooldowns = new Dictionary<string, float>();

        // Zero Allocation: Update에서 키 복사 대신 캐시된 리스트 재사용
        private readonly List<string> m_keyCache = new List<string>();
        private bool m_isKeyCacheDirty = true;
        #endregion

        #region 공개 메서드
        public void SetCooldown(string actionName, float duration)
        {
            m_cooldowns[actionName] = duration;
            m_maxCooldowns[actionName] = duration;
            m_isKeyCacheDirty = true;
        }

        public bool IsOnCooldown(string actionName)
        {
            return m_cooldowns.ContainsKey(actionName) && m_cooldowns[actionName] > 0;
        }

        public float GetRemainingTime(string actionName)
        {
            return m_cooldowns.ContainsKey(actionName) ? m_cooldowns[actionName] : 0;
        }

        public float GetNormalizedProgress(string actionName)
        {
            if (!m_cooldowns.ContainsKey(actionName) || m_maxCooldowns[actionName] <= 0) return 0;
            return m_cooldowns[actionName] / m_maxCooldowns[actionName];
        }

        public void Update(float deltaTime)
        {
            if (m_isKeyCacheDirty)
            {
                m_keyCache.Clear();
                m_keyCache.AddRange(m_cooldowns.Keys);
                m_isKeyCacheDirty = false;
            }

            for (int i = 0; i < m_keyCache.Count; i++)
            {
                var key = m_keyCache[i];
                if (m_cooldowns[key] > 0)
                {
                    m_cooldowns[key] -= deltaTime;
                }
            }
        }
        #endregion
    }
}
