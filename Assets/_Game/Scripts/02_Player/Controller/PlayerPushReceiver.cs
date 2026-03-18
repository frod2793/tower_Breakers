using System;
using UnityEngine;
using TowerBreakers.Player.Logic;
using TowerBreakers.Player.DTO;

namespace TowerBreakers.Player.Controller
{
    /// <summary>
    /// [기능]: 플레이어 밀림 수신 컨트롤러
    /// </summary>
    public class PlayerPushReceiver : MonoBehaviour
    {
        #region 내부 변수
        private PlayerLogic m_playerLogic;
        private PlayerConfigDTO m_config;
        private float m_lastDamageTime;
        #endregion

        #region 이벤트
        /// <summary>
        /// [이벤트]: 플레이어 체력이 변경될 때 발생합니다.
        /// </summary>
        public event Action<int> OnHealthChanged;
        /// <summary>
        /// [이벤트]: 플레이어가 사망할 때 발생합니다.
        /// </summary>
        public event Action OnPlayerDeath;
        #endregion

        #region 속성
        /// <summary>
        /// [속성]: 현재 플레이어의 체력을 반환합니다.
        /// </summary>
        public int CurrentHealth => m_playerLogic?.State.Health ?? 0;
        #endregion

        #region 초기화
        /// <summary>
        /// [설명]: 플레이어 밀림 수신기를 초기화합니다.
        /// </summary>
        /// <param name="maxHealth">최대 체력</param>
        /// <param name="config">플레이어 설정 DTO</param>
        /// <param name="playerLogic">플레이어 로직 인스턴스</param>
        public void Initialize(int maxHealth, PlayerConfigDTO config, PlayerLogic playerLogic)
        {
            m_config = config;
            m_playerLogic = playerLogic;
            
            if (m_playerLogic != null)
            {
                m_playerLogic.InitializeHealth(maxHealth);
            }
        }
        #endregion

        #region 유니티 생명주기
        private void Update()
        {
            CheckLeftWall();
        }
        #endregion

        #region 공개 API
        /// <summary>
        /// [설명]: 외부에서 밀림 힘을 적용합니다. 
        /// </summary>
        /// <param name="force">적용할 물리적인 힘</param>
        public void Push(Vector2 force)
        {
            if (m_config == null) return;

            if (m_playerLogic != null)
            {
                // 밀림 저항력을 적용하여 로직에 전달
                m_playerLogic.ApplyExternalPush(force * (1.0f - m_config.PushResistance));
            }
            else
            {
                // 로직이 없는 경우의 폴백
                transform.Translate(force * (1.0f - m_config.PushResistance) * Time.deltaTime);
            }
        }
        #endregion

        #region 내부 로직
        /// <summary>
        /// [설명]: 플레이어가 왼쪽 벽에 도달했는지 체크하고 데미지를 처리합니다.
        /// </summary>
        private void CheckLeftWall()
        {
            if (m_config == null) return;

            // 좌측 벽 충돌 체크 (센서 역할)
            if (transform.position.x <= m_config.LeftWallX)
            {
                if (Time.time - m_lastDamageTime >= m_config.DamageCooldown)
                {
                    TakeDamage();
                }
            }
        }

        /// <summary>
        /// [설명]: 무적 쿨다운을 고려하여 데미지를 계산하고 로직에 통보합니다.
        /// </summary>
        private void TakeDamage()
        {
            if (m_config == null) return;
            m_lastDamageTime = Time.time;

            if (m_playerLogic != null)
            {
                m_playerLogic.TakeDamage(m_config.DamagePerHit);
            }

            OnHealthChanged?.Invoke(m_playerLogic?.State.Health ?? 0);
        }

        private void Die()
        {
            // 실제 사망 처리는 PlayerLogic에서 수행하고 View가 반응함
            OnPlayerDeath?.Invoke();
        }
        #endregion

        #region 에디터 지원
        private void OnDrawGizmosSelected()
        {
            if (m_config == null) return;

            Gizmos.color = Color.red;
            Gizmos.DrawLine(
                new Vector3(m_config.LeftWallX, transform.position.y - 10f, 0),
                new Vector3(m_config.LeftWallX, transform.position.y + 10f, 0)
            );
        }
        #endregion
    }
}
