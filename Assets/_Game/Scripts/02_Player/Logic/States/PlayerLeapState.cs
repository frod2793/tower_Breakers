using UnityEngine;
using DG.Tweening;
using TowerBreakers.Core.Interfaces;
using TowerBreakers.Player.Data.SO;
using TowerBreakers.Player.Data.Models;
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

        private PlayerPushReceiver m_pushReceiver;

        // [최적화]: GC 할당 및 문자열 파싱 방지를 위한 정적 캐싱 필드들
        private static readonly Collider2D[] s_hitBuffer = new Collider2D[32];
        private static readonly int s_targetLayer = LayerMask.GetMask("Enemy", "Object");
        private static readonly ContactFilter2D s_hitFilter = CreateHitFilter();

        private static ContactFilter2D CreateHitFilter()
        {
            ContactFilter2D filter = new ContactFilter2D();
            filter.SetLayerMask(s_targetLayer);
            filter.useLayerMask = true;
            filter.useTriggers = true;
            return filter;
        }
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
            // 도약 사용 중 벽 압착 판정 방지를 위해 밀림 수신 비활성화
            if (m_pushReceiver == null && m_view != null) m_view.TryGetComponent(out m_pushReceiver);
            if (m_pushReceiver != null) m_pushReceiver.IsClampingEnabled = false;

            if (m_view != null) m_view.SetAfterImage(true);
            ExecuteLeap();
        }

        public void OnExit() 
        {
            if (m_pushReceiver != null) m_pushReceiver.IsClampingEnabled = true;
            if (m_view != null) m_view.SetAfterImage(false);
        }

        public void OnTick() { }

        private void ExecuteLeap()
        {
            // 1. 전방의 가장 가까운 적 탐색 (사거리 약 10m)
            float detectionRange = 10f;
            Vector2 origin = m_view.transform.position;
            // [최적화]: 캐싱된 레이어 마스크와 필터를 사용하여 할당 제거
            int hitCount = Physics2D.OverlapBox(origin + Vector2.right * (detectionRange * 0.5f), new Vector2(detectionRange, 2f), 0f, s_hitFilter, s_hitBuffer);
            
            float targetX = origin.x + m_data.LeapDistance; // 기본값 (타겟 없을 시)
            float minDistance = float.MaxValue;
            bool foundTarget = false;

            for (int i = 0; i < hitCount; i++)
            {
                var col = s_hitBuffer[i];
                if (col == null) continue;

                var damageable = col.GetComponent<IDamageable>();
                if (damageable == null)
                {
                    damageable = col.GetComponentInParent<IDamageable>();
                }

                if (damageable != null && !damageable.IsDead)
                {
                    // [수정]: 타겟의 중심점이 아닌 왼쪽 경계(Bounds.min)를 기준으로 거리 계산하여 거대 오브젝트 통과 방지
                    float targetLeftEdge = col.bounds.min.x;
                    float distToEdge = targetLeftEdge - origin.x;

                    // 플레이어 전방에 있는 타겟 중 가장 가까운 것 선택
                    if (distToEdge > 0.1f && distToEdge < minDistance)
                    {
                        minDistance = distToEdge;
                        
                        // 타켓의 왼쪽 경계에서 충분한 거리 앞까지만 이동하여 오버슛 방지
                        float stopOffset = m_data.LeapStopOffset;
                        targetX = targetLeftEdge - stopOffset;
                        foundTarget = true;
                    }
                }
            }

            if (foundTarget)
            {
                // 타겟 발견 시 타겟팅된 위치가 현재 위치보다 뒤로 가지 않도록 방지
                targetX = Mathf.Max(origin.x, targetX);
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
