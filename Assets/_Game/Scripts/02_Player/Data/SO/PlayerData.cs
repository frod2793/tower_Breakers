using UnityEngine;
using UnityEngine.Serialization;

namespace TowerBreakers.Player.Data.SO
{
    /// <summary>
    /// [설명]: 플레이어의 기본 능력치 및 설정을 저장하는 데이터 에셋입니다.
    /// </summary>
    [CreateAssetMenu(fileName = "NewPlayerData", menuName = "TowerBreakers/Player Data")]
    public class PlayerData : ScriptableObject
    {
        #region 에디터 설정
        [Header("기본 스탯")]
        [SerializeField, FormerlySerializedAs("m_maxHp"), Tooltip("최대 생명 횟수 (피격 가능 횟수)")]
        private int m_maxLifeCount = 3;

        [SerializeField, Tooltip("기본 공격력")]
        private int m_attackPower = 10;

        [SerializeField, Tooltip("공격 사거리")]
        private float m_attackRange = 1.5f;

        [SerializeField, Tooltip("공격 속도 (초당 공격 횟수)")]
        private float m_attackSpeed = 1.0f;

        [SerializeField, Tooltip("기본 이동 속도")]
        private float m_moveSpeed = 3.0f;

        [Header("전투 설정")]
        [SerializeField, Tooltip("밀림 저항 (0.0 ~ 1.0)")]
        private float m_pushResistance = 0.2f;

        [SerializeField, Tooltip("도약 거리")]
        private float m_leapDistance = 5.0f;

        [Header("스킬 설정")]
        [SerializeField, Tooltip("스킬 1 (원거리/범위) 데미지 배율")]
        private float m_skill1Multiplier = 1.5f;

        [SerializeField, Tooltip("스킬 2 (강타) 데미지 배율")]
        private float m_skill2Multiplier = 3.5f;

        [SerializeField, Tooltip("스킬 3 (배시) 데미지 배율")]
        private float m_skill3Multiplier = 1.2f;

        [Header("방어 설정")]
        [SerializeField, Tooltip("방어 가능 거리 (이 범위 내의 적만 스턴에 걸림)")]
        private float m_defendRange = 2.5f;

        [SerializeField, Tooltip("방어 시 적 행렬 밀어내기 거리")]
        private float m_defendPushbackDistance = 3.0f;

        [SerializeField, Tooltip("벽 압착 데미지 배율 (밀린 거리 × 배율 = 데미지)")]
        private float m_wallCrushDamageMultiplier = 10f;

        [Header("장비 설정")]
        [SerializeField, Tooltip("게임 시작 시 기본으로 장착될 무기")]
        private WeaponData m_defaultWeapon;
        #endregion

        #region 프로퍼티
        // Note: The TakeDamage method and related properties (IsDead, CurrentLifeCount) typically belong to a runtime PlayerModel class,
        // not a ScriptableObject PlayerData. This implementation is based on the provided instruction.
        public bool IsDead { get; private set; } // Placeholder for IsDead, assuming it exists in the context where TakeDamage is used
        public int CurrentLifeCount { get; private set; } // Placeholder for CurrentLifeCount, assuming it exists in the context where TakeDamage is used

        public void TakeDamage(int damage)
        {
            if (IsDead) return;
            // [변경]: 데미지 수치와 상관없이 1회 피격 시 생명 1 감소
            CurrentLifeCount -= 1;
        }
        public int MaxLifeCount => m_maxLifeCount;
        public int AttackPower => m_attackPower;
        public float AttackRange => m_attackRange;
        public float AttackSpeed => m_attackSpeed;
        public float MoveSpeed => m_moveSpeed;
        public float PushResistance => m_pushResistance;
        public float LeapDistance => m_leapDistance;
        public float Skill1Multiplier => m_skill1Multiplier;
        public float Skill2Multiplier => m_skill2Multiplier;
        public float Skill3Multiplier => m_skill3Multiplier;
        public float DefendRange => m_defendRange;
        public float DefendPushbackDistance => m_defendPushbackDistance;
        public float WallCrushDamageMultiplier => m_wallCrushDamageMultiplier;
        public WeaponData DefaultWeapon => m_defaultWeapon;
        #endregion
    }
}
