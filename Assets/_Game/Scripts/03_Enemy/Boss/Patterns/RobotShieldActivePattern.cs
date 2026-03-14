using UnityEngine;
using Cysharp.Threading.Tasks;
using TowerBreakers.Enemy.View;
using System.Threading;
using TowerBreakers.Core.Events;
using TowerBreakers.Core.Interfaces;
using System.Collections.Generic;

namespace TowerBreakers.Enemy.Logic
{
    /// <summary>
    /// [설명]: 로봇(쉴드)의 활성 이동 및 공격 패턴입니다.
    /// 특정 경로를 순환하며 플레이어와 접촉 시 데미지를 줍니다.
    /// </summary>
    public class RobotShieldActivePattern : IBossPattern
    {
        #region 내부 필드
        private const float OnActiveSpeedMultiplier = 5f;
        private const float ActiveDuration = 3.0f;
        #endregion

        #region 공개 프로퍼티
        public string PatternName => "Shield Charge";
        #endregion

        #region 초기화
        public RobotShieldActivePattern(IEventBus eventBus) { }
        #endregion

        #region 비즈니스 로직
        public async UniTask ExecuteAsync(EnemyController controller, CancellationToken ct)
        {
            var view = controller.CachedView;
            var pushLogic = controller.CachedPushLogic;
            var data = controller.Data;

            if (view == null || pushLogic == null || data == null) return;

            // 1. 활성 애니메이션 재생 (Index 1: Active)
            view.PlayAnimation(global::PlayerState.ATTACK, 1);

            if (controller.EventBus != null)
            {
                controller.EventBus.Publish(new OnSoundRequested("Robot_Shield_Active"));
            }

            // 2. 사각형 경로 이동 공격 (레거시 OnActiveDirection 로직 변형)
            // 상 -> 우 -> 하 -> 좌 순환
            Vector2[] directions = { Vector2.up, Vector2.right, Vector2.down, Vector2.left };
            float segmentDuration = ActiveDuration / directions.Length;

            for (int i = 0; i < directions.Length; i++)
            {
                if (ct.IsCancellationRequested) break;

                Vector2 dir = directions[i];
                float startTime = Time.time;

                while (Time.time - startTime < segmentDuration)
                {
                    if (ct.IsCancellationRequested) break;

                    // 이동 실행
                    float speed = data.MoveSpeed * OnActiveSpeedMultiplier;
                    controller.transform.Translate(dir * speed * Time.deltaTime);

                    // 접촉 데미지 판정 (PushLogic 활용)
                    var player = pushLogic.PlayerReceiver;
                    if (player != null && Vector2.Distance(controller.transform.position, player.transform.position) < 1.0f)
                    {
                        // [최적화]: IDamageable 인터페이스 직접 사용
                        var damageable = player.GetComponent<IDamageable>();
                        if (damageable != null)
                        {
                            damageable.TakeDamage(data.AttackDamage, data.PushForce);
                        }
                    }

                    await UniTask.Yield(PlayerLoopTiming.Update, ct);
                }
            }

            // 3. 패턴 종료
            view.PlayAnimation(global::PlayerState.IDLE);
            await UniTask.Delay(500, cancellationToken: ct);
        }
        #endregion
    }
}
