using UnityEngine;
using VContainer;
using TowerBreakers.Core.SceneManagement;
using TowerBreakers.Player.Data;
using TowerBreakers.UI.Equipment;

namespace TowerBreakers.Core.DI
{
    /// <summary>
    /// [설명]: 인게임 씬의 초기 진입점이며, 주입된 DTO를 사용해 플레이어의 초기 장비 및 인벤토리 상태를 구성합니다.
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
            // [표준화]: SceneContextDTO로부터 데이터를インポートし、UserSessionModelを更新
            if (resolver.TryResolve<SceneContextDTO>(out var context))
            {
                if (context != null && context.Equipment != null)
                {
                    Debug.Log($"[GameSceneInitializer] SceneContextDTOからインポート: 武器{context.Equipment.OwnedWeaponIds.Count}個, 甲冑{context.Equipment.OwnedArmorIds.Count}個");
                    sessionModel.ImportDTO(context.Equipment);
                }
                else
                {
                    Debug.Log("[GameSceneInitializer] SceneContextDTOのEquipmentがnull、既存のセッションデータを維持");
                }
            }
            else
            {
                Debug.LogWarning("[GameSceneInitializer] SceneContextDTOが見つからず、既存のセッションデータを維持");
            }

            // ビュー初期化
            if (m_equipmentView != null)
            {
                m_equipmentView.Initialize(equipmentViewModel);
                Debug.Log("[GameSceneInitializer] EquipmentView初期化完了");
            }
        }
        #endregion
    }
}
