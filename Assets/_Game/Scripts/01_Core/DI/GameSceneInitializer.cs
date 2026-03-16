using UnityEngine;
using VContainer;
using TowerBreakers.Core.SceneManagement;
using TowerBreakers.Player.Data;
using TowerBreakers.UI.Equipment;

namespace TowerBreakers.Core.DI
{
    /// <summary>
    /// [설명]: 인게임 씬의 초기 진입점이며, 주입된 DTO를 사용해 시스템을 조립합니다.
    /// </summary>
    public class GameSceneInitializer : MonoBehaviour
    {
        #region 에디터 설정
        [SerializeField] private EquipmentView m_equipmentView;
        #endregion

        #region 초기화 로직
        [Inject]
        public void Initialize(
            UserSessionModel sessionModel,
            EquipmentViewModel equipmentViewModel,
            IObjectResolver resolver)
        {
            if (resolver.TryResolve<SceneContextDTO>(out var context))
            {
                if (context != null && context.Equipment != null)
                {
                    Debug.Log("[GameSceneInitializer] 주입된 DTO로부터 세션 동기화");
                    sessionModel.UpdateEquipment(context.Equipment);
                }
            }

            // 뷰 초기화 (만약 LifetimeScope에서 직접 하지 않는 경우)
            if (m_equipmentView != null)
            {
                m_equipmentView.Initialize(equipmentViewModel);
            }
        }
        #endregion
    }
}
