using UnityEngine;
using UnityEngine.Pool;
using VContainer;
using System.Collections.Generic;
using VContainer.Unity;

namespace TowerBreakers.Enemy.Service
{
    /// <summary>
    /// [클래스]: 적 오브젝트의 생성과 풀링을 담당하는 팩토리 클래스입니다.
    /// 적 데이터(EnemyData)에 설정된 프리펩별로 독립적인 풀을 관리합니다.
    /// </summary>
    public class EnemyFactory
    {
        #region 내부 필드
        private readonly IObjectResolver m_resolver;
        private readonly Dictionary<GameObject, IObjectPool<GameObject>> m_pools = new Dictionary<GameObject, IObjectPool<GameObject>>();
        private readonly Dictionary<GameObject, GameObject> m_instanceToPrefab = new Dictionary<GameObject, GameObject>();
        private Transform m_poolParent;
        #endregion

        #region 초기화
        public EnemyFactory(IObjectResolver resolver)
        {
            m_resolver = resolver;
        }
        #endregion

        #region 공개 API
        /// <summary>
        /// [설명]: 특정 프리펩에 해당하는 풀에서 적 오브젝트를 가져옵니다.
        /// </summary>
        public GameObject Get(GameObject prefab, Transform parent)
        {
            if (prefab == null) return null;

            m_poolParent = parent;
            var pool = GetOrCreatePool(prefab);
            var instance = pool.Get();
            
            // 인스턴스가 어떤 프리펩의 것인지 기록 (반환 시 필요)
            m_instanceToPrefab[instance] = prefab;
            
            return instance;
        }

        /// <summary>
        /// [설명]: 사용이 끝난 적 오브젝트를 해당 프리펩 풀로 반환합니다.
        /// </summary>
        public void Release(GameObject enemy)
        {
            if (enemy == null) return;

            if (m_instanceToPrefab.TryGetValue(enemy, out GameObject prefab))
            {
                if (m_pools.TryGetValue(prefab, out var pool))
                {
                    pool.Release(enemy);
                }
                else
                {
                    Object.Destroy(enemy);
                }
                m_instanceToPrefab.Remove(enemy);
            }
            else
            {
                // 풀에 관리되지 않는 오브젝트인 경우 파괴
                Object.Destroy(enemy);
            }
        }
        #endregion

        #region 풀 관리
        private IObjectPool<GameObject> GetOrCreatePool(GameObject prefab)
        {
            if (m_pools.TryGetValue(prefab, out var pool))
            {
                return pool;
            }

            var newPool = new ObjectPool<GameObject>(
                createFunc: () => CreateEnemyInstance(prefab),
                actionOnGet: OnGetEnemy,
                actionOnRelease: OnReleaseEnemy,
                actionOnDestroy: OnDestroyEnemy,
                collectionCheck: true,
                defaultCapacity: 10,
                maxSize: 50
            );

            m_pools.Add(prefab, newPool);
            return newPool;
        }

        private GameObject CreateEnemyInstance(GameObject prefab)
        {
            var instance = Object.Instantiate(prefab, m_poolParent);
            // [핵심]: VContainer를 통한 의존성 주입
            m_resolver.InjectGameObject(instance);
            return instance;
        }

        private void OnGetEnemy(GameObject enemy)
        {
            enemy.SetActive(true);
        }

        private void OnReleaseEnemy(GameObject enemy)
        {
            enemy.SetActive(false);
        }

        private void OnDestroyEnemy(GameObject enemy)
        {
            Object.Destroy(enemy);
        }
        #endregion
    }
}
