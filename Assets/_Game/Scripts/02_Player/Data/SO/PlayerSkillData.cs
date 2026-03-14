using UnityEngine;
using UnityEngine.Serialization;

namespace TowerBreakers.Player.Data.SO
{
    /// <summary>
    /// [설명]: 플레이어의 스킬 데이터를 관리하는 ScriptableObject입니다.
    /// </summary>
    [CreateAssetMenu(fileName = "NewPlayerSkillData", menuName = "TowerBreakers/Player Skill Data")]
    public class PlayerSkillData : ScriptableObject
    {
        #region 에디터 설정

        #region 공통 스킬 설정
        [Header("공통 스킬 설정")]
        [SerializeField, Tooltip("스킬 1 (원거리/범위) 데미지 배율")]
        private float m_skill1Multiplier = 1.5f;

        [SerializeField, Tooltip("스킬 2 (강타) 데미지 배율")]
        private float m_skill2Multiplier = 3.5f;

        [SerializeField, Tooltip("스킬 3 (배시) 데미지 배율")]
        private float m_skill3Multiplier = 1.2f;

        [Header("스킬 쿨다운 설정")]
        [SerializeField, Tooltip("스킬 1 쿨다운 (초)")]
        private float m_skill1Cooldown = 1.0f;

        [SerializeField, Tooltip("스킬 2 쿨다운 (초)")]
        private float m_skill2Cooldown = 1.0f;

        [SerializeField, Tooltip("스킬 3 쿨다운 (초)")]
        private float m_skill3Cooldown = 1f;
        #endregion

        #region 스킬 1 - 대시/돌진 설정
        [Header("스킬 1: 일반/특수 몹 구분")]
        [SerializeField, Tooltip("스킬 1의 일반 몹 타격 한계 (即死)")]
        [FormerlySerializedAs("m_skill1NormalKillLimit")]
        private int m_skill1NormalHitLimit = 3;

        [Header("스킬 1 - 대시 설정")]
        [SerializeField, Tooltip("스킬 1 돌진 사용 활성화 여부")]
        private bool m_skill1DashEnabled = false;

        [SerializeField, Tooltip("돌진 최대 거리 (유닛 단위)")]
        private float m_skill1DashMaxDistance = 6.0f;

        [SerializeField, Tooltip("돌진 속도 (유닛/초) - 거리와 무관하게 일정한 속도감 제공")]
        private float m_skill1DashSpeed = 24.0f;

        [Header("스킬 1 - 윈드스톰 데미지 배율")]
        [SerializeField, Tooltip("일반 적(Normal)에게 적용할 데미지 배율 - 즉사 판정")]
        private float m_skill1NormalDamageMultiplier = 10f;

        [SerializeField, Tooltip("특수 적(특화/보스)에게 적용할 데미지 배율")]
        private float m_skill1SpecialDamageMultiplier = 2f;

        [SerializeField, Tooltip("오브젝트에 적용할 데미지 배율 (1 = 기본 데미지)")]
        private float m_skill1ObjectDamageMultiplier = 1f;

        [SerializeField, Tooltip("발도술 카메라 확대 양 (작아질수록 확대)")]
        private float m_skill1CameraZoomDelta = 0.8f;

        [SerializeField, Tooltip("발도술 카메라 줌 시간 (초)")]
        private float m_skill1CameraZoomDuration = 0.25f;

        [SerializeField, Tooltip("발도술 도중 시간 축소 비율 (0.0~1.0)")]
        private float m_skill1TimeScaleDuringDash = 0.5f;

        #endregion

        #region 스킬 2 - 가이드 미사일 설정
        [Header("스킬 2: 무차별 폭격 미사일")]
        [SerializeField, Tooltip("미사일 개수")]
        private int m_skill2MissileCount = 3;

        [SerializeField, Tooltip("미사일 속도")]
        private float m_skill2MissileSpeed = 8.0f;

        [SerializeField, Tooltip("미사일 추적 회전 속도 (초당 각도)")]
        private float m_skill2MissileTurnSpeed = 180.0f;

        [Header("스킬 2 - 수직 발사 설정")]
        [SerializeField, Tooltip("발사 상승 높이 (플레이어 기준)")]
        private float m_skill2LaunchHeight = 5.0f;

        [SerializeField, Tooltip("상승 페이즈 지속 시간 (초)")]
        private float m_skill2LaunchDuration = 0.4f;

        [Header("스킬 2 - 이동 효과")]
        [SerializeField, Tooltip("사인파 이동 진폭")]
        private float m_skill2MissileWaveAmplitude = 0.5f;

        [SerializeField, Tooltip("사인파 이동 빈도")]
        private float m_skill2MissileWaveFrequency = 2.0f;

        [SerializeField, Tooltip("잔상 간격 (초)")]
        private float m_skill2MissileAfterimageInterval = 0.1f;

        [SerializeField, Tooltip("미사일 프리팹")]
        private GameObject m_skill2MissilePrefab;

        [SerializeField, Tooltip("미사일 존재 시간 (초)")]
        private float m_skill2MissileLifetime = 5.0f;
        #endregion

        #region 스킬 3 - 참격 설정
        [Header("스킬 3: 관통 참격")]
        [SerializeField, Tooltip("참격 이동 속도")]
        private float m_skill3SlashSpeed = 15.0f;

        [SerializeField, Tooltip("참격 최대 이동 거리")]
        private float m_skill3SlashDistance = 10.0f;

        [SerializeField, Tooltip("적 이동 속도 저하 비율 (0.5 = 50% 감소)")]
        private float m_skill3SlowMultiplier = 0.5f;

        [SerializeField, Tooltip("디버프 지속 시간 (초)")]
        private float m_skill3SlowDuration = 2.0f;

        [SerializeField, Tooltip("参격 프리팹")]
        private GameObject m_skill3SlashPrefab;

        [SerializeField, Tooltip("参격 존재 시간 (초)")]
        private float m_skill3SlashLifetime = 1.0f;

        [SerializeField, Tooltip("参격 넉백 거리")]
        private float m_skill3KnockbackDistance = 2.0f;

        [SerializeField, Tooltip("参격 넉백 지속 시간")]
        private float m_skill3KnockbackDuration = 0.25f;

        [SerializeField, Tooltip("参격 기절 지속 시간")]
        private float m_skill3StunDuration = 0.5f;
        #endregion

        #endregion

        #region 프로퍼티

        #region 공통 스킬 프로퍼티
        public float Skill1Multiplier => m_skill1Multiplier;
        public float Skill2Multiplier => m_skill2Multiplier;
        public float Skill3Multiplier => m_skill3Multiplier;
        public float Skill1Cooldown => m_skill1Cooldown;
        public float Skill2Cooldown => m_skill2Cooldown;
        public float Skill3Cooldown => m_skill3Cooldown;
        #endregion

        #region 스킬 1 프로퍼티
        public int Skill1NormalHitLimit => m_skill1NormalHitLimit;
        public bool Skill1DashEnabled => m_skill1DashEnabled;
        public float Skill1DashMaxDistance => m_skill1DashMaxDistance;
        public float Skill1DashSpeed => m_skill1DashSpeed;
        public float Skill1NormalDamageMultiplier => m_skill1NormalDamageMultiplier;
        public float Skill1SpecialDamageMultiplier => m_skill1SpecialDamageMultiplier;
        public float Skill1ObjectDamageMultiplier => m_skill1ObjectDamageMultiplier;
        public float Skill1CameraZoomDelta => m_skill1CameraZoomDelta;
        public float Skill1CameraZoomDuration => m_skill1CameraZoomDuration;
        public float Skill1TimeScaleDuringDash => m_skill1TimeScaleDuringDash;
        #endregion

        #region 스킬 2 프로퍼티
        public int Skill2MissileCount => m_skill2MissileCount;
        public float Skill2MissileSpeed => m_skill2MissileSpeed;
        public float Skill2MissileTurnSpeed => m_skill2MissileTurnSpeed;
        public float Skill2LaunchHeight => m_skill2LaunchHeight;
        public float Skill2LaunchDuration => m_skill2LaunchDuration;
        public float Skill2MissileWaveAmplitude => m_skill2MissileWaveAmplitude;
        public float Skill2MissileWaveFrequency => m_skill2MissileWaveFrequency;
        public float Skill2MissileAfterimageInterval => m_skill2MissileAfterimageInterval;
        public GameObject Skill2MissilePrefab => m_skill2MissilePrefab;
        public float Skill2MissileLifetime => m_skill2MissileLifetime;
        #endregion

        #region 스킬 3 프로퍼티
        public float Skill3SlashSpeed => m_skill3SlashSpeed;
        public float Skill3SlashDistance => m_skill3SlashDistance;
        public float Skill3SlowMultiplier => m_skill3SlowMultiplier;
        public float Skill3SlowDuration => m_skill3SlowDuration;
        public GameObject Skill3SlashPrefab => m_skill3SlashPrefab;
        public float Skill3SlashLifetime => m_skill3SlashLifetime;
        public float Skill3KnockbackDistance => m_skill3KnockbackDistance;
        public float Skill3KnockbackDuration => m_skill3KnockbackDuration;
        public float Skill3StunDuration => m_skill3StunDuration;
        #endregion

        #endregion
    }
}
