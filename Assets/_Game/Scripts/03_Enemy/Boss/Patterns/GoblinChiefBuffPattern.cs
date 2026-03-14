using UnityEngine;
using Cysharp.Threading.Tasks;
using TowerBreakers.Enemy.View;
using System.Threading;
using TowerBreakers.Core.Events;
using System.Linq;

namespace TowerBreakers.Enemy.Logic
{
    /// <summary>
    /// [설명]: 고블린족장의 버프/소환 토템 패턴입니다.
    /// 플레이어 근처에 아군 고블린 3기를 소환합니다.
    /// </summary>
    public class GoblinChiefBuffPattern : IBossPattern
    {
        #region 공개 프로퍼티
        public string PatternName => "Buff Totem";
        #endregion

        #region 초기화
        public GoblinChiefBuffPattern(IEventBus eventBus) { }
        #endregion

        #region 비즈니스 로직
        public async UniTask ExecuteAsync(EnemyController controller, CancellationToken ct)
        {
            var view = controller.CachedView;
            var pushLogic = controller.CachedPushLogic;
            var data = controller.Data;

            if (view == null || pushLogic == null || data == null) return;

            var player = pushLogic.PlayerReceiver;
            if (player == null) return;

            // 소환 모션 (AnimState = 3: SummonTotem)
            view.PlayAnimation(global::PlayerState.ATTACK, 3);
            await UniTask.Delay(500, cancellationToken: ct);

            SummonGoblins(controller, player.transform.position);

            controller.EventBus?.Publish(new OnSoundRequested("TotemSummon"));

            await UniTask.Delay(1000, cancellationToken: ct);
            view.PlayAnimation(global::PlayerState.IDLE, 0);
        }

        private void SummonGoblins(EnemyController boss, Vector3 playerPos)
        {
            for (int i = -1; i <= 1; i++)
            {
                Vector3 spawnPos = new Vector3(
                    playerPos.x + (i * 1.5f),
                    boss.transform.position.y,
                    0);

                GameObject goblin = CreateGoblinSummon(spawnPos);
                Debug.Log($"[BuffPattern] 고블린 소환: 위치 {spawnPos}");
            }

            Debug.Log($"[BuffPattern] 버프 토템 완료: 플레이어 근처에 고블린 3기 소환");
        }

        private GameObject CreateGoblinSummon(Vector3 position)
        {
            GameObject goblin = new GameObject("SummonedGoblin");
            goblin.transform.position = position;

            SpriteRenderer sr = goblin.AddComponent<SpriteRenderer>();
            sr.color = Color.green;
            sr.size = new Vector2(0.8f, 1.2f);

            BoxCollider2D collider = goblin.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = new Vector2(0.8f, 1.2f);

            GoblinSummonComponent summonComp = goblin.AddComponent<GoblinSummonComponent>();
            summonComp.Initialize();

            return goblin;
        }
        #endregion
    }

    internal class GoblinSummonComponent : MonoBehaviour
    {
        private float m_lifeTime = 10f;
        private float m_timer;

        public void Initialize()
        {
            m_timer = 0f;
        }

        private void Update()
        {
            m_timer += Time.deltaTime;

            if (m_timer >= m_lifeTime)
            {
                Destroy(gameObject);
            }
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.CompareTag("Player"))
            {
                var damageable = collision.GetComponent<Core.Interfaces.IDamageable>();
                damageable?.TakeDamage(5, 2f);
            }
        }
    }
}
