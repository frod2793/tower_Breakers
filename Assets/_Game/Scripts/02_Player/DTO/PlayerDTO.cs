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
        [Tooltip("대쉬 속도")]
        public float DashSpeed = 10f;

        [Tooltip("대쉬 쿨타임")]
        public float DashCooldown = 2f;

        [Tooltip("패링 쿨타임")]
        public float ParryCooldown = 3f;

        [Tooltip("공격 범위")]
        public float AttackRange = 2f;

        [Tooltip("공격 쿨타임")]
        public float AttackCooldown = 0.5f;

        [Tooltip("패링 후 퇴각 속도")]
        public float RetreatSpeed = 8f;

        [Tooltip("패링 지속 시간")]
        public float ParryDuration = 0.5f;

        [Tooltip("왼쪽 벽 위치")]
        public float LeftWallX = -8f;

        [Tooltip("패링 시 적 정지 시간")]
        public float EnemyStopDuration = 0.5f;
    }

    /// <summary>
    /// [설명]: 플레이어의 실시간 상태 정보를 담는 DTO 클래스입니다.
    /// </summary>
    public class PlayerStateDTO
    {
        public Vector2 Position;
        public bool IsDashing;
        public bool IsParrying;
        public bool IsAttacking;
        public bool IsRetreating;
        public float LastDashTime = -100f;
        public float LastParryTime = -100f;
        public float LastAttackTime = -100f;
        
        public int Health;
        public int MaxHealth;
    }
    #endregion
}
