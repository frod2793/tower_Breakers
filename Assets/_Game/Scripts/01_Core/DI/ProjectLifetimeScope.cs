using UnityEngine;
using VContainer;
using VContainer.Unity;
using TowerBreakers.Player.Data;
using TowerBreakers.Player.Model;
using TowerBreakers.Player.Service;
using TowerBreakers.Core.Events;

namespace TowerBreakers.Core.DI
{
    /// <summary>
    /// [기능]: 프로젝트 전역 의존성 주입 컨테이너
    /// </summary>
    public class ProjectLifetimeScope : LifetimeScope
    {
        #region 에디터 설정
        [SerializeField, Tooltip("장비 데이터베이스 에셋")]
        private EquipmentDatabase m_equipmentDatabase;
        #endregion

        protected override void Configure(IContainerBuilder builder)
        {
            // [코어]: 이벤트 버스 싱글톤 등록
            builder.Register<IEventBus, EventBus>(Lifetime.Singleton);

            // 데이터 모델 등록
            builder.Register<UserSessionModel>(Lifetime.Singleton);

            // 데이터베이스 인스턴스 등록
            if (m_equipmentDatabase != null)
            {
                builder.RegisterInstance(m_equipmentDatabase);
            }
            else
            {
                Debug.LogWarning("[ProjectLifetimeScope] EquipmentDatabase가 할당되지 않았습니다.");
            }

            // 서비스 등록
            builder.Register<IEquipmentService, EquipmentService>(Lifetime.Singleton);
        }
    }
}
