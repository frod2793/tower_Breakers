using VContainer;
using VContainer.Unity;
using TowerBreakers.UI.Equipment;
using TowerBreakers.Core.Scene;

namespace TowerBreakers.Core.DI
{
    /// <summary>
    /// [기능]: 로비 씬 전용 의존성 주입 컨테이너
    /// </summary>
    public class LobbyLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            // 뷰모델 및 서비스 등록
            builder.Register<EquipmentViewModel>(Lifetime.Scoped);
            builder.Register<SceneTransitionService>(Lifetime.Scoped);
        }
    }
}
