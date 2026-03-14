using UnityEngine;
using Cysharp.Threading.Tasks;
using TowerBreakers.Enemy.View;
using System.Threading;
using DG.Tweening;
using TowerBreakers.Core.Events;
using TowerBreakers.Core.Interfaces;

namespace TowerBreakers.Enemy.Logic
{
    /// <summary>
    /// [설명]: 고블린 족장의 점프 공격 패턴입니다.
    /// 공중으로 점프한 후 플레이어 위치에 착지하며 데미지를 줍니다.
    /// </summary>
    public class GoblinChiefJumpPattern : IBossPattern
    {

        #region 공개 프로퍼티
        public string PatternName => "Jump Attack";
        #endregion

        #region 초기화
        public GoblinChiefJumpPattern(IEventBus eventBus) { }
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

            // 1. 점프 시작 애니메이션 (AnimState = 1: Jump)
            view.PlayAnimation(global::PlayerState.ATTACK, 1);
            await UniTask.Delay(300, cancellationToken: ct);

            // 2. 플레이어 위치로 점프 이동
            Vector3 targetPos = new Vector3(player.transform.position.x, controller.transform.position.y, 0);
            
            // 점프 곡선 연출
            await controller.transform.DOJump(targetPos, 3.0f, 1, 0.8f).SetEase(Ease.OutQuad).WithCancellation(ct);

            // 3. 착지 및 데미지 (AnimState = 4: Landing)
            view.PlayAnimation(global::PlayerState.ATTACK, 4);
            
            // 주변 카메라 쉐이크 요청 (EventBus 사용)
            if (controller.EventBus != null)
            {
                // [리팩토링]: 프로필 없이 강한 쉐이크 직접 요청
                controller.EventBus.Publish(new OnHitEffectRequested(controller.transform.position, 0.8f, 0.3f));
            }

            if (Vector3.Distance(controller.transform.position, player.transform.position) < 2.5f)
            {
                var damageable = player.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    damageable.TakeDamage((int)(data.AttackDamage * 1.5f), data.PushForce * 4.0f);
                }
            }

            await UniTask.Delay(1000, cancellationToken: ct);
            view.PlayAnimation(global::PlayerState.IDLE, 0);
        }
        #endregion
    }
}
