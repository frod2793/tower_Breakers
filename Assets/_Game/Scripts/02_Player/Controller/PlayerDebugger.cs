using UnityEngine;
using TowerBreakers.Player.Data;
using TowerBreakers.Player.Model;
using TowerBreakers.Player.Service;
using VContainer;

namespace TowerBreakers.Player.Controller
{
    /// <summary>
    /// [설명]: 인스펙터 상에서 플레이어 데이터를 조작하기 위한 디버그 도구입니다.
    /// </summary>
    /// <summary>
    /// [주의]: 이 컴포넌트는 ProjectLifetimeScope와 같은 오브젝트에 배치하거나 
    /// ProjectLifetimeScope 인스펙터의 PlayerDebugger 필드에 직접 할당해야 합니다.
    /// </summary>
    public class PlayerDebugger : MonoBehaviour
    {
        [Header("치트 설정")]
        [SerializeField, Tooltip("추가할 아이템 에셋")] 
        private EquipmentData m_targetItem;

        private IEquipmentService m_equipmentService;
        private UserSessionModel m_userSession;

        [Inject]
        public void Construct(IEquipmentService equipmentService, UserSessionModel userSession)
        {
            m_equipmentService = equipmentService;
            m_userSession = userSession;
        }

        public void AddSelectedItem()
        {
            if (m_targetItem == null)
            {
                Debug.LogWarning("[PlayerDebugger] 추가할 아이템이 설정되지 않았습니다.");
                return;
            }

            if (m_userSession != null)
            {
                m_userSession.AddItem(m_targetItem.ID);
                Debug.Log($"[PlayerDebugger] 아이템 추가 완료: {m_targetItem.ItemName}");
            }
        }

        public void ClearInventory()
        {
            if (m_userSession != null)
            {
                m_userSession.Clear();
                // 장비 서비스의 저장 로직 호출 (이벤트를 통해 저장되나 명시적으로 확인)
                if (m_equipmentService is EquipmentService service) service.SaveData();
                Debug.Log("[PlayerDebugger] 인벤토리 및 장착 정보가 초기화되었습니다.");
            }
        }
    }
}