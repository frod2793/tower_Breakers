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
            if (soundDatabase != null && soundPlayer != null)
            {
                builder.RegisterInstance(soundDatabase);
                builder.RegisterComponent(soundPlayer);
                
                // [수정]: Singleton으로 등록하고 AsSelf()를 통해 구체 타입으로 접근 가능하게 함
                builder.Register<SoundPresenter>(Lifetime.Singleton).AsSelf();
                
                // [추가]: 빌드 시점에 즉시 인스턴스를 생성(Eager Resolve)하여 생성자의 이벤트 구독이 실행되도록 함
                builder.RegisterBuildCallback(resolver =>
                {
                    resolver.Resolve<SoundPresenter>();
                    UnityEngine.Debug.Log("[SoundDIModule] SoundPresenter 즉시 인스턴스 생성 및 이벤트 구독 완료");
                });
            }
            else
            {
                UnityEngine.Debug.LogWarning("[SoundDIModule] SoundDatabase 또는 SoundPlayer가 설정되지 않았습니다.");
            }
        }
    }
}
