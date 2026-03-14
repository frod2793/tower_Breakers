 using UnityEngine;
 using TowerBreakers.Core.Performance;
using VContainer;
using TowerBreakers.Player.Data.Models;
using TowerBreakers.Player.Data.SO;
using TowerBreakers.Player.View;

namespace TowerBreakers.Player.Logic
{
    /// <summary>
    /// [설명]: 에디터 상에서 플레이어의 런타임 데이터(스탯, 장비)를 디버깅하고 테스트하기 위한 컴포넌트입니다.
    /// VContainer를 통해 주요 모델들을 주입받습니다.
    /// </summary>
public class PlayerDebugger : MonoBehaviour
{
        [SerializeField]
        private bool m_enableFrameDropLogs = false;
        #region 내부 변수
        private PlayerModel m_playerModel;
        private InventoryModel m_inventoryModel;
        private PlayerData m_playerData;
        private PlayerView m_playerView;
        #endregion

        #region 프로퍼티
        public PlayerModel PlayerModel => m_playerModel;
        public InventoryModel InventoryModel => m_inventoryModel;
        public PlayerData PlayerData => m_playerData;
        public PlayerView PlayerView => m_playerView;
        #endregion

        #region 초기화
        /// <summary>
        /// [설명]: VContainer에서 의존성을 주입받습니다.
        /// </summary>
        [Inject]
        public void Construct(
            PlayerModel playerModel, 
            InventoryModel inventoryModel, 
            PlayerData playerData,
            PlayerView playerView)
        {
            m_playerModel = playerModel;
            m_inventoryModel = inventoryModel;
            m_playerData = playerData;
            m_playerView = playerView;

            Debug.Log("[PlayerDebugger] Dependencies Injected.");
            // 런타임 로그 활성화 상태 동기화
            FrameDropMonitor.SetLoggingEnabled(m_enableFrameDropLogs);
        }

        /// <summary>
        /// [설명]: Inspector에서 설정한 Frame Drop 로깅 활성 여부를 런타임에 반영합니다.
        /// </summary>
        public bool FrameDropLogsEnabled
        {
            get => m_enableFrameDropLogs;
            set
            {
                m_enableFrameDropLogs = value;
                FrameDropMonitor.SetLoggingEnabled(value);
            }
        }
        #endregion

        #region 공개 API
        /// <summary>
        /// [설명]: 특정 무기를 강제로 장착시킵니다.
        /// </summary>
        public void ForceEquipWeapon(WeaponData weapon)
        {
            if (m_playerModel == null) return;
            m_playerModel.SetWeapon(weapon);
        }

        /// <summary>
        /// [설명]: 특정 갑주를 강제로 장착시킵니다.
        /// </summary>
        public void ForceEquipArmor(ArmorData armor)
        {
            if (m_playerModel == null) return;
            m_playerModel.SetArmor(armor);
        }

        /// <summary>
        /// [설명]: 인벤토리에 아이템을 강제로 추가합니다.
        /// </summary>
        public void AddToInventory(ScriptableObject item)
        {
            if (m_inventoryModel == null) return;

            if (item is WeaponData weapon)
            {
                m_inventoryModel.AddWeapon(weapon);
            }
            else if (item is ArmorData armor)
            {
                m_inventoryModel.AddArmor(armor);
            }
        }
        #endregion
    }
}
