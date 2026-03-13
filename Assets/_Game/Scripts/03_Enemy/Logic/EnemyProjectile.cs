using UnityEngine;
using TowerBreakers.Player.Logic;

namespace TowerBreakers.Enemy.Logic
{
    /// <summary>
    /// [설명]: 적 서포터가 발사하는 투사체 컴포넌트입니다.
    /// 플레이어 레이어를 감지하여 강제로 밀쳐내는 역할을 수행합니다.
    /// </summary>
    public class EnemyProjectile : MonoBehaviour
    {
        #region 내부 필드
        private float m_speed;
        private float m_pushDistance;
        private PlayerPushReceiver m_target;
        private System.Action<EnemyProjectile> m_onReclaim;
        
        private float m_lifeTimer;
        private const float MAX_LIFE_TIME = 5.0f;
        #endregion

        #region 초기화
        public void Initialize(float speed, float pushDistance, PlayerPushReceiver target, System.Action<EnemyProjectile> onReclaim)
        {
            m_speed = speed;
            m_pushDistance = pushDistance;
            m_target = target;
            m_onReclaim = onReclaim;
            m_lifeTimer = 0f;
            
            gameObject.SetActive(true);
        }
        #endregion

        #region 유니티 생명주기
        private void Update()
        {
            // 왼쪽으로 이동
            transform.Translate(Vector3.left * m_speed * Time.deltaTime);
            
            // 안전장치: 일정 시간 후 자동 소멸
            m_lifeTimer += Time.deltaTime;
            if (m_lifeTimer >= MAX_LIFE_TIME)
            {
                Reclaim();
            }
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            // 플레이어 레이어 감지 (레이어 체크 로직은 프로젝트 설정에 따라 다를 수 있으나 보통 Player 레이어 사용)
            if (collision.CompareTag("Player"))
            {
                if (m_target != null)
                {
                    m_target.ApplyProjectileKnockback(m_pushDistance);
                }
                Reclaim();
            }
        }
        #endregion

        #region 내부 로직
        private void Reclaim()
        {
            gameObject.SetActive(false);
            m_onReclaim?.Invoke(this);
        }
        #endregion
    }
}
