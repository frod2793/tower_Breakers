using TowerBreakers.Environment.Logic;
using TowerBreakers.Environment.View;
using VContainer;
using VContainer.Unity;

namespace TowerBreakers.Core.DI
{
    /// <summary>
    /// [설명]: Environment 시스템 DI 모듈입니다.
    /// </summary>
    public static class EnvironmentDIModule
    {
        public static void Register(IContainerBuilder builder, EnvironmentManager environmentManager)
        {
            if (environmentManager != null)
            {
                builder.RegisterComponent(environmentManager);
            }
            else
            {
                UnityEngine.Debug.LogWarning("[EnvironmentDIModule] EnvironmentManager가 설정되지 않았습니다.");
            }
        }
    }
}
