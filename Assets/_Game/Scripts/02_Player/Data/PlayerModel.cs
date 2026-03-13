using System;
using UnityEngine;

namespace TowerBreakers.Player.Data
{
    /// <summary>
    /// [설명]: 플레이어의 실시간 상태 및 데이터를 관리하는 모델 클래스입니다.
    /// POCO (Plain Old C# Object)로 설계되어 Unity API에 의존하지 않습니다.
    /// </summary>
    public class PlayerModel
    {
        #region 내부 변수
        private int m_currentLifeCount;
        private int m_maxLifeCount;
        private int m_killCount;
        private int m_chestCount;
        private Vector2 m_position;
        private WeaponData m_currentWeapon;
        
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

            m_maxLifeCount = data.MaxLifeCount;
            CurrentLifeCount = m_maxLifeCount;
            
            Debug.Log($"[PlayerModel] 초기화 완료 (Life: {m_maxLifeCount})");
            if (data.DefaultWeapon != null)
            {
                Debug.Log($"[PlayerModel] 기본 무기 장착: {data.DefaultWeapon.WeaponName}");
                SetWeapon(data.DefaultWeapon);
            }
            else
            {
                Debug.LogWarning("[PlayerModel] PlayerData에 기본 무기가 설정되어 있지 않습니다.");
            }
        }

        /// <summary>
        /// [설명]: 플레이어에게 데미지를 입힙니다. 데미지 적용 시 설정된 시간만큼 무적 상태가 됩니다.
        /// </summary>
        /// <param name="damage">입힐 데미지 (현재는 하트 시스템이므로 1로 처리)</param>
        /// <param name="invincibilityDuration">무적 지속 시간</param>
        public void TakeDamage(int damage, float invincibilityDuration = 0.5f)
        {
            if (IsInvincible)
            {
                // Debug.Log($"[PlayerModel] TakeDamage 무시 (무적 상태)");
                return;
            }

            Debug.Log($"[PlayerModel] TakeDamage 수령: 현재 생명={CurrentLifeCount}");

            // 무적 타이머 설정
            m_invincibilityTimer = invincibilityDuration;

            // 로직: 생명력 감소 (현재는 하트 1개 고정 감소)
            CurrentLifeCount -= 1;
            // Debug.Log($"[PlayerModel] 생명 감소 완료 -> 현재: {CurrentLifeCount}/{m_maxLifeCount}");
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
            Debug.Log($"[PlayerModel] 무기 장착: {(weapon != null ? weapon.WeaponName : "없음")}");
            OnWeaponChanged?.Invoke(weapon);
        }
        #endregion
    }
}
