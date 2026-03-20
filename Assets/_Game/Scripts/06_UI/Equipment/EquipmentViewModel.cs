using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using VContainer;
using Cysharp.Threading.Tasks;
using TowerBreakers.Player.Data;
using TowerBreakers.Player.Model;
using TowerBreakers.Player.Service;

namespace TowerBreakers.UI.Equipment
{
    /// <summary>
    /// [기능]: 장비 UI 뷰모델
    /// </summary>
    public class EquipmentViewModel : IDisposable
    {
        #region 내부 변수
        private readonly UserSessionModel m_userSession;
        private readonly IEquipmentService m_equipmentService;
        private readonly List<ItemSlotViewModel> m_itemSlotViewModels = new List<ItemSlotViewModel>();
        private readonly CancellationTokenSource m_cts = new CancellationTokenSource();

        private bool m_isDirty = false;
        #endregion

        #region 이벤트
        public event Action<IReadOnlyList<ItemSlotViewModel>> OnInventoryUpdated;
        public event Action<EquipmentType, ItemSlotViewModel> OnEquippedItemUpdated;
        public event Action<StatModifiers> OnStatsUpdated;
        #endregion

        #region 프로퍼티
        public IReadOnlyList<ItemSlotViewModel> ItemSlots => m_itemSlotViewModels;
        #endregion

        #region 초기화
        [Inject]
        public EquipmentViewModel(UserSessionModel userSession, IEquipmentService equipmentService)
        {
            m_userSession = userSession;
            m_equipmentService = equipmentService;

            SubscribeEvents();
            // 최초 로드 시 즉시 갱신 요청
            RequestRefresh();
        }

        private void SubscribeEvents()
        {
            m_userSession.OnInventoryChanged += OnDataChanged;
            m_userSession.OnEquipmentChanged += OnEquipmentDataChanged;
            m_userSession.OnStatsChanged += OnStatsDataChanged;
        }

        private void UnsubscribeEvents()
        {
            m_userSession.OnInventoryChanged -= OnDataChanged;
            m_userSession.OnEquipmentChanged -= OnEquipmentDataChanged;
            m_userSession.OnStatsChanged -= OnStatsDataChanged;
        }
        #endregion

        #region 이벤트 핸들러
        private void OnDataChanged()
        {
            RequestRefresh();
        }

        private void OnEquipmentDataChanged(EquipmentType type, string itemId)
        {
            RequestRefresh();
        }

        private void OnStatsDataChanged(StatModifiers stats)
        {
            RequestRefresh();
        }
        #endregion

        #region 핵심 로직 (갱신 및 최적화)
        /// <summary>
        /// [설명]: 갱신 요청을 보냅니다. 한 프레임에 여러 번 호출되어도 다음 프레임에 한 번만 실행됩니다.
        /// </summary>
        private void RequestRefresh()
        {
            if (m_isDirty) return;

            m_isDirty = true;
            RefreshAsync().Forget();
        }

        /// <summary>
        /// [설명]: 다음 프레임까지 대기한 후 실제 UI 데이터를 갱신합니다.
        /// </summary>
        private async UniTaskVoid RefreshAsync()
        {
            // 다음 프레임까지 대기하여 중복 호출을 모음
            await UniTask.Yield(PlayerLoopTiming.Update, m_cts.Token);

            if (m_cts.IsCancellationRequested) return;

            try
            {
                RefreshInventory();
                RefreshStats();
            }
            finally
            {
                m_isDirty = false;
            }
        }

        private void RefreshInventory()
        {
            m_itemSlotViewModels.Clear();

            var inventoryItems = m_equipmentService.GetInventoryItems();
            foreach (var item in inventoryItems)
            {
                var isEquipped = m_userSession.EquippedIds.Values.Contains(item.ID);
                var slotVm = new ItemSlotViewModel(item, isEquipped, m_equipmentService);
                m_itemSlotViewModels.Add(slotVm);
            }

            OnInventoryUpdated?.Invoke(m_itemSlotViewModels);
        }

        private void RefreshStats()
        {
            // 세션에 이미 계산된 최신 스탯을 사용
            var stats = m_userSession.CurrentStats;
            OnStatsUpdated?.Invoke(stats);

            // 장착된 아이템 정보들도 함께 갱신 알림
            foreach (EquipmentType type in System.Enum.GetValues(typeof(EquipmentType)))
            {
                var item = GetEquippedItem(type);
                OnEquippedItemUpdated?.Invoke(type, item);
            }
        }
        #endregion

        #region 공개 API
        public ItemSlotViewModel GetEquippedItem(EquipmentType type)
        {
            var itemData = m_equipmentService.GetEquippedItem(type);
            if (itemData == null) return null;
            return new ItemSlotViewModel(itemData, true, m_equipmentService);
        }

        public void EquipItem(string itemId)
        {
            m_equipmentService.Equip(itemId);
        }

        public void UnequipItem(EquipmentType type)
        {
            m_equipmentService.Unequip(type);
        }

        public void Dispose()
        {
            m_cts.Cancel();
            m_cts.Dispose();
            UnsubscribeEvents();
        }
        #endregion
    }
}
