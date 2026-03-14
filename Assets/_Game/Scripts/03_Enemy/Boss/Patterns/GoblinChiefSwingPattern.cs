using UnityEngine;
using Cysharp.Threading.Tasks;
using TowerBreakers.Enemy.View;
using TowerBreakers.Core.Interfaces;
using System.Threading;
using DG.Tweening;
using TowerBreakers.Core.Events;

namespace TowerBreakers.Enemy.Logic
{
    /// <summary>
    /// [설명]: 고블린 족장의 대시 스윙 공격 패턴입니다.
    /// 플레이어에게 대시한 후 스윙 공격을 수행합니다.
    /// </summary>
    public class GoblinChiefSwingPattern : IBossPattern
    {

        #region 공개 프로퍼티
        public string PatternName => "Dash Swing";
        #endregion

        #region 초기화
        public GoblinChiefSwingPattern(IEventBus eventBus) { }
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

            // 1. 플레이어에게 대시
            Vector3 targetPos = new Vector3(player.transform.position.x, controller.transform.position.y, 0);
            
            // 방향 전환 (Player 쪽을 바라봄)
            controller.transform.rotation = TowerBreakers.Core.Utilities.DirectionHelper.FaceTarget(
                controller.transform.position.x, targetPos.x);

            // 이동 (트윈 사용, 속도는 데이터 기반 - 기본 속도의 2배 설정)
            float moveSpeed = data.MoveSpeed * 3.33f; // 기존 하드코딩 5.0f (데이터 기본 1.5f 기준 대략 3.3배)
            float moveDuration = Vector3.Distance(controller.transform.position, targetPos) / moveSpeed;
            await controller.transform.DOMove(targetPos, moveDuration).SetEase(Ease.OutQuad).WithCancellation(ct);

            // 2. 공격 애니메이션 재생 (AnimState = 2: Swing)
            view.PlayAnimation(global::PlayerState.ATTACK, 2);
            
            // 애니메이션 싱크를 위해 약간 대기 (공격 판정 시점까지)
            await UniTask.Delay(500, cancellationToken: ct);

            // 3. 공격 판정 (사거리 체크)
            if (Vector3.Distance(controller.transform.position, player.transform.position) < 2.0f)
            {
                var damageable = player.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    damageable.TakeDamage(data.AttackDamage, data.PushForce * 2.5f);
                }
                
                if (controller.EventBus != null)
                {
                    // [리팩토링]: 프로필 없이 직접 타격 연출 요청
                    controller.EventBus.Publish(new OnHitEffectRequested(player.transform.position, 0.5f, 0.2f));
                }
            }

            // 애니메이션 종료 대기
            await UniTask.Delay(1000, cancellationToken: ct);
            view.PlayAnimation(global::PlayerState.IDLE, 0);
        }
        #endregion
    }
}
