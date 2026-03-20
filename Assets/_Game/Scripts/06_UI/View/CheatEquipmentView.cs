using UnityEngine;
using UnityEngine.UI;
using TowerBreakers.UI.ViewModel;
using TowerBreakers.Player.Data;
using System.Collections.Generic;
using VContainer;

namespace TowerBreakers.UI.View
{
    /// <summary>
    /// [설명]: 인게임 장비 테스트를 위한 치트 에디터 UI 뷰입니다.
    /// </summary>
    public class CheatEquipmentView : MonoBehaviour
    {
        [Header("UI 참조")]
        [SerializeField] private GameObject m_panel;
        [SerializeField] private Transform m_contentRoot;
        [SerializeField] private GameObject m_itemPrefab;
        [SerializeField] private Button m_closeButton;

        private CheatEquipmentViewModel m_viewModel;

        [Inject]
        public void Construct(CheatEquipmentViewModel viewModel)
        {
            m_viewModel = viewModel;
            m_viewModel.OnItemListUpdated += UpdateItemList;
        }

        private void Start()
        {
            m_closeButton.onClick.AddListener(() => m_panel.SetActive(false));
            m_panel.SetActive(false);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F1))
            {
                TogglePanel();
            }
        }

        public void TogglePanel()
        {
            bool active = !m_panel.activeSelf;
            m_panel.SetActive(active);
            if (active)
            {
                m_viewModel.LoadAllItems();
            }
        }

        private void UpdateItemList(IReadOnlyList<EquipmentData> items)
        {
            // 기존 리스트 초기화
            foreach (Transform child in m_contentRoot)
            {
                Destroy(child.gameObject);
            }

            // 아이템 버튼 생성
            foreach (var item in items)
            {
                GameObject go = Instantiate(m_itemPrefab, m_contentRoot);
                var text = go.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                if (text != null) text.text = $"[{item.Type}] {item.ItemName}";

                var button = go.GetComponent<Button>();
                if (button != null)
                {
                    button.onClick.AddListener(() => m_viewModel.EquipItem(item.ID));
                }
            }
        }
    }
}