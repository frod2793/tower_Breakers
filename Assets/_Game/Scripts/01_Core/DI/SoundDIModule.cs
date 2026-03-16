using TowerBreakers.Sound.Data;
using TowerBreakers.Sound.Logic;
using TowerBreakers.Sound.View;
using VContainer;
using VContainer.Unity;

namespace TowerBreakers.Core.DI
{
    /// <summary>
    /// [설명]: Sound 시스템 DI 모듈입니다.
    /// </summary>
    public static class SoundDIModule
    {
        public static void Register(IContainerBuilder builder, SoundDatabase soundDatabase, SoundPlayer soundPlayer)
        {
            // [설명]: 전역 사운드 시스템은 ProjectLifetimeScope에서 관리하는 것을 권장합니다.
            // 씬마다 개별 사운드 시스템이 필요한 경우에만 아래 로직이 동작합니다.
            if (soundDatabase != null && soundPlayer != null)
            {
                builder.RegisterInstance(soundDatabase);
                builder.RegisterComponent(soundPlayer);
                builder.Register<SoundPresenter>(Lifetime.Singleton).AsSelf();
                
                builder.RegisterBuildCallback(resolver =>
                {
                    // 전역 스코프에 이미 존재한다면 Resolve 시 동일한 인스턴스가 반환됩니다.
                    resolver.Resolve<SoundPresenter>();
                });
            }
        }
    }
}
