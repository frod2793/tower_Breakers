using UnityEngine;
using Cysharp.Threading.Tasks;
using TowerBreakers.Enemy.View;
using System.Threading;
using TowerBreakers.Core.Events;

namespace TowerBreakers.Enemy.Logic
{
    /// <summary>
    /// [설명]: 크라켄의 포격 패턴입니다. 회전하며 투사체를 발사합니다.
    /// </summary>
    public class KrakenArtilleryPattern : IBossPattern
    {
        private readonly ProjectileFactory m_projectileFactory;
        private readonly IEventBus m_eventBus;

        public string PatternName => "Artillery Fire";

        public KrakenArtilleryPattern(ProjectileFactory factory, IEventBus eventBus)
        {
            m_projectileFactory = factory;
            m_eventBus = eventBus;
        }

        public async UniTask ExecuteAsync(EnemyController controller, CancellationToken ct)
        {
            var view = controller.CachedView;
            var pushLogic = controller.CachedPushLogic;
            var data = controller.Data;

            if (view == null || pushLogic == null || data == null) return;

            var player = pushLogic.PlayerReceiver;
            if (player == null) return;

            float duration = 3.0f;
            float interval = 0.5f;
            float rotationSpeed = 30f;
            float currentAngle = 0f;

            view.PlayAnimation(global::PlayerState.ATTACK, 2);

            for (float t = 0; t < duration; t += interval)
            {
                if (ct.IsCancellationRequested) break;

                // 4방향 발사
                for (int i = 0; i < 4; i++)
                {
                    float angle = (currentAngle + (i * 90)) * Mathf.Deg2Rad;
                    Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                    
                    // ProjectileFactory를 통한 투사체 생성
                    if (data.ProjectilePrefab != null)
                    {
                        m_projectileFactory.Create(
                            data.ProjectilePrefab, 
                            controller.transform.position, 
                            5.0f, // 투사체 속도 (기본값)
                            data.ProjectilePushDistance, 
                            player
                        );
                    }
                }

                if (controller.EventBus != null)
                {
                    controller.EventBus.Publish(new OnSoundRequested("Kraken_Shoot", 0.5f));
                }
                
                currentAngle += rotationSpeed;

                await UniTask.Delay((int)(interval * 1000), cancellationToken: ct);
            }

            view.PlayAnimation(global::PlayerState.IDLE);
        }
    }
}
