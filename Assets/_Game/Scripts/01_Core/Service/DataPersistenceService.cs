using UnityEngine;
using System.IO;
using TowerBreakers.Player.Model;
using TowerBreakers.Player.DTO;
using TowerBreakers.Player.Data;
using System.Collections.Generic;

namespace TowerBreakers.Core.Service
{
    /// <summary>
    /// [설명]: 게임 데이터를 로컬 저장소에 JSON 형태로 저장하고 불러오는 서비스입니다.
    /// </summary>
    public class DataPersistenceService
    {
        private readonly string m_savePath;
        private const string FILE_NAME = "tb_save_data.json";

        public DataPersistenceService()
        {
            m_savePath = Path.Combine(Application.persistentDataPath, FILE_NAME);
        }

        public void Save(UserSessionModel model)
        {
            if (model == null) return;

            SaveDataDTO dto = new SaveDataDTO();
            dto.Gold = model.Gold;
            dto.InventoryIds.AddRange(model.InventoryIds);

            foreach (var pair in model.EquippedIds)
            {
                dto.EquippedSlots.Add(new SaveDataDTO.EquipmentSlotData 
                { 
                    Type = pair.Key, 
                    ItemId = pair.Value 
                });
            }

            string json = JsonUtility.ToJson(dto, true);
            File.WriteAllText(m_savePath, json);
            Debug.Log($"[Persistence] 데이터 저장 완료: {m_savePath}");
        }

        public void Load(UserSessionModel model)
        {
            if (model == null) return;

            if (!File.Exists(m_savePath))
            {
                Debug.Log("[Persistence] 저장된 데이터가 없습니다. 초기 상태를 유지합니다.");
                return;
            }

            try
            {
                string json = File.ReadAllText(m_savePath);
                SaveDataDTO dto = JsonUtility.FromJson<SaveDataDTO>(json);

                model.Clear();
                model.Gold = dto.Gold;
                
                // [주의]: AddItem 등의 메서드가 이벤트를 발생시키므로 로드 시에는 신중해야 함
                foreach (var id in dto.InventoryIds)
                {
                    model.AddItem(id);
                }

                foreach (var slot in dto.EquippedSlots)
                {
                    model.SetEquip(slot.Type, slot.ItemId);
                }

                Debug.Log("[Persistence] 데이터 로드 완료");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Persistence] 데이터 로드 실패: {e.Message}");
            }
        }
    }
}