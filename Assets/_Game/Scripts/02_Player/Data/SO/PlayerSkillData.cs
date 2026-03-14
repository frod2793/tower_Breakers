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
        private float m_skill1Cooldown = 3.0f;

        [SerializeField, Tooltip("스킬 2 쿨다운 (초)")]
        private float m_skill2Cooldown = 4.0f;

        [SerializeField, Tooltip("스킬 3 쿨다운 (초)")]
        private float m_skill3Cooldown = 3.5f;
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

        [SerializeField, Tooltip("발도술 카메라 확대 양 (작아질수록 확대)")]
        private float m_skill1CameraZoomDelta = 0.8f;

        [SerializeField, Tooltip("발도술 카메라 줌 시간 (초)")]
        private float m_skill1CameraZoomDuration = 0.25f;

        [SerializeField, Tooltip("발도술 도중 시간 축소 비율 (0.0~1.0)")]
        private float m_skill1TimeScaleDuringDash = 0.5f;
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
        public float Skill1CameraZoomDelta => m_skill1CameraZoomDelta;
        public float Skill1CameraZoomDuration => m_skill1CameraZoomDuration;
        public float Skill1TimeScaleDuringDash => m_skill1TimeScaleDuringDash;
        #endregion

        #endregion
    }
}
