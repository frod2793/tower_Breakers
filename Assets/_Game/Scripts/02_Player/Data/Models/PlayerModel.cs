using System;
using UnityEngine;
using TowerBreakers.Player.Data.SO;

namespace TowerBreakers.Player.Data.Models
{
    /// <summary>
    /// [설명]: 플레이어의 실시간 상태 및 데이터를 관리하는 모델 클래스입니다.
    /// POCO (Plain Old C# Object)로 설계되어 Unity API에 의존하지 않습니다.
    /// </summary>
    public class PlayerModel
    {
        #region 내부 변수
        private int m_currentLifeCount;
        private int m_baseMaxLife;
        private int m_maxLifeCount; // [주의]: 합산된 최종 최대 생명력
        private int m_killCount;
        private int m_chestCount;
        private Vector2 m_position;
        private WeaponData m_currentWeapon;
        private ArmorData m_currentHelmet;
        private ArmorData m_currentBodyArmor;
        
        // 무적 시스템 관련 (Part 3-2)
        private float m_invincibilityTimer;
        #endregion

        #region 프로퍼티
        public int CurrentLifeCount 
        { 
            get => m_currentLifeCount; 
            private set
            {
                m_currentLifeCount = Math.Max(0, Math.Min(m_maxLifeCount, value));
                OnLifeCountChanged?.Invoke(m_currentLifeCount, m_maxLifeCount);
            }
        }
        
        public int MaxLifeCount => m_maxLifeCount;
        public int KillCount => m_killCount;
        public int ChestCount => m_chestCount;
        public Vector2 Position { get => m_position; set => m_position = value; }
        public bool IsDead => m_currentLifeCount <= 0;
        public WeaponData CurrentWeapon => m_currentWeapon;
        public ArmorData CurrentHelmet => m_currentHelmet;
        public ArmorData CurrentBodyArmor => m_currentBodyArmor;
        
        /// <summary>
        /// [설명]: 현재 플레이어가 무적 상태인지 여부를 반환합니다.
        /// </summary>
        public bool IsInvincible => m_invincibilityTimer > 0f;
        
        /// <summary>
        /// [설명]: 남은 무적 시간을 반환합니다.
        /// </summary>
        public float InvincibilityRemaining => m_invincibilityTimer;

        // 보정된 스탯 프로퍼티 (WeaponData는 ScriptableObject이므로 ?. 대신 null 체크 사용)
        private float GetWeaponModifier(float modifier) => m_currentWeapon != null ? modifier : 1.0f;
        public int FinalAttackPower(int basePower) => (int)(basePower * GetWeaponModifier(m_currentWeapon != null ? m_currentWeapon.AttackPowerModifier : 1.0f));
        public float FinalAttackRange(float baseRange) => baseRange * GetWeaponModifier(m_currentWeapon != null ? m_currentWeapon.AttackRangeModifier : 1.0f);
        public float FinalAttackSpeed(float baseSpeed) => baseSpeed * GetWeaponModifier(m_currentWeapon != null ? m_currentWeapon.AttackSpeedModifier : 1.0f);

        // 갑주 보정 스탯 (헬멧 + 흉갑 합산)
        public int FinalLifeBonus => (m_currentHelmet != null ? m_currentHelmet.LifeBonus : 0) + (m_currentBodyArmor != null ? m_currentBodyArmor.LifeBonus : 0);
        public float FinalPushResistance => Math.Min(1.0f, (m_currentHelmet != null ? m_currentHelmet.PushResistance : 0f) + (m_currentBodyArmor != null ? m_currentBodyArmor.PushResistance : 0f));
        
        public float FinalMoveSpeed(float baseSpeed) 
        {
            float modifier = 1.0f;
            if (m_currentHelmet != null) modifier *= m_currentHelmet.MoveSpeedModifier;
            if (m_currentBodyArmor != null) modifier *= m_currentBodyArmor.MoveSpeedModifier;
            return baseSpeed * modifier;
        }
        #endregion

