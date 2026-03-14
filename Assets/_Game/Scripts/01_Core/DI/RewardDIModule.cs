using TowerBreakers.Player.Data.SO;
using TowerBreakers.Combat.Logic;
using TowerBreakers.Interactions.ViewModel;
using TowerBreakers.Interactions.Logic;
using VContainer;
using VContainer.Unity;

namespace TowerBreakers.Core.DI
{
    /// <summary>
    /// [설명]: Reward 시스템 DI 모듈입니다.
    /// </summary>
    public static class RewardDIModule
    {
        public static void Register(IContainerBuilder builder, RewardTableData rewardTable)
        {
            if (rewardTable != null)
            {
                // [수정]: AsSelf()를 추가하여 구체 타입으로도 resolve 가능하게 하고,
                // RegisterBuildCallback으로 빌드 시점에 즉시 인스턴스를 생성하여
                // 생성자의 이벤트 구독(OnRewardChestOpened)이 실행되도록 합니다.
                builder.Register<RewardApplier>(Lifetime.Singleton)
                    .AsSelf()
                    .AsImplementedInterfaces()
                    .WithParameter(rewardTable);

                builder.RegisterBuildCallback(resolver =>
                {
                    // [설명]: Eager resolve - 빌드 시점에 RewardApplier를 생성하여 이벤트 구독을 보장합니다.
                    resolver.Resolve<RewardApplier>();
                    UnityEngine.Debug.Log("[RewardDIModule] RewardApplier 즉시 인스턴스 생성 완료");
                });
            }
            else
            {
                UnityEngine.Debug.LogError("[RewardDIModule] RewardTableData가 설정되지 않았습니다.");
            }

            builder.Register<RewardChestViewModel>(Lifetime.Transient);
        }
    }
}
