using UnityEngine;

namespace TowerBreakers.Player.Data
{
    /// <summary>
    /// [설명]: 플레이어의 기본 능력치 및 설정을 저장하는 데이터 에셋입니다.
    /// </summary>
    [CreateAssetMenu(fileName = "NewPlayerData", menuName = "TowerBreakers/Player Data")]
    public class PlayerData : ScriptableObject
    {
        #region 에디터 설정
        [Header("기본 스탯")]
        [SerializeField, Tooltip("최대 체력")]
        private int m_maxHp = 100;

        [SerializeField, Tooltip("기본 공격력")]
        private int m_attackPower = 10;

        [SerializeField, Tooltip("공격 사거리")]
        private float m_attackRange = 1.5f;

        [SerializeField, Tooltip("공격 속도 (초당 공격 횟수)")]
        private float m_attackSpeed = 1.0f;

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

        [Header("장비 설정")]
        [SerializeField, Tooltip("게임 시작 시 기본으로 장착될 무기")]
        private WeaponData m_defaultWeapon;
        #endregion

        #region 프로퍼티
        public int MaxHp => m_maxHp;
        public int AttackPower => m_attackPower;
        public float AttackRange => m_attackRange;
        public float AttackSpeed => m_attackSpeed;
        public float PushResistance => m_pushResistance;
        public float LeapDistance => m_leapDistance;
        public float Skill1Multiplier => m_skill1Multiplier;
        public float Skill2Multiplier => m_skill2Multiplier;
        public float Skill3Multiplier => m_skill3Multiplier;
        public WeaponData DefaultWeapon => m_defaultWeapon;
        #endregion
    }
}
