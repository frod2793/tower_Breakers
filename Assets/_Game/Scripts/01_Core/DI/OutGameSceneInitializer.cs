using UnityEngine;
using VContainer;
using TowerBreakers.Core.SceneManagement;
using TowerBreakers.Player.Data;
using TowerBreakers.UI.Screens;

namespace TowerBreakers.Core.DI
{
    /// <summary>
    /// [설명]: 아웃게임 씬의 초기 진입점입니다.
    /// SceneContextDTO로부터 UserSessionModel을 업데이트하여 아웃게임 UI에 최신 보유 목록이 표시되도록 합니다.
    /// </summary>
    public class OutGameSceneInitializer : MonoBehaviour
    {
        #region 에디터 설정
        [SerializeField] private OutGameView m_outGameView;
        #endregion

        #region 초기화 로직
        [Inject]
        public void Initialize(UserSessionModel sessionModel, OutGameViewModel outGameVM, IObjectResolver resolver)
        {
            Debug.Log("[TRACE] OutGameSceneInitializer.Initialize: 시작");

            // [표준화]: SceneContextDTO로부터 데이터를インポートし、UserSessionModelを更新
            if (resolver.TryResolve<SceneContextDTO>(out var context))
            {
                Debug.Log($"[TRACE] OutGameSceneInitializer: SceneContextDTO 발견. Equipment={(context.Equipment != null ? "not null" : "null")}");
                
                if (context != null && context.Equipment != null)
                {
                    Debug.Log($"[TRACE] OutGameSceneInitializer: ImportDTO 전 - 무기 {context.Equipment.OwnedWeaponIds.Count}개, 갑주 {context.Equipment.OwnedArmorIds.Count}개");
                    Debug.Log($"[TRACE] OutGameSceneInitializer: ImportDTO 전 데이터 - {JsonUtility.ToJson(context.Equipment)}");
                    
                    sessionModel.ImportDTO(context.Equipment);
                    
                    Debug.Log($"[TRACE] OutGameSceneInitializer: ImportDTO 후 - 무기 {sessionModel.CurrentEquipment.OwnedWeaponIds.Count}개, 갑주 {sessionModel.CurrentEquipment.OwnedArmorIds.Count}개");
                    Debug.Log($"[OutGameSceneInitializer] SceneContextDTOからインポート: 武器{context.Equipment.OwnedWeaponIds.Count}個, 甲冑{context.Equipment.OwnedArmorIds.Count}個");
                }
                else
                {
                    Debug.Log("[TRACE] OutGameSceneInitializer: SceneContextDTO.Equipment가 null - 기존 세션 데이터 유지");
                    Debug.Log("[OutGameSceneInitializer] SceneContextDTOのEquipmentがnull、既存のセッションデータを維持");
                }
            }
            else
            {
                Debug.LogWarning("[TRACE] OutGameSceneInitializer: SceneContextDTO를 찾을 수 없음 - 기존 세션 데이터 유지");
                Debug.LogWarning("[OutGameSceneInitializer] SceneContextDTOが見つからず、既存のセッションデータを維持");
            }

            if (m_outGameView != null)
            {
                m_outGameView.Initialize(outGameVM);
                Debug.Log("[OutGameSceneInitializer] OutGameView初期化完了");
            }
            
            Debug.Log("[TRACE] OutGameSceneInitializer.Initialize: 완료");
        }
        #endregion
    }
}
