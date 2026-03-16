using System.Collections.Generic;
using TowerBreakers.Player.Data.SO;

namespace TowerBreakers.DevTools
{
    /// <summary>
    /// [설명]: 아이템 치트 UI와 모델 사이를 연결하는 뷰모델 클래스입니다.
    /// UI에 표시할 데이터를 가공하고, 사용자의 입력(버튼 클릭 등)을 모델로 전달합니다.
    /// </summary>
    public class ItemCheatViewModel
    {
        #region 내부 필드
        private readonly ItemCheatModel m_model;
        #endregion

        #region 프로퍼티
        /// <summary>
        /// [설명]: UI에 표시할 무기 데이터 리스트입니다.
        /// </summary>
        public IReadOnlyList<WeaponData> Weapons { get; private set; }

        /// <summary>
        /// [설명]: UI에 표시할 갑주 데이터 리스트입니다.
        /// </summary>
        public IReadOnlyList<ArmorData> Armors { get; private set; }
        #endregion

        #region 생성자
        /// <summary>
        /// [설명]: 모델을 주입받아 뷰모델을 생성하고 데이터를 초기화합니다.
        /// </summary>
        /// <param name="model">아이템 치트 모델</param>
        public ItemCheatViewModel(ItemCheatModel model)
        {
            m_model = model;
            RefreshData();
        }
        #endregion

        #region 공개 메서드
        /// <summary>
        /// [설명]: 모델로부터 최신 아이템 데이터를 가져와 갱신합니다.
        /// </summary>
        public void RefreshData()
        {
            Weapons = m_model?.GetAvailableWeapons() ?? new List<WeaponData>();
            Armors = m_model?.GetAvailableArmors() ?? new List<ArmorData>();
        }

        /// <summary>
        /// [설명]: 무기 획득 버튼 클릭 시 호출됩니다.
        /// </summary>
        /// <param name="id">획득할 무기 ID</param>
        public void OnClickAcquireWeapon(string id)
        {
            m_model?.AcquireWeapon(id);
        }

        /// <summary>
        /// [설명]: 갑주 획득 버튼 클릭 시 호출됩니다.
        /// </summary>
        /// <param name="id">획득할 갑주 ID</param>
        public void OnClickAcquireArmor(string id)
        {
            m_model?.AcquireArmor(id);
        }
        #endregion
    }
}

