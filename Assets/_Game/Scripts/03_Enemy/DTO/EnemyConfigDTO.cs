using System;
using UnityEngine;

namespace TowerBreakers.Enemy.DTO
{
    /// <summary>
    /// [설명]: 적 전체의 이동 및 물리 설정값을 담는 DTO 클래스입니다.
    /// BattleLifetimeScope에서 주입받아 사용합니다.
    /// </summary>
    [Serializable]
    public class EnemyConfigDTO
    {
        #region 이동 및 물리 설정
        [Tooltip("적 기본 이동 속도")]
        public float MoveSpeed = 2f;

        [Tooltip("플레이어 밀어내기 힘")]
        public float PushForce = 5f;

        [Tooltip("밀어내기 범위 (센싱 반경)")]
        public float PushRange = 1.5f;

        [Tooltip("적 기차 행렬 간격")]
        public float TrainSpacing = 1.5f;

        [Tooltip("스폰 Y 오프셋")]
        public float SpawnYOffset = 0f;
        #endregion
    }
}
