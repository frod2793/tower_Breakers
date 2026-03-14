using UnityEngine;
using Cysharp.Threading.Tasks;
using TowerBreakers.Enemy.View;
using System.Threading;
using TowerBreakers.Core.Events;
using TowerBreakers.Core.Interfaces;

namespace TowerBreakers.Enemy.Logic
{
    /// <summary>
    /// [설명]: 고블린 족장의 폭탄 토템 패턴입니다.
    /// 플레이어 위에 3개의 폭탄을 순차적으로 투하합니다.
    /// </summary>
    public class GoblinChiefBombPattern : IBossPattern
    {
        #region 공개 프로퍼티
        public string PatternName => "Bomb Totem";
        #endregion

        #region 초기화
        public GoblinChiefBombPattern(IEventBus eventBus) { }
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

            // 소모 모션 (AnimState = 3: SummonTotem)
            view.PlayAnimation(global::PlayerState.ATTACK, 3);
            await UniTask.Delay(300, cancellationToken: ct);

            for (int i = 0; i < 3; i++)
            {
                Vector3 spawnPos = new Vector3(
                    player.transform.position.x,
                    controller.transform.position.y + 5f,
                    0);

                SpawnBomb(spawnPos, data.AttackDamage * 2, player.transform.position);

                controller.EventBus?.Publish(new OnSoundRequested("TotemSummon"));

                if (i < 2)
                {
                    await UniTask.Delay(300, cancellationToken: ct);
                }
            }

            await UniTask.Delay(1000, cancellationToken: ct);
            view.PlayAnimation(global::PlayerState.IDLE, 0);
        }

        private void SpawnBomb(Vector3 spawnPos, int damage, Vector3 targetPos)
        {
            GameObject bomb = new GameObject("GoblinBomb");
            bomb.transform.position = spawnPos;

            SpriteRenderer sr = bomb.AddComponent<SpriteRenderer>();
            sr.color = Color.red;
            sr.size = new Vector2(0.5f, 0.5f);

            CircleCollider2D collider = bomb.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 1.5f;

            BombComponent bombComp = bomb.AddComponent<BombComponent>();
            bombComp.Initialize(damage, targetPos, 1.5f);
        }
        #endregion
    }

    internal class BombComponent : MonoBehaviour
    {
        private int m_damage;
        private Vector3 m_targetPos;
        private float m_delay;
        private float m_timer;
        private bool m_exploded;

        public void Initialize(int damage, Vector3 targetPos, float delay)
        {
            m_damage = damage;
            m_targetPos = targetPos;
            m_delay = delay;
            m_timer = 0f;
            m_exploded = false;
        }

        private void Update()
        {
            m_timer += Time.deltaTime;

            transform.position = Vector3.MoveTowards(
                transform.position,
                m_targetPos,
                8f * Time.deltaTime);

            if (!m_exploded && m_timer >= m_delay)
            {
                Explode();
            }
        }

        private void Explode()
        {
            m_exploded = true;

            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 2f);
            foreach (var hit in hits)
            {
                if (hit.CompareTag("Player"))
                {
                    var damageable = hit.GetComponent<IDamageable>();
                    damageable?.TakeDamage(m_damage, 5f);
                }
            }

            Debug.Log($"[BombPattern] 폭탄爆炸: 위치 {transform.position}, 데미지 {m_damage}");
            Destroy(gameObject);
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (m_exploded) return;
            if (collision.CompareTag("Player"))
            {
                Explode();
            }
        }
    }
}
