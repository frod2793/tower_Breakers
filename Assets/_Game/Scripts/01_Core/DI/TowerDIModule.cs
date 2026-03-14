using TowerBreakers.Tower.Logic;
using TowerBreakers.Tower.Data;
using TowerBreakers.Player.Data.SO;
using TowerBreakers.Player.Data;
using VContainer;
using VContainer.Unity;

namespace TowerBreakers.Core.DI
{
    /// <summary>
    /// [설명]: Tower 시스템 DI 모듈입니다.
    /// </summary>
    public static class TowerDIModule
    {
        public static void Register(IContainerBuilder builder, TowerData towerData, EquipmentDatabase equipmentDatabase)
        {
            if (towerData != null)
            {
                builder.RegisterInstance(towerData);
            }
            else
            {
                UnityEngine.Debug.LogError("[TowerDIModule] TowerData가 설정되지 않았습니다!");
            }

            builder.Register<TowerManager>(Lifetime.Singleton);

            if (equipmentDatabase != null)
            {
                builder.RegisterInstance(equipmentDatabase);
            }
            else
            {
                UnityEngine.Debug.LogWarning("[TowerDIModule] EquipmentDatabase가 설정되지 않았습니다.");
            }
        }
    }
}