        #region 이벤트
        /// <summary>
        /// [설명]: 생명 수 변경 시 발행되는 이벤트 (현재 생명, 최대 생명)
        /// </summary>
        public event Action<int, int> OnLifeCountChanged;

        /// <summary>
        /// [설명]: 무기 변경 시 발행되는 이벤트
        /// </summary>
        public event Action<WeaponData> OnWeaponChanged;

        /// <summary>
        /// [설명]: 헬멧 변경 시 발행되는 이벤트
        /// </summary>
        public event Action<ArmorData> OnHelmetChanged;

        /// <summary>
        /// [설명]: 흉갑 변경 시 발행되는 이벤트
        /// </summary>
        public event Action<ArmorData> OnBodyArmorChanged;

        /// <summary>
        /// [설명]: 처치 수 변경 시 발행되는 이벤트
        /// </summary>
        public event Action<int> OnKillsChanged;

        /// <summary>
        /// [설명]: 보물상자 수 변경 시 발행되는 이벤트
        /// </summary>
        public event Action<int> OnChestsChanged;
        #endregion

        #region 초기화 및 비즈니스 로직
        public void Initialize(PlayerData data)
        {
            if (data == null)
            {
                Debug.LogError("[PlayerModel] 초기화 실패: PlayerData가 null입니다.");
                return;
            }

            m_baseMaxLife = data.MaxLifeCount;
            UpdateMaxLife();
            
            CurrentLifeCount = m_maxLifeCount;
            
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[PlayerModel] 초기화 완료 (Life: {m_maxLifeCount})");
            #endif
            if (data.DefaultWeapon != null)
            {
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log($"[PlayerModel] 기본 무기 장착: {data.DefaultWeapon.WeaponName}");
                #endif
                SetWeapon(data.DefaultWeapon);
            }
            else
            {
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning("[PlayerModel] PlayerData에 기본 무기가 설정되어 있지 않습니다.");
                #endif
            }
        }

        /// <summary>
        /// [설명]: 플레이어에게 데미지를 입힙니다. 데미지 적용 시 설정된 시간만큼 무적 상태가 됩니다.
        /// </summary>
        /// <param name="damage">입힐 데미지 (현재는 하트 시스템이므로 기본 1로 처리)</param>
        /// <param name="invincibilityDuration">무적 지속 시간</param>
        public void TakeDamage(int damage, float invincibilityDuration = 0.5f)
        {
            if (IsInvincible)
            {
                // Debug.Log($"[PlayerModel] TakeDamage 무시 (무적 상태)");
                return;
            }

            // 카운트제 라이프 시스템이므로 데미지(damage)만큼 생명력을 차감합니다.
            // 방어력(Defense) 대신 밀기 저항 등이 위치 유지에 도움을 줍니다.
            if (damage > 0)
            {
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[PlayerModel] TakeDamage 수령: 현재 생명={CurrentLifeCount}, 입힌 데미지={damage}");
            #endif
                CurrentLifeCount -= damage;
            }

            // 무적 타이머 설정 (데미지 수령 여부와 관계없이 피격 액션이 발생했다면 적용)
            m_invincibilityTimer = invincibilityDuration;
        }

        /// <summary>
        /// [설명]: 특정 시간 동안 플레이어를 무적 상태로 만듭니다.
        /// </summary>
        /// <param name="duration">무적 지속 시간 (초)</param>
        public void SetInvincibility(float duration)
        {
            m_invincibilityTimer = Math.Max(m_invincibilityTimer, duration);
            
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[PlayerModel] 무적 강제 설정: {duration}초 (현재 남은 시간: {m_invincibilityTimer})");
            #endif
        }

        /// <summary>
        /// [설명]: 무적 타이머를 갱신합니다. 매 프레임 ITickable을 통해 호출되어야 합니다.
        /// </summary>
        /// <param name="deltaTime">프레임 증분 시간</param>
        public void Tick(float deltaTime)
        {
            if (m_invincibilityTimer > 0f)
            {
                m_invincibilityTimer -= deltaTime;
                if (m_invincibilityTimer <= 0f)
                {
                    m_invincibilityTimer = 0f;
                    // Debug.Log("[PlayerModel] 무적 상태 해제");
                }
            }
        }

