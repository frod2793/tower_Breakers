using UnityEngine;
using Cysharp.Threading.Tasks;
using TowerBreakers.Enemy.View;
using System.Threading;
using DG.Tweening;
using TowerBreakers.Core.Events;

namespace TowerBreakers.Enemy.Logic
{
    /// <summary>
    /// [설명]: 로봇(검)의 돌진 공격 패턴입니다.
    /// 플레이어를 향해 회전하며 돌진합니다.
    /// </summary>
    public class RobotSwordDashPattern : IBossPattern
    {
        public string PatternName => "Sword Dash";

        public RobotSwordDashPattern(IEventBus eventBus) { } // 생성자 유지 (호출부 호환성)

        public async UniTask ExecuteAsync(EnemyController controller, CancellationToken ct)
        {
            var view = controller.CachedView;
            var pushLogic = controller.CachedPushLogic;
            var data = controller.Data;

            if (view == null || pushLogic == null || data == null) return;

            var player = pushLogic.PlayerReceiver;
            if (player == null) return;

            // 1. 타겟 방향 계산 및 회전
            Vector3 dir = player.transform.position - controller.transform.position;
            float angle = TowerBreakers.Core.Utilities.DirectionHelper.ToRotation(dir).eulerAngles.z;
            
            view.PlayAnimation(global::PlayerState.ATTACK);
            await controller.transform.DORotate(new Vector3(0, 0, angle - 180f), 0.5f).WithCancellation(ct);

            // 2. 돌진
            float dashDistance = data.MoveSpeed * 3.33f; // 기존 하드코딩 5.0f 기준
            Vector3 dashPos = controller.transform.position + dir.normalized * dashDistance;
            
            if (controller.EventBus != null)
            {
                controller.EventBus.Publish(new OnSoundRequested("Robot_Dash"));
            }
            
            await controller.transform.DOMove(dashPos, 0.3f).SetEase(Ease.InExpo).WithCancellation(ct);

            // 3. 복귀 회전
            await controller.transform.DORotate(Vector3.zero, 0.3f).WithCancellation(ct);
            view.PlayAnimation(global::PlayerState.IDLE);
        }
    }
}
