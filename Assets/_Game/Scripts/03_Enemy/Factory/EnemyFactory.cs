using UnityEngine;
using VContainer;
using VContainer.Unity;
using TowerBreakers.Enemy.View;
using TowerBreakers.Enemy.Data;
using TowerBreakers.Player.Logic;
using System.Collections.Generic;

namespace TowerBreakers.Enemy.Factory
{
    /// <summary>
    /// [설명]: 적 개체를 생성하고 오브젝트 풀링을 관리하는 팩토리 클래스입니다.
    /// </summary>
    public class EnemyFactory
    {
        #region 내부 필드
        private readonly IObjectResolver m_resolver;
        private readonly Core.Events.IEventBus m_eventBus;
        private readonly Dictionary<string, Stack<EnemyView>> m_pools = new Dictionary<string, Stack<EnemyView>>();
        private PlayerPushReceiver m_playerReceiver;
        #endregion

        [Inject]
        public EnemyFactory(IObjectResolver resolver, Core.Events.IEventBus eventBus)
        {
            m_resolver = resolver;
            m_eventBus = eventBus;
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
        public EnemyView Create(EnemyData data, Vector2 position, Transform parent = null)
        {
            if (data.EnemyPrefab == null)
            {
                Debug.LogError($"[EnemyFactory] '{data.EnemyName}' 데이터에 프리팹이 설정되지 않았습니다.");
                return null;
            }

            EnemyView view = null;
            string poolKey = data.EnemyName;

            // 1. 풀에서 사용 가능한 오브젝트가 있는지 확인
            if (m_pools.TryGetValue(poolKey, out var stack) && stack.Count > 0)
            {
                view = stack.Pop();
                if (view != null)
                {
                    view.transform.position = position;
                    view.transform.SetParent(parent);
                    view.gameObject.SetActive(true);
                }
            }

            // 2. 풀에 없으면 새로 생성
            if (view == null)
            {
                GameObject go = Object.Instantiate(data.EnemyPrefab, position, Quaternion.identity, parent);
                m_resolver.Inject(go);
                go.name = data.EnemyName;

                // 기차 대열 감지를 위해 레이어 설정 (TagManager에 'Enemy' 레이어가 있어야 합니다)
                int enemyLayer = LayerMask.NameToLayer("Enemy");
                if (enemyLayer != -1)
                {
                    go.layer = enemyLayer;
                }

                view = go.GetComponent<EnemyView>();
                if (view == null) view = go.AddComponent<EnemyView>();
            }

            // 3. 로직 초기화 (보스 여부 등 스탯 적용)
            var pushLogic = view.GetComponent<Logic.EnemyPushLogic>();
            if (pushLogic != null)
            {
                if (m_playerReceiver != null)
                {
                    float effectiveForce = (data.Type == EnemyType.Boss) ? data.PushForce * 0.5f : data.PushForce;
                    pushLogic.Initialize(effectiveForce, m_playerReceiver);
                }
                else
                {
                    Debug.LogWarning($"[EnemyFactory] PlayerPushReceiver가 설정되지 않아 '{data.EnemyName}'의 밀기 로직이 비활성화됩니다.");
                }
            }

            // 4. 상태 머신 컨트롤러 초기화 (전진 로직 구동축)
            var controller = view.GetComponent<Logic.EnemyController>();
            if (controller == null) controller = view.gameObject.AddComponent<Logic.EnemyController>();
            
            if (controller != null && pushLogic != null)
            {
                // Reclaim 콜백을 넘겨주어 사망 시 스스로 풀로 복귀하게 함
                controller.Initialize(data, view, pushLogic, m_eventBus, Reclaim);
            }

            return view;
        }

        /// <summary>
        /// [설명]: 사용이 끝난 적 오브젝트를 풀에 반환합니다.
        /// </summary>
        /// <param name="view">반환할 적 뷰 컴포넌트</param>
        /// <param name="enemyName">해당 적의 데이터 이름 (풀 키)</param>
        public void Reclaim(EnemyView view, string enemyName)
        {
            if (view == null) return;

            view.gameObject.SetActive(false);

            if (!m_pools.TryGetValue(enemyName, out var stack))
            {
                stack = new Stack<EnemyView>();
                m_pools[enemyName] = stack;
            }

            stack.Push(view);
        }
        #endregion
    }
}
