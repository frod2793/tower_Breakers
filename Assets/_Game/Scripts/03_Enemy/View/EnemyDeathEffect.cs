using UnityEngine;
using System.Collections.Generic;
using TowerBreakers.Enemy.View;

namespace TowerBreakers.Enemy.View
{
    /// <summary>
    /// [설명]: 적 사망 시 산산조각 매니저 클래스입니다.
    /// 파편 풀링을 관리하고 연출을 실행합니다.
    /// </summary>
    public class EnemyDeathEffect : MonoBehaviour
    {
        #region 에디터 설정
        [Header("설정")]
        [SerializeField, Tooltip("파편으로 사용할 프리팹 (DeathFragment 컴포넌트 포함 필수)")]
        private GameObject m_fragmentPrefab;

        [SerializeField, Tooltip("최초 풀 크기")]
        private int m_initialPoolSize = 50;

        [Header("폭발 파라미터")]
        [SerializeField, Tooltip("폭발 힘 강도")]
        private float m_explosionForce = 7f;

        [SerializeField, Tooltip("수직 상승 가중치 (0~1)")]
        private float m_upwardBias = 0.8f;

        [SerializeField, Tooltip("회전 강도")]
        private float m_torqueForce = 360f;

        [SerializeField, Tooltip("소멸 전 대기 시간 (초)")]
        private float m_fadeDelay = 0.5f;

        [SerializeField, Tooltip("사라지는 시간 (초)")]
        private float m_fadeDuration = 0.5f;
        #endregion

        #region 내부 변수
        private readonly Queue<DeathFragment> m_pool = new Queue<DeathFragment>();
        #endregion

        #region 초기화
        private void Awake()
        {
            CreatePool();
        }

        private void CreatePool()
        {
            if (m_fragmentPrefab == null) return;

            for (int i = 0; i < m_initialPoolSize; i++)
            {
                m_pool.Enqueue(CreateNewFragment());
            }
        }

        private DeathFragment CreateNewFragment()
        {
            GameObject obj = Instantiate(m_fragmentPrefab, transform);
            obj.SetActive(false);
            DeathFragment fragment = obj.GetComponent<DeathFragment>();
            if (fragment == null) fragment = obj.AddComponent<DeathFragment>();
            return fragment;
        }
        #endregion

        #region 공개 API
        /// <summary>
        /// [설명]: 지정된 적 캐릭터의 렌더러를 분석하여 산산조각 연출을 실행합니다.
        /// </summary>
        /// <param name="renderers">분석할 소스 렌더러 리스트</param>
        /// <param name="centerPos">폭발 중심 위치</param>
        public void PlayShatter(SpriteRenderer[] renderers, Vector3 centerPos)
        {
            if (renderers == null || renderers.Length == 0) return;

            foreach (var source in renderers)
            {
                if (source == null || source.sprite == null || !source.gameObject.activeInHierarchy) continue;

                DeathFragment fragment = GetFromPool();
                
                // 랜덤한 방향과 힘 계산
                Vector3 randomDir = (source.transform.position - centerPos).normalized;
                if (randomDir == Vector3.zero) randomDir = Random.insideUnitSphere.normalized;
                
                // [연출 개선]: 위로 솟구치는 느낌을 강화하기 위해 Up 벡터 가중치 적용
                Vector3 spreadDir = Vector3.Lerp(randomDir, Vector3.up, m_upwardBias);
                Vector3 velocity = spreadDir * Random.Range(m_explosionForce * 0.8f, m_explosionForce * 1.2f);
                float torque = Random.Range(-m_torqueForce, m_torqueForce);

                fragment.Initialize(
                    source.sprite,
                    source.transform.position,
                    velocity,
                    torque,
                    m_fadeDelay,
                    m_fadeDuration,
                    source.sortingOrder,
                    source.sortingLayerID,
                    source.color,
                    source.transform.lossyScale,
                    ReturnToPool
                );
            }
        }
        #endregion

        #region 풀링 로직
        private DeathFragment GetFromPool()
        {
            if (m_pool.Count > 0)
            {
                return m_pool.Dequeue();
            }
            return CreateNewFragment();
        }

        private void ReturnToPool(DeathFragment fragment)
        {
            m_pool.Enqueue(fragment);
        }
        #endregion
    }
}
