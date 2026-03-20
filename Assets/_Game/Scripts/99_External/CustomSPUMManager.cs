using System.Collections.Generic;
using UnityEngine;
using TowerBreakers.Player.Data;
using TowerBreakers.Player.Model;
using TowerBreakers.Player.Service;

namespace TowerBreakers.SPUM
{
    /// <summary>
    /// [기능]: SPUM 캐릭터 외형 관리자 (인게임/로비 공용)
    /// </summary>
    public class CustomSPUMManager : MonoBehaviour
    {
        private UserSessionModel m_userSession;
        private IEquipmentService m_equipmentService;
        private SPUM_CharacterManager m_spumManager;

        public void Initialize(UserSessionModel userSession, IEquipmentService equipmentService)
        {
            m_userSession = userSession;
            m_equipmentService = equipmentService;
            m_spumManager = GetComponent<SPUM_CharacterManager>();
            if (m_spumManager == null) m_spumManager = GetComponentInChildren<SPUM_CharacterManager>();
            if (m_spumManager == null) m_spumManager = GameObject.FindAnyObjectByType<SPUM_CharacterManager>();

            if (m_spumManager != null)
            {
                m_spumManager.Initialize(userSession, equipmentService);
            }

            SubscribeEvents();
            Debug.Log("[CustomSPUMManager] 초기화 완료 및 SPUM 매니저 연결");
        }

        private void SubscribeEvents()
        {
            if (m_userSession != null)
            {
                m_userSession.OnEquipmentChanged += OnEquipmentChanged;
            }
        }

        private void UnsubscribeEvents()
        {
            if (m_userSession != null)
            {
                m_userSession.OnEquipmentChanged -= OnEquipmentChanged;
            }
        }

        private void OnEquipmentChanged(EquipmentType type, string itemId)
        {
            Debug.Log($"[CustomSPUMManager] OnEquipmentChanged 수신 - Type: {type}, ID: {itemId}");
            if (m_spumManager != null)
            {
                m_spumManager.UpdateEquipmentAppearance(type, itemId);
            }
            else
            {
                Debug.LogWarning("[CustomSPUMManager] m_spumManager가 null입니다!");
            }
        }

        public void UpdateSpumAppearance(EquipmentData data)
        {
            if (data == null) return;
            if (m_spumManager != null) m_spumManager.UpdateEquipmentAppearance(data.Type, data.ID);
        }

        private void OnDestroy()
        {
            UnsubscribeEvents();
        }
    }
}