using UnityEngine;
using TowerBreakers.Enemy.Data;
using TowerBreakers.Enemy.View;

namespace TowerBreakers.Enemy.Logic
{
    /// <summary>
    /// [설명]: 적 유닛의 공통적인 이동 및 전진 로직을 관리하는 정적 헬퍼 클래스입니다.
    /// </summary>
    public static class EnemyMovementHelper
    {
        /// <summary>
        /// [설명]: 전진 로직을 실행합니다. 애니메이션 상태 전이 및 플레이어 위치 동기화를 포함합니다.
        /// </summary>
        /// <param name="view">적 뷰</param>
        /// <param name="data">적 데이터</param>
        /// <param name="pushLogic">밀기 로직</param>
        /// <param name="isMoving">현재 이동 상태 (ref)</param>
        public static void ExecuteMovement(EnemyView view, EnemyData data, EnemyPushLogic pushLogic, ref bool isMoving)
        {
            if (pushLogic == null || pushLogic.PlayerReceiver == null) return;

            float gap = 1.1f;
            float moveDelta = data.MoveSpeed * Time.deltaTime;

            // [개선]: 전체 그룹이 아닌 개별/전파식 차단 상태 확인
            bool isBlocked = pushLogic.IsBlocked(gap);

            // 애니메이션 상태 제어
            bool shouldMove = !isBlocked;
            if (isMoving != shouldMove)
            {
                isMoving = shouldMove;
                view.PlayAnimation(isMoving ? global::PlayerState.MOVE : global::PlayerState.IDLE);
                
                var animator = view.CachedAnimator;
                if (animator != null)
                {
                    animator.Play(0, -1, 0f);
                }
            }

            // 이동 처리
            if (!isBlocked)
            {
                var pos = view.transform.position;
                pos.x -= moveDelta;

                // [중요]: 앞의 적을 앞지를 수 없도록 강제 간격 유지
                if (pushLogic.AheadEnemy != null)
                {
                    float minX = pushLogic.AheadEnemy.transform.position.x + pushLogic.TrainSpacing;
                    if (pos.x < minX)
                    {
                        pos.x = minX;
                        isBlocked = true;
                    }
                }

                view.transform.position = pos;

                // 플레이어와 실제로 닿아 있는 유닛이 위치 동기화 수행
                if (pushLogic.IsTouchingPlayer(gap + 0.1f))
                {
                    pushLogic.PlayerReceiver.SyncPositionDelta(-moveDelta);
                }
            }
            else
            {
                // 막혀 있더라도 플레이어와 압착 중이라면 압박 상태 유지
                if (pushLogic.IsTouchingPlayer(gap + 0.1f) && pushLogic.PlayerReceiver.IsAtWall)
                {
                    pushLogic.PlayerReceiver.SyncPositionDelta(0f);
                }
            }
            
            // 타격 밀기 시도 (TryPushPlayer 내부에서 거리 및 중복 체크 수행)
            pushLogic.TryPushPlayer();
        }
    }
}
