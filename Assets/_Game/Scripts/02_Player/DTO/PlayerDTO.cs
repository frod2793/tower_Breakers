using System;
using UnityEngine;

namespace TowerBreakers.Player.DTO
{
    #region 데이터 모델 (DTO)
    /// <summary>
    /// [설명]: 플레이어의 초기 설정값을 담는 DTO 클래스입니다.
    /// </summary>
    [Serializable]
    public class PlayerConfigDTO
    {
        #region 기본 설정
        [Header("대쉬 설정")]
        [Tooltip("대쉬 속도")]
        public float DashSpeed = 10f;

        [Tooltip("대쉬 쿨타임")]
        public float DashCooldown = 2f;

        [Tooltip("대쉬 최소 거리 (적과의 거리가 이 값 이상일 때만 대시 가능)")]
        public float DashMinDistance = 1.5f;

        [Tooltip("대시 정지 거리 (적 앞의 해당 거리에서 멈춤)")]
        public float DashStopDistance = 1.5f;

        [Header("패링 설정")]
        [Tooltip("패링 쿨타임")]
        public float ParryCooldown = 3f;

        [Tooltip("패링 후 퇴각 속도 (항상 왼쪽 벽까지)")]
        public float ParryRetreatSpeed = 12f;

        [Tooltip("일반 스킬용 후퇴 속도")]
        public float SkillRetreatSpeed = 8f;

        [Tooltip("패링 지속 시간")]
        public float ParryDuration = 0.5f;

        [Tooltip("패링 시 적 정지 시간")]
        public float EnemyStopDuration = 0.5f;

        [Tooltip("패링 시 적을 밀어내는 힘")]
        public float ParryPushForce = 10f;

        [Tooltip("패링 발동 가능 최소 거리 (적이 이 거리 안에 있어야 패링이 작동)")]
        public float ParryActivationRange = 8.0f;

        [Tooltip("패링 시 백덤블링 점프 높이")]
        public float ParryJumpHeight = 3.0f;

        [Tooltip("패링 시 압착 피해 면역 지속 시간 (초)")]
        public float ParryImmunityDuration = 1.0f;

        [Header("공격 설정")]
        [Tooltip("공격 범위")]
        public float AttackRange = 2f;

        [Tooltip("공격 쿨타임")]
        public float AttackCooldown = 0.5f;

        [Header("질풍참 설정")]
        [Tooltip("질풍참 데미지 배율")]
        public float WindstormDamageMultiplier = 2.0f;

        [Tooltip("질풍참 기 모으기 시간 (슬로우 모션 지속 시간)")]
        public float WindstormChargeDuration = 0.5f;

        [Tooltip("질풍참 기 모으기 시 카메라 FOV (Perspective 모드용, 기본 60기준, 작을수록 줌인)")]
        public float WindstormZoomFOV = 40f;

        [Tooltip("질풍참 기 모으기 시 카메라 크기 (Orthographic 모드용, 작을수록 줌인)")]
        public float WindstormZoomOrthoSize = 2.5f;

        [Tooltip("질풍참 최대 타격 대상 수")]
        public int WindstormMaxTargets = 3;

        [Tooltip("질풍참 대쉬 속도 (기본 대쉬보다 빠르게 설정 권장)")]
        public float WindstormDashSpeed = 25f;

        [Tooltip("질풍참 쿨타임")]
        public float WindstormCooldown = 5.0f;

        [Header("상태 판정 임계값 (Thresholds)")]
        [Tooltip("대쉬/퇴각 이동 완료 판정 거리")]
        public float MovementArrivalThreshold = 0.05f;

        [Tooltip("밀림 상태 지속 시간")]
        public float PushDuration = 0.1f;

        [Tooltip("애니메이션 전환을 위한 이동 판정 거리")]
        public float animMovementThreshold = 0.01f;

        [Header("연출 설정 (Visuals)")]
        [Tooltip("시각적 위치 동기화 보간 속도")]
        public float VisualLerpSpeed = 100f;

        [Tooltip("백덤블링 시 회전 각도")]
        public float BackflipRotationDegrees = 720f;

        [Tooltip("백덤블링 판정을 위한 최소 이동 거리")]
        public float BackflipDistanceThreshold = 0.1f;

        [Tooltip("질풍참 대쉬 연출 대기 시간 (ms)")]
        public int WindstormDashDelayMs = 150;

        [Tooltip("질풍참 공격 애니메이션 대기 시간 (ms)")]
        public int WindstormAttackDelayMs = 300;

        [Header("적 감지 및 판정 (Detection)")]
        [Tooltip("패링/공격 시 타겟팅할 적의 Y축 허용 범위")]
        public float EnemyDetectionYRange = 2.0f;

        [Tooltip("패링 시 뒤쪽 적까지 포함할 X축 오프셋")]
        public float EnemyDetectionXOffset = 0.5f;

        [Tooltip("공격 범위 판정 여유분")]
        public float AttackRangeBuffer = 0.2f;
        #endregion

        #region 이동 및 밀림 설정
        [Tooltip("왼쪽 벽 위치")]
        public float LeftWallX = -8f;

        [Tooltip("패링 범위")]
        public float ParryRange = 2.0f;


        [Tooltip("벽 도달 시 체력 감소량")]
        public int DamagePerHit = 1;

        [Tooltip("밀림 후 쿨다운 (초)")]
        public float DamageCooldown = 1f;
        #endregion
    }

    /// <summary>
    /// [설명]: 플레이어의 실시간 상태 정보를 담는 DTO 클래스입니다.
    /// </summary>
    public class PlayerStateDTO
    {
        public Vector2 Position;
        public Vector2 LocalPosition; // [추가]: 로컬 좌표 로그 출력을 위한 필드
        public Vector2 TargetPosition; // [추가]: 이동형 액션(Dash, Retreat)의 목표 좌표
        public bool IsDashing;
        public bool IsParrying;
        public bool IsAttacking;
        public bool IsBeingPushed;
        public bool IsRetreating;
        public bool IsBackflip; // [추가]: 백덤블링 수행 여부
        public bool IsWindstormDash; // [추가]: 질풍참 수행 여부
        public bool IsCharging; // [추가]: 기 모으는 중 (이동 정지)
        public Vector2 ParryStartPosition; // [추가]: 패링 시작 시점의 위치
        public float ParryReferenceX; // [추가]: 백덤블링 연출 기준 X 좌표 (m_parryReference)
        public float LastDashTime = -100f;
        public float LastParryTime = -100f;
        public float LastAttackTime = -100f;
        
        public int Health;
        public int MaxHealth;
    }
    #endregion
}
