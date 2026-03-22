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
        private TowerBreakers.Battle.CombatSystem m_combatSystem;
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
        /// <param name="combatSystem">전투 시스템</param>
        public void Initialize(int maxHealth, PlayerConfigDTO config, PlayerLogic playerLogic, TowerBreakers.Battle.CombatSystem combatSystem)
        {
            m_config = config;
            m_playerLogic = playerLogic;
            m_combatSystem = combatSystem;
            
            // [개선]: 스폰 즉시 벽 데미지를 입지 않도록 마지막 데미지 시간을 현재로 초기화
            m_lastDamageTime = Time.time;

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
                // [수정]: PushResistance 제거됨. 외부 힘을 100% 그대로 전달.
                m_playerLogic.ApplyExternalPush(force);
            }
            else
            {
                // 로직이 없는 경우의 폴백
                transform.Translate(force * Time.deltaTime);
            }
        }
        #endregion

        #region 내부 로직
        /// <summary>
        /// [설명]: 플레이어가 왼쪽 벽에 도달했는지 체크하고 데미지를 처리합니다.
        /// </summary>
        private void CheckLeftWall()
        {
            if (m_config == null || m_playerLogic == null) return;

            float logicalX = m_playerLogic.State.Position.x;
            
            // 1. 플레이어가 왼쪽 벽 임계값 이하인지 확인
            if (logicalX <= m_config.LeftWallX + 0.01f)
            {
                // 2. [추가]: 단순히 벽에 있다고 데미지를 입는 것이 아니라, 적이 근처에 있는지(압착 중인지) 확인
                GameObject nearestEnemy = m_playerLogic.GetFrontEnemy();
                if (nearestEnemy != null)
                {
                    float distToEnemy = nearestEnemy.transform.position.x - logicalX;
                    
                    // 적과의 거리가 공격 범위 + 버퍼 이내일 때만 압착으로 판정
                    if (distToEnemy <= m_config.AttackRange + m_config.AttackRangeBuffer + 0.1f)
                    {
                        if (Time.time - m_lastDamageTime >= m_config.DamageCooldown)
                        {
                            TakeDamage();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// [설명]: 무적 쿨다운을 고려하여 데미지를 계산하고 로직에 통보합니다.
        /// </summary>
        private void TakeDamage()
        {
            if (m_config == null || m_combatSystem == null) return;
            m_lastDamageTime = Time.time;

            // [리팩토링]: 전투 판정 로직을 전담 시스템으로 위임
            m_combatSystem.HandleWallCrush();
            
            // [참고]: HUD 갱신 등은 이제 EventBus(OnPlayerDamaged)를 통해 처리되지만,
            // 기존 델리게이트 호환성을 위해 유지합니다.
            if (m_playerLogic != null)
            {
                OnHealthChanged?.Invoke(m_playerLogic.State.Health);
            }
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
