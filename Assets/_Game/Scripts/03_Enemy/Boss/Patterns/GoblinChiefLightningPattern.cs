using UnityEngine;
using Cysharp.Threading.Tasks;
using TowerBreakers.Enemy.View;
using System.Threading;
using TowerBreakers.Core.Events;
using TowerBreakers.Core.Interfaces;
using System.Linq;

namespace TowerBreakers.Enemy.Logic
{
    /// <summary>
    /// [설명]: 고블린 족장의 번개 토템 패턴입니다.
    /// 3방향에서 번개가 순차적으로 떨어집니다.
    /// </summary>
    public class GoblinChiefLightningPattern : IBossPattern
    {
        #region 공개 프로퍼티
        public string PatternName => "Lightning Totem";
        #endregion

        #region 초기화
        public GoblinChiefLightningPattern(IEventBus eventBus) { }
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
            await UniTask.Delay(300, cancellationToken: ct);

            float[] directions = { -3f, 0f, 3f };
            directions = directions.OrderBy(x => Random.value).ToArray();

            for (int i = 0; i < 3; i++)
            {
                float xOffset = directions[i];
                Vector3 strikePos = new Vector3(
                    player.transform.position.x + xOffset,
                    player.transform.position.y,
                    0);

                SpawnLightning(strikePos, (int)(data.AttackDamage * 1.2f));

                controller.EventBus?.Publish(new OnSoundRequested("TotemSummon"));

                await UniTask.Delay(800, cancellationToken: ct);
            }

            await UniTask.Delay(500, cancellationToken: ct);
            view.PlayAnimation(global::PlayerState.IDLE, 0);
        }

        private void SpawnLightning(Vector3 position, int damage)
        {
            GameObject lightning = new GameObject("GoblinLightning");
            lightning.transform.position = position;

            SpriteRenderer sr = lightning.AddComponent<SpriteRenderer>();
            sr.color = new Color(0.8f, 0.8f, 1f, 0.9f);
            sr.size = new Vector2(0.3f, 10f);

            BoxCollider2D collider = lightning.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = new Vector2(0.5f, 10f);

            LightningComponent lightningComp = lightning.AddComponent<LightningComponent>();
            lightningComp.Initialize(damage, 0.5f);
        }
        #endregion
    }

    internal class LightningComponent : MonoBehaviour
    {
        private int m_damage;
        private float m_duration;
        private float m_timer;
        private bool m_hit;

        public void Initialize(int damage, float duration)
        {
            m_damage = damage;
            m_duration = duration;
            m_timer = 0f;
            m_hit = false;
        }

        private void Update()
        {
            m_timer += Time.deltaTime;

            if (m_timer >= m_duration)
            {
                Destroy(gameObject);
            }
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (m_hit) return;
            if (collision.CompareTag("Player"))
            {
                m_hit = true;
                var damageable = collision.GetComponent<IDamageable>();
                damageable?.TakeDamage(m_damage, 3f);
                Debug.Log($"[LightningPattern] 번개 히트: 데미지 {m_damage}");
            }
        }
    }
}