        public void AddKill()
        {
            m_killCount++;
            OnKillsChanged?.Invoke(m_killCount);
        }

        public void AddChest()
        {
            m_chestCount++;
            OnChestsChanged?.Invoke(m_chestCount);
        }

        public void Heal(int amount)
        {
            if (IsDead) return;
            CurrentLifeCount += amount;
        }

        /// <summary>
        /// [설명]: 새로운 무기를 장착하고 관련 이벤트를 발생시킵니다.
        /// </summary>
        public void SetWeapon(WeaponData weapon)
        {
            m_currentWeapon = weapon;
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[PlayerModel] 무기 장착: {(weapon != null ? weapon.WeaponName : "없음")}");
            #endif
            OnWeaponChanged?.Invoke(weapon);
        }

        /// <summary>
        /// [설명]: 새로운 갑주(헬멧 또는 흉갑)를 장착하고 관련 이벤트를 발생시킵니다.
        /// 장비에 따른 최대 생명력 변화를 동기화합니다.
        /// </summary>
        public void SetArmor(ArmorData armor)
        {
            if (armor == null) return;

            // 1. 장비 교체
            if (armor.Category == ArmorCategory.Helmet)
            {
                m_currentHelmet = armor;
                Debug.Log($"[PlayerModel] 헬멧 장착: {armor.ArmorName} (LifeBonus: {armor.LifeBonus})");
            }
            else
            {
                m_currentBodyArmor = armor;
                Debug.Log($"[PlayerModel] 흉갑 장착: {armor.ArmorName} (LifeBonus: {armor.LifeBonus})");
            }

            // 2. 최대 생명력 갱신 (기본 생명력 + 모든 장비 보너스)
            // 주의: PlayerData는 초기화 시에만 사용하므로, m_maxLifeCount의 베이스 값을 유지해야 함.
            // 여기서는 m_maxLifeCount가 '최종' 값이 되도록 관리합니다.
            // (간소화를 위해 Initialize에서 받은 baseMaxLife를 별도로 저장하지 않았다면 구조적 개선이 필요할 수 있으나,
            // 현재 요구사항 내에서 해결하기 위해 합산 로직을 처리합니다.)
            
            // TODO: PlayerData의 원본 MaxLifeCount를 별도 백업필드(m_baseMaxLife)에 두는 것이 정확함.
            // 현재는 m_maxLifeCount를 갱신하는 방식으로 진행하되, UI 갱신을 위해 Property 세터를 활용합니다.
            UpdateMaxLife();

            // 3. 이벤트 발행
            if (armor.Category == ArmorCategory.Helmet) OnHelmetChanged?.Invoke(armor);
            else OnBodyArmorChanged?.Invoke(armor);
        }

        private void UpdateMaxLife()
        {
            int previousMax = m_maxLifeCount;
            m_maxLifeCount = m_baseMaxLife + FinalLifeBonus;

            // 최대 생명력이 늘어났을 때 그 차이만큼 현재 생명력 즉시 회복
            int diff = m_maxLifeCount - previousMax;
            if (diff > 0)
            {
                m_currentLifeCount += diff;
            }

            // 최대 생명력이 줄어들었을 경우 현재 생명력을 상한선에 맞게 조정
            if (m_currentLifeCount > m_maxLifeCount)
            {
                m_currentLifeCount = m_maxLifeCount;
            }
            
            // 변경 사항 전파
            OnLifeCountChanged?.Invoke(m_currentLifeCount, m_maxLifeCount);
            
            Debug.Log($"[PlayerModel] MaxLife 업데이트: {previousMax} -> {m_maxLifeCount} (증가분: {diff}, 현재 생명력: {m_currentLifeCount})");
        }
        #endregion
    }
}
