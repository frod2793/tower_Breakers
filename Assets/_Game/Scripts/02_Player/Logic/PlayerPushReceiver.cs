using UnityEngine;
using TowerBreakers.Player.Data;
using TowerBreakers.Core.Events;
using System;

namespace TowerBreakers.Player.Logic
{
    /// <summary>
    /// [설명]: 적의 밀기 힘을 수신하여 플레이어의 위치를 조정하고 벽 충돌을 판정하는 클래스입니다.
    /// </summary>
    public class PlayerPushReceiver : MonoBehaviour
    {
        #region 에디터 설정
        [SerializeField, Tooltip("왼쪽 벽(경계)의 X 좌표")]
        private float m_leftWallXThreshold = -2.5f;
        #endregion

        #region 내부 필드
        private PlayerModel m_model;
        private IEventBus m_eventBus;
        #endregion

        #region 초기화
        public void Initialize(PlayerModel model, IEventBus eventBus)
        {
            m_model = model;
            m_eventBus = eventBus;
        }
        #endregion

        #region 공개 메서드
        /// <summary>
        /// [설명]: 특정 힘으로 플레이어를 왼쪽으로 밀어냅니다.
        /// </summary>
        /// <param name="pushForce">밀어내는 힘의 크기</param>
        public void ReceivePush(float pushForce)
        {
            if (m_model == null) return;

            // 새로운 위치 계산 (가로 방향 왼쪽으로 밀림)
            float moveAmount = pushForce * Time.deltaTime;
            Vector2 newPosition = m_model.Position + Vector2.left * moveAmount;

            // 왼쪽 벽 충돌 판정
            if (newPosition.x <= m_leftWallXThreshold)
            {
                newPosition.x = m_leftWallXThreshold;
                // 벽에 닿은 상태에서 계속 밀리면 데미지 발생
                m_eventBus?.Publish(new OnPlayerPushed(moveAmount));
            }

            m_model.Position = newPosition;
            transform.position = new Vector3(newPosition.x, newPosition.y, 0f);
        }
        #endregion
    }
}
