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
            float gap = 1.1f;
            float moveDelta = data.MoveSpeed * Time.deltaTime;

            // [성능]: 그룹 전체의 차단 여부를 확인 (리더 기반)
            bool isGroupBlocked = pushLogic.IsGroupBlocked(gap);

            // 애니메이션 상태 제어
            bool shouldMove = !isGroupBlocked;
            if (isMoving != shouldMove)
            {
                isMoving = shouldMove;
                view.PlayAnimation(isMoving ? global::PlayerState.MOVE : global::PlayerState.IDLE);
                
                // [성능]: 캐싱된 애니메이터를 사용하여 normalizedTime 리셋
                var animator = view.CachedAnimator;
                if (animator != null)
                {
                    animator.Play(0, -1, 0f);
                }
            }

            // 그룹이 움직일 수 있을 때만 전진
            if (!isGroupBlocked)
            {
                // 1. 적 유닛 전진
                // [성능]: new Vector3() 할당 없이 transform.position 직접 수정
                var pos = view.transform.position;
                pos.x -= moveDelta;
                view.transform.position = pos;

                // 2. 플레이어 위치 동기화 수동 밀기
                // 리더 적(맨 앞)만 플레이어를 밀도록 하여 중복 계산 방지
                var leader = pushLogic.GetLeader();
                if (leader == pushLogic && pushLogic.IsTouchingPlayer(gap + 0.1f))
                {
                    // [최적화]: SyncPositionDelta로 명칭 변경됨 (적 전진과 1:1 동기화)
                    pushLogic.PlayerReceiver?.SyncPositionDelta(-moveDelta);
                }
            }
            else
            {
                // [신규]: 그룹이 막혀있더라도(교착 상태), 리더가 플레이어와 닿아있다면
                // 지속적으로 압력을 가하고 있음을 알림 (데미지 발행용)
                // IsLeaderPushingAtWall(gap) 내부에서 IsTouchingPlayer(gap + 0.2f) 즉 1.3f를 사용하여 정지 조건과 일치함
                if (pushLogic.IsLeaderPushingAtWall(gap))
                {
                    // [로그]: 교착 상태 감지 및 가상 밀기 수행 (필요 시 주석 해제)
                    // Debug.Log("[EnemyMovementHelper] 교착 상태(적 정지) 감지 - 가상 밀기(Force Push) 수행");
                    // 위치 변위는 0이지만, 벽에 닿아있는 상태에서 이벤트를 트리거하기 위해 호출
                    pushLogic.PlayerReceiver.SyncPositionDelta(0f);
                }
            }
            
            // 힘 기반 밀기 로직은 리더만 수행
            if (pushLogic.GetLeader() == pushLogic)
            {
                pushLogic.TryPushPlayer();
            }
        }
    }
}
