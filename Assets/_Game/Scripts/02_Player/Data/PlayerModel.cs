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
        private int m_currentHp;
        private int m_maxHp;
        private Vector2 m_position;
        private WeaponData m_currentWeapon;
        #endregion

        #region 프로퍼티
        public int CurrentHp 
        { 
            get => m_currentHp; 
            private set
            {
                m_currentHp = Math.Max(0, Math.Min(m_maxHp, value));
                OnHpChanged?.Invoke(m_currentHp, m_maxHp);
            }
        }
        
        public int MaxHp => m_maxHp;
        public Vector2 Position { get => m_position; set => m_position = value; }
        public bool IsDead => m_currentHp <= 0;
        public WeaponData CurrentWeapon => m_currentWeapon;

        // 보정된 스탯 프로퍼티 (WeaponData는 ScriptableObject이므로 ?. 대신 null 체크 사용)
        private float GetWeaponModifier(float modifier) => m_currentWeapon != null ? modifier : 1.0f;
        public int FinalAttackPower(int basePower) => (int)(basePower * GetWeaponModifier(m_currentWeapon != null ? m_currentWeapon.AttackPowerModifier : 1.0f));
        public float FinalAttackRange(float baseRange) => baseRange * GetWeaponModifier(m_currentWeapon != null ? m_currentWeapon.AttackRangeModifier : 1.0f);
        public float FinalAttackSpeed(float baseSpeed) => baseSpeed * GetWeaponModifier(m_currentWeapon != null ? m_currentWeapon.AttackSpeedModifier : 1.0f);
        #endregion

        #region 이벤트
        /// <summary>
        /// [설명]: 체력 변경 시 발행되는 이벤트 (현재 체력, 최대 체력)
        /// </summary>
        public event Action<int, int> OnHpChanged;

        /// <summary>
        /// [설명]: 무기 변경 시 발행되는 이벤트
        /// </summary>
        public event Action<WeaponData> OnWeaponChanged;
        #endregion

        #region 초기화 및 비즈니스 로직
        public void Initialize(PlayerData data)
        {
            if (data == null)
            {
                Debug.LogError("[PlayerModel] 초기화 실패: PlayerData가 null입니다.");
                return;
            }

            m_maxHp = data.MaxHp;
            CurrentHp = m_maxHp;
            
            Debug.Log($"[PlayerModel] 초기화 완료 (HP: {m_maxHp})");
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

        public void TakeDamage(int damage)
        {
            if (IsDead) return;
            CurrentHp -= damage;
        }

        public void Heal(int amount)
        {
            if (IsDead) return;
            CurrentHp += amount;
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
