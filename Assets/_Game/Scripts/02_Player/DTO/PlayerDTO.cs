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

        [Tooltip("패링 후 퇴각 속도")]
        public float RetreatSpeed = 8f;

        [Tooltip("패링 지속 시간")]
        public float ParryDuration = 0.5f;

        [Tooltip("패링 시 적 정지 시간")]
        public float EnemyStopDuration = 0.5f;

        [Tooltip("패링 시 적을 밀어내는 힘")]
        public float ParryPushForce = 10f;

        [Tooltip("패링 발동 가능 최소 거리 (적이 이 거리 안에 있어야 패링이 작동)")]
        public float ParryActivationRange = 8.0f;

        [Header("공격 설정")]
        [Tooltip("공격 범위")]
        public float AttackRange = 2f;

        [Tooltip("공격 쿨타임")]
        public float AttackCooldown = 0.5f;

        [Header("질풍참 설정")]
        [Tooltip("질풍참 데미지 배율")]
        public float WindstormDamageMultiplier = 2.0f;

        [Tooltip("질풍참 최대 타격 대상 수")]
        public int WindstormMaxTargets = 5;

        [Tooltip("질풍참 쿨타임")]
        public float WindstormCooldown = 5.0f;
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
        public float LastDashTime = -100f;
        public float LastParryTime = -100f;
        public float LastAttackTime = -100f;
        
        public int Health;
        public int MaxHealth;
    }
    #endregion
}
