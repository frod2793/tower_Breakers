using UnityEngine;
using UnityEngine.Pool;
using System.Collections.Generic;
using TowerBreakers.Player.Logic;

namespace TowerBreakers.Enemy.Logic
{
    /// <summary>
    /// [설명]: 투사체 오브젝트의 생성을 관리하고 풀링하는 팩토리 클래스입니다.
    /// UnityEngine.Pool API를 사용하여 효율적인 객체 관리를 수행합니다.
    /// </summary>
    public class ProjectileFactory
    {
        #region 내부 필드
        private readonly Dictionary<GameObject, IObjectPool<EnemyProjectile>> m_pools = new();
        private Transform m_root;
        #endregion

        public ProjectileFactory()
        {
            // 루트 오브젝트 생성 (정리용)
            m_root = new GameObject("ProjectileRoot").transform;
            Object.DontDestroyOnLoad(m_root.gameObject);
        }

        #region 공개 API
        public EnemyProjectile Create(GameObject prefab, Vector3 pos, float speed, float pushDist, PlayerPushReceiver target)
        {
            if (prefab == null) return null;

            // [최적화]: 프리팹별 풀이 없으면 생성
            if (!m_pools.TryGetValue(prefab, out var pool))
            {
                pool = new ObjectPool<EnemyProjectile>(
                    createFunc: () => OnCreateProjectile(prefab),
                    actionOnGet: (p) => p.gameObject.SetActive(true),
                    actionOnRelease: (p) => p.gameObject.SetActive(false),
                    actionOnDestroy: (p) => Object.Destroy(p.gameObject),
                    collectionCheck: true,
                    defaultCapacity: 10,
                    maxSize: 50
                );
                m_pools[prefab] = pool;
            }

            EnemyProjectile instance = pool.Get();
            if (instance != null)
            {
                instance.transform.position = pos;
                // 초기화 시 반환 콜백으로 pool.Release를 전달
                instance.Initialize(speed, pushDist, target, (p) => pool.Release(p));
            }

            return instance;
        }
        #endregion

        #region 풀 콜백
        private EnemyProjectile OnCreateProjectile(GameObject prefab)
        {
            var go = Object.Instantiate(prefab, m_root);
            var instance = go.GetComponent<EnemyProjectile>();
            if (instance == null)
            {
                instance = go.AddComponent<EnemyProjectile>();
            }
            return instance;
        }
        #endregion
    }
}
