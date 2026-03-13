using UnityEngine;
using DG.Tweening;
using TowerBreakers.Enemy.Logic;
using TowerBreakers.Player.Data;
using TowerBreakers.Player.View;

namespace TowerBreakers.Player.Logic
{
    /// <summary>
    /// [설명]: 플레이어의 도약(Leap) 상태입니다. 선두 적의 위치 앞까지 순식간에 이동합니다.
    /// </summary>
    public class PlayerLeapState : IPlayerState
    {
        #region 내부 필드
        private readonly PlayerView m_view;
        private readonly PlayerModel m_model;
        private readonly PlayerData m_data;
        private readonly PlayerStateMachine m_stateMachine;

        // [최적화]: GC 할당을 방지하기 위한 정적 히트 버퍼
        private static readonly Collider2D[] s_hitBuffer = new Collider2D[32];
        #endregion

        public PlayerLeapState(PlayerView view, PlayerModel model, PlayerData m_data, PlayerStateMachine stateMachine)
        {
            m_view = view;
            m_model = model;
            this.m_data = m_data;
            m_stateMachine = stateMachine;
        }

        public void OnEnter()
        {
            Debug.Log("[PlayerLeapState] 도약 시작");
            ExecuteLeap();
        }

        public void OnExit() { }

        public void OnTick() { }

        private void ExecuteLeap()
        {
            // 1. 전방의 가장 가까운 적 탐색 (사거리 약 10m)
            float detectionRange = 10f;
            Vector2 origin = m_view.transform.position;
            int enemyLayer = LayerMask.GetMask("Enemy");
            
            // 레이어가 정의되지 않았을 경우를 대비한 폴백 (모든 레이어 탐색)
            if (enemyLayer == 0) enemyLayer = -1;

            int hitCount = Physics2D.OverlapBoxNonAlloc(origin + Vector2.right * (detectionRange * 0.5f), new Vector2(detectionRange, 2f), 0f, s_hitBuffer, enemyLayer);
            
            float targetX = origin.x + m_data.LeapDistance; // 기본값
            float minDistance = float.MaxValue;
            bool foundEnemy = false;

            for (int i = 0; i < hitCount; i++)
            {
                var col = s_hitBuffer[i];
                var controller = col.GetComponent<EnemyController>();
                if (controller != null && !controller.IsDead)
                {
                    float dist = col.transform.position.x - origin.x;
                    if (dist > 0.5f && dist < minDistance) // 플레이어 바로 앞은 제외
                    {
                        minDistance = dist;
                        // 적의 위치에서 약 1.2m 앞 (공격 사거리 즈음)까지만 이동
                        targetX = col.transform.position.x - 1.2f;
                        foundEnemy = true;
                    }
                }
            }

            if (foundEnemy)
            {
                Debug.Log($"[PlayerLeapState] 적 발견! 대시 목표: {targetX}");
            }
            else
            {
                Debug.Log($"[PlayerLeapState] 적 없음. 최대 거리 도약: {targetX}");
            }

            // 2. 수평 대시 연출 (DOJump -> DOMoveX)
            // 지면에 붙어서 빠르게 달려가는 느낌을 줍니다. (0.25초)
            m_view.transform.DOMoveX(targetX, 0.25f)
                .SetEase(Ease.OutQuad)
                .OnUpdate(() => m_model.Position = m_view.transform.position)
                .OnComplete(() => m_stateMachine.ChangeState<PlayerIdleState>());

            // 애니메이션은 달리기(Move) 재생
            m_view.PlayAnimation(global::PlayerState.MOVE, 0); 
        }
    }
}
