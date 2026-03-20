using System;
using System.Collections.Generic;
using TowerBreakers.Player.Data;
using TowerBreakers.Player.Service;
using TowerBreakers.Player.Model;

namespace TowerBreakers.UI.ViewModel
{
    /// <summary>
    /// [설명]: 인게임 장비 테스트를 위한 치트 에디터 뷰모델입니다.
    /// </summary>
    public class CheatEquipmentViewModel
    {
        private readonly IEquipmentService m_equipmentService;
        private readonly UserSessionModel m_userSession;

        public event Action<IReadOnlyList<EquipmentData>> OnItemListUpdated;

        public CheatEquipmentViewModel(IEquipmentService equipmentService, UserSessionModel userSession)
        {
            m_equipmentService = equipmentService;
            m_userSession = userSession;
        }

        public void LoadAllItems()
        {
            var allItems = m_equipmentService.GetAllEquipmentData();
            OnItemListUpdated?.Invoke(allItems);
        }

        public void EquipItem(string itemId)
        {
            // 치트 기능이므로 인벤토리에 없어도 강제 추가 후 장착
            if (!m_userSession.HasItem(itemId))
            {
                m_userSession.AddItem(itemId);
            }
            m_equipmentService.Equip(itemId);
        }
    }
}