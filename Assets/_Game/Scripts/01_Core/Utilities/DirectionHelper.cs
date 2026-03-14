using UnityEngine;

namespace TowerBreakers.Core.Utilities
{
    /// <summary>
    /// [설명]: 방향 계산, 회전, 좌우 반전(Facing) 등 공간 벡터 관련 공통 로직을 관리하는 유틸리티 클래스입니다.
    /// </summary>
    public static class DirectionHelper
    {
        #region 공개 메서드
        /// <summary>
        /// [설명]: 2D 방향 벡터를 기반으로 Z축 회전값(Quaternion)을 계산합니다.
        /// </summary>
        /// <param name="direction">이동 또는 조준 방향</param>
        /// <returns>Z축 회전이 적용된 Quaternion</returns>
        public static Quaternion ToRotation(Vector2 direction)
        {
            if (direction == Vector2.zero) return Quaternion.identity;
            
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            return Quaternion.Euler(0f, 0f, angle);
        }

        /// <summary>
        /// [설명]: 타겟의 위치에 따라 캐릭터를 좌우로 회전시킵니다. (Y축 180도 전환 방식)
        /// </summary>
        /// <param name="selfX">자신의 X 위치</param>
        /// <param name="targetX">타겟의 X 위치</param>
        /// <returns>결정된 Y축 회전 Quaternion</returns>
        public static Quaternion FaceTarget(float selfX, float targetX)
        {
            // 우측을 바라볼 때 180도, 좌측을 바라볼 때 0도 (프로젝트 스프라이트 기본 방향 기준)
            float yRotation = (targetX > selfX) ? 180f : 0f;
            return Quaternion.Euler(0f, yRotation, 0f);
        }

        /// <summary>
        /// [설명]: 트랜스폼의 회전 상태를 기반으로 현재 바라보고 있는 방향(1 또는 -1)을 반환합니다.
        /// </summary>
        /// <param name="transform">검사할 대상 트랜스폼</param>
        /// <returns>우측: 1, 좌측: -1 (Y축 180도 회전 시 우측으로 간주)</returns>
        public static float GetFacingSign(Transform transform)
        {
            if (transform == null) return 1f;

            // Y축 회전값을 기준으로 판단 (0 내외면 좌측(-1), 180 내외면 우측(1))
            float yRot = transform.eulerAngles.y;
            return (Mathf.Abs(Mathf.DeltaAngle(yRot, 180f)) < 45f) ? 1f : -1f;
        }

        /// <summary>
        /// [설명]: 특정 방향으로 바라보는 Quaternion을 반환합니다.
        /// </summary>
        /// <param name="isRight">우측을 바라보는지 여부</param>
        /// <returns>회전값</returns>
        public static Quaternion GetFacingRotation(bool isRight)
        {
            return Quaternion.Euler(0f, isRight ? 180f : 0f, 0f);
        }
        #endregion
    }
}
