using UnityEngine;
using Cysharp.Threading.Tasks;
using TowerBreakers.Enemy.View;
using System.Threading;
using TowerBreakers.Core.Events;
using TowerBreakers.Core.Interfaces;
using DG.Tweening;

namespace TowerBreakers.Enemy.Logic
{
    /// <summary>
    /// [설명]: 로봇(거너)의 조준 및 발격 패턴입니다.
    /// 플레이어 방향으로 팔을 회전시킨 후 발사합니다.
    /// </summary>
    public class RobotGunnerAttackPattern : IBossPattern
    {
        #region 내부 필드
        private readonly ProjectileFactory m_projectileFactory;
        private readonly IEventBus m_eventBus;
        
        private const float AimingTime = 0.317f;
        #endregion

        #region 공개 프로퍼티
        public string PatternName => "Gunner Fire";
        #endregion

        #region 초기화
        public RobotGunnerAttackPattern(ProjectileFactory factory, IEventBus eventBus)
        {
            m_projectileFactory = factory;
        }
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

            // 1. 조준 (플레이어 방향으로 회전)
            Vector2 dir = (Vector2)player.transform.position - (Vector2)controller.transform.position;
            Quaternion rotation = Core.Utilities.DirectionHelper.ToRotation(dir);
            float angle = rotation.eulerAngles.z - 180f; // SPUM 보정
            
            bool lookRight = (-90 > angle && angle > -270);
            Vector3 goalBodyScale = Core.Utilities.DirectionHelper.GetFacingRotation(lookRight).eulerAngles.y > 90f ? new Vector3(-1f, 1f, 1f) : Vector3.one;

            await controller.transform.DORotate(new Vector3(0, 0, angle), AimingTime).SetEase(Ease.OutQuad).WithCancellation(ct);
            controller.transform.localScale = goalBodyScale;

            // 2. 공격 애니메이션 재생 (Index 2: Arm_Attack)
            view.PlayAnimation(global::PlayerState.ATTACK, 2);

            if (controller.EventBus != null)
            {
                controller.EventBus.Publish(new OnSoundRequested("Robot_Shoot"));
            }

            // 3. 투사체 생성
            if (data.ProjectilePrefab != null)
            {
                m_projectileFactory.Create(
                    data.ProjectilePrefab,
                    controller.transform.position,
                    8.0f, // 발사체 속도
                    data.ProjectilePushDistance,
                    player
                );
            }

            await UniTask.Delay(1000, cancellationToken: ct);
            
            // 4. 복귀 및 IDLE
            await controller.transform.DORotate(Vector3.zero, 0.3f).WithCancellation(ct);
            view.PlayAnimation(global::PlayerState.IDLE);
        }
        #endregion
    }
}
