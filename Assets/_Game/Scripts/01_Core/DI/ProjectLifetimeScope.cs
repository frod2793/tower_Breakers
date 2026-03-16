using UnityEngine;
using VContainer;
using VContainer.Unity;
using TowerBreakers.Core;
using TowerBreakers.Core.Events;
using TowerBreakers.Player.Data;
using TowerBreakers.Core.SceneManagement;
using TowerBreakers.Sound.Data;
using TowerBreakers.Sound.Logic;
using TowerBreakers.Sound.View;

/// <summary>
/// [설명]: 어플리케이션 전역에서 유지되어야 하는 의존성을 관리하는 LifetimeScope입니다.
/// </summary>
public class ProjectLifetimeScope : LifetimeScope
{
    #region 에디터 설정
    [Header("전역 사운드 시스템")]
    [SerializeField, Tooltip("전역 사운드 데이터베이스")]
    private SoundDatabase m_soundDatabase;

    [SerializeField, Tooltip("전역 사운드 플레이어")]
    private SoundPlayer m_soundPlayer;
    #endregion

    protected override void Configure(IContainerBuilder builder)
    {
        // 1. 코어 엔진 시스템
        builder.Register<EventBus>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
        builder.Register<CooldownSystem>(Lifetime.Singleton).AsSelf();
        builder.Register<SceneLoader>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();

        // 2. 사용자 세션 및 데이터 관리
        builder.Register<UserSessionModel>(Lifetime.Singleton).AsSelf();
        builder.Register<SceneContextDTO>(Lifetime.Singleton).AsSelf();

        // 3. 전역 사운드 시스템 구성
        if (m_soundDatabase != null && m_soundPlayer != null)
        {
            builder.RegisterInstance(m_soundDatabase);
            builder.RegisterComponent(m_soundPlayer);
            builder.Register<SoundPresenter>(Lifetime.Singleton).AsSelf();

            builder.RegisterBuildCallback(resolver =>
            {
                resolver.Resolve<SoundPresenter>();
                Debug.Log("[ProjectLifetimeScope] 글로벌 시스템 초기화 완료");
            });
        }
    }
}
