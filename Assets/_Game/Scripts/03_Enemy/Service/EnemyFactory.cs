using UnityEngine;
using UnityEngine.Pool;
using VContainer;
using VContainer.Unity;
using TowerBreakers.Tower.Service;

namespace TowerBreakers.Enemy.Service
{
    /// <summary>
    /// [클래스]: 적 오브젝트의 생성과 풀링을 담당하는 팩토리 클래스입니다.
    /// UnityEngine.Pool을 사용하여 메모리 할당을 최적화합니다.
    /// </summary>
    public class EnemyFactory
    {
        #region 내부 필드
        private readonly IObjectResolver m_resolver;
        private readonly GameObject m_enemyPrefab;
        private IObjectPool<GameObject> m_pool;
        private Transform m_poolParent;
        #endregion

        #region 초기화
        public EnemyFactory(IObjectResolver resolver, GameObject enemyPrefab)
        {
            m_resolver = resolver;
            m_enemyPrefab = enemyPrefab;
            
            InitializePool();
        }

        private void InitializePool()
        {
            m_pool = new ObjectPool<GameObject>(
                createFunc: CreateEnemyInstance,
                actionOnGet: OnGetEnemy,
                actionOnRelease: OnReleaseEnemy,
                actionOnDestroy: OnDestroyEnemy,
                collectionCheck: true,
                defaultCapacity: 10,
                maxSize: 50
            );
        }
        #endregion

        #region 공개 API
        /// <summary>
        /// [설명]: 풀에서 적 오브젝트를 가져옵니다.
        /// </summary>
        public GameObject Get(Transform parent)
        {
            m_poolParent = parent;
            return m_pool.Get();
        }

        /// <summary>
        /// [설명]: 사용이 끝난 적 오브젝트를 풀로 반환합니다.
        /// </summary>
        public void Release(GameObject enemy)
        {
            if (enemy != null)
            {
                m_pool.Release(enemy);
            }
        }
        #endregion

        #region 풀 콜백
        private GameObject CreateEnemyInstance()
        {
            var instance = Object.Instantiate(m_enemyPrefab, m_poolParent);
            // [핵심]: VContainer를 통한 의존성 주입 (생성된 인스턴스에 필요한 컴포넌트 주입)
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
