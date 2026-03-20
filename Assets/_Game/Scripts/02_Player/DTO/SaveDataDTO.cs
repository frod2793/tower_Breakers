using System;
using System.Collections.Generic;
using TowerBreakers.Player.Data;

namespace TowerBreakers.Player.DTO
{
    /// <summary>
    /// [설명]: 영구 저장될 사용자 데이터를 담는 DTO 클래스입니다.
    /// </summary>
    [Serializable]
    public class SaveDataDTO
    {
        public int Gold;
        public List<string> InventoryIds = new List<string>();
        public List<EquipmentSlotData> EquippedSlots = new List<EquipmentSlotData>();

        [Serializable]
        public class EquipmentSlotData
        {
            public EquipmentType Type;
            public string ItemId;
        }
    }
}