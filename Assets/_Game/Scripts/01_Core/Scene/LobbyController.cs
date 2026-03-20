using UnityEngine;
using VContainer.Unity;
using TowerBreakers.SPUM;
using TowerBreakers.Player.Model;
using TowerBreakers.Player.Service;

namespace TowerBreakers.Core.Scene
{
    /// <summary>
    /// [설명]: 로비 씬의 초기화 및 흐름을 관리하는 컨트롤러입니다.
    /// </summary>
    public class LobbyController : IStartable
    {
        private readonly CustomSPUMManager m_characterManager;
        private readonly UserSessionModel m_userSession;
        private readonly IEquipmentService m_equipmentService;

        public LobbyController(
            CustomSPUMManager characterManager, 
            UserSessionModel userSession, 
            IEquipmentService equipmentService)
        {
            m_characterManager = characterManager;
            m_userSession = userSession;
            m_equipmentService = equipmentService;
        }

        public void Start()
        {
            Debug.Log("[LobbyController] Start 호출됨");
            if (m_characterManager != null)
            {
                Debug.Log("[LobbyController] 로비 캐릭터 외형 초기화 시작");
                m_characterManager.Initialize(m_userSession, m_equipmentService);
            }
            else
            {
                Debug.LogError("[LobbyController] m_characterManager가 null입니다! LobbyLifetimeScope 할당을 확인하세요.");
            }
        }
    }
}