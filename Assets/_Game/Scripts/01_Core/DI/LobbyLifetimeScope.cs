using VContainer;
using VContainer.Unity;
using TowerBreakers.UI.Equipment;
using TowerBreakers.Core.Scene;
using UnityEngine;

namespace TowerBreakers.Core.DI
{
    /// <summary>
    /// [기능]: 로비 씬 전용 의존성 주입 컨테이너
    /// </summary>
    public class LobbyLifetimeScope : LifetimeScope
    {
        [SerializeField] private UI.View.EquipmentView m_equipmentView;
        [SerializeField, Tooltip("로비에 배치된 플레이어 캐릭터")] 
        private SPUM.CustomSPUMManager m_lobbyCharacterManager;

        protected override void Configure(IContainerBuilder builder)
        {
            // 뷰모델 및 서비스 등록
            builder.Register<EquipmentViewModel>(Lifetime.Scoped);
            builder.Register<SceneTransitionService>(Lifetime.Scoped);

            if (m_equipmentView != null) builder.RegisterComponent(m_equipmentView);
            if (m_lobbyCharacterManager != null) builder.RegisterComponent(m_lobbyCharacterManager);

            // 로비 진입점 등록
            builder.RegisterEntryPoint<LobbyController>();
        }
    }
}
