using UnityEngine;
using UnityEngine.Pool;
using VContainer;
using VContainer.Unity;
using TowerBreakers.Enemy.View;
using TowerBreakers.Enemy.Data;
using TowerBreakers.Player.Logic;
using System.Collections.Generic;
using TowerBreakers.Enemy.Logic;
using TowerBreakers.Tower.Logic;

namespace TowerBreakers.Enemy.Factory
{
    /// <summary>
    /// [설명]: 적 개체를 생성하고 오브젝트 풀링을 관리하는 팩토리 클래스입니다.
    /// UnityEngine.Pool API를 사용하여 메모리 할당을 줄이고 객체 생명주기를 관리합니다.
    /// </summary>
    public class EnemyFactory
    {
        #region 내부 필드
        private readonly IObjectResolver m_resolver;
        private readonly Core.Events.IEventBus m_eventBus;
        private readonly ProjectileFactory m_projectileFactory;
        private readonly TowerManager m_towerManager;
        private readonly EnemyDeathEffect m_deathEffect;
        private readonly Dictionary<string, IObjectPool<EnemyView>> m_pools = new();
        private PlayerPushReceiver m_playerReceiver;
        #endregion

        [Inject]
        public EnemyFactory(IObjectResolver resolver, Core.Events.IEventBus eventBus, ProjectileFactory projectileFactory, TowerManager towerManager, EnemyDeathEffect deathEffect)
        {
            m_resolver = resolver;
            m_eventBus = eventBus;
            m_projectileFactory = projectileFactory;
            m_towerManager = towerManager;
            m_deathEffect = deathEffect;
        }

        #region 초기화
        /// <summary>
        /// [설명]: 플레이어 밀림 수신자를 설정합니다. 씬에 PlayerPushReceiver가 존재할 때 호출됩니다.
        /// </summary>
        /// <param name="receiver">플레이어 밀림 수신 컴포넌트</param>
        public void SetPlayerPushReceiver(PlayerPushReceiver receiver)
        {
            m_playerReceiver = receiver;
        }
        #endregion

        #region 공개 메서드
        /// <summary>
        /// [설명]: 적 데이터를 기반으로 적을 생성하거나 풀에서 가져옵니다.
        /// </summary>
        public EnemyView Create(EnemyData data, Vector2 position, int floorIndex, Transform parent = null)
        {
            if (data.EnemyPrefab == null)
            {
                global::UnityEngine.Debug.LogError($"[EnemyFactory] '{data.EnemyName}' 데이터에 프리팹이 설정되지 않았습니다.");
                return null;
            }

            string poolKey = data.EnemyName;

            // [최적화]: 프리팹별 풀이 없으면 생성
            if (!m_pools.TryGetValue(poolKey, out var pool))
            {
                pool = new ObjectPool<EnemyView>(
                    createFunc: () => OnCreateEnemy(data),
                    actionOnGet: (v) => v.gameObject.SetActive(true),
                    actionOnRelease: (v) => v.gameObject.SetActive(false),
                    actionOnDestroy: (v) => Object.Destroy(v.gameObject),
                    collectionCheck: true,
                    defaultCapacity: 5,
                    maxSize: 30
                );
                m_pools[poolKey] = pool;
            }

            EnemyView view = pool.Get();
            if (view != null)
            {
                view.transform.position = position;
                view.transform.SetParent(parent);
            }

            // 뷰 초기화 (애니메이션 시스템 등)
            view.Initialize();
            view.ResetState();

            // 3. 로직 초기화 (특수 개체 판별 및 스탯 적용)
            var pushLogic = view.GetComponent<Logic.EnemyPushLogic>();
            if (pushLogic != null)
            {
                if (m_playerReceiver != null)
                {
                    // [최적화]: Normal 타입을 제외한 모든 타입은 특수 개체로 분류 (항시 콜라이더 활성화)
                    bool isSpecial = (data.Type != EnemyType.Normal);
                    
                    float effectiveForce = (data.Type == EnemyType.Boss || data.Type == EnemyType.Tank) ? data.PushForce * 0.5f : data.PushForce;
                    pushLogic.Initialize(effectiveForce, m_playerReceiver, isSpecial);
                }
                else
                {
                    global::UnityEngine.Debug.LogError($"[EnemyFactory] 경고! PlayerPushReceiver가 NULL입니다. {data.EnemyName}이(가) 플레이어를 관통할 수 있습니다.");
                }
            }

            // 4. 상태 머신 컨트롤러 초기화 (전진 로직 구동축)
            var controller = view.GetComponent<Logic.EnemyController>();
            if (controller == null) controller = view.gameObject.AddComponent<Logic.EnemyController>();
            
            if (controller != null && pushLogic != null)
            {
                // Reclaim 대신 pool.Release를 사용하여 반환하도록 콜백 전달
                controller.Initialize(data, view, pushLogic, m_deathEffect, m_eventBus, m_towerManager, floorIndex, m_projectileFactory, (v, name) => pool.Release(v));
            }

            return view;
        }

        /// <summary>
        /// [설명]: 사용이 끝난 적 오브젝트를 풀에 반환합니다. (외부 호출용 브릿지)
        /// </summary>
        public void Reclaim(EnemyView view, string enemyName)
        {
            if (view == null) return;
            if (m_pools.TryGetValue(enemyName, out var pool))
            {
                pool.Release(view);
            }
            else
            {
                view.gameObject.SetActive(false);
            }
        }
        #endregion

        #region 풀 콜백
        private EnemyView OnCreateEnemy(EnemyData data)
        {
            GameObject go = Object.Instantiate(data.EnemyPrefab);
            m_resolver.Inject(go);
            go.name = data.EnemyName;

            // 레이어 설정
            int enemyLayer = LayerMask.NameToLayer("Enemy");
            if (enemyLayer != -1) go.layer = enemyLayer;

            var view = go.GetComponent<EnemyView>();
            if (view == null) view = go.AddComponent<EnemyView>();

            return view;
        }
        #endregion
    }
}
