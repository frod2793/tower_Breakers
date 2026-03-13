using UnityEngine;
using System.Collections.Generic;
using TowerBreakers.Player.Logic;

namespace TowerBreakers.Enemy.Logic
{
    /// <summary>
    /// [설명]: 투사체 오브젝트의 생성을 관리하고 풀링하는 팩토리 클래스입니다.
    /// </summary>
    public class ProjectileFactory
    {
        #region 내부 필드
        private readonly Dictionary<GameObject, Stack<EnemyProjectile>> m_pools = new Dictionary<GameObject, Stack<EnemyProjectile>>();
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

            if (!m_pools.TryGetValue(prefab, out var pool))
            {
                pool = new Stack<EnemyProjectile>();
                m_pools[prefab] = pool;
            }

            EnemyProjectile instance;
            if (pool.Count > 0)
            {
                instance = pool.Pop();
                instance.transform.position = pos;
            }
            else
            {
                var go = Object.Instantiate(prefab, pos, Quaternion.identity, m_root);
                instance = go.GetComponent<EnemyProjectile>();
                if (instance == null)
                {
                    instance = go.AddComponent<EnemyProjectile>();
                }
            }

            instance.Initialize(speed, pushDist, target, (p) => pool.Push(p));
            return instance;
        }
        #endregion
    }
}
