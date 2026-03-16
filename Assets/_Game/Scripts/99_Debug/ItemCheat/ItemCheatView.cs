using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TowerBreakers.DevTools
{
    /// <summary>
    /// [설명]: 아이템 치트 UI를 화면에 그리고 사용자 입력을 처리하는 뷰 클래스입니다.
    /// OnGUI를 사용하여 디버그 전용 UI를 제공하며, 특정 키 입력으로 토글할 수 있습니다.
    /// </summary>
    public class ItemCheatView : MonoBehaviour
    {
        #region 에디터 설정
        [Header("표시 설정")]
        [SerializeField, Tooltip("특정 키를 눌렀을 때 UI를 표시할지 여부")]
        private bool m_showOnKeyPress = true;

        [SerializeField, Tooltip("UI 토글용 단축키 (기본: F2)")]
        private Key m_toggleKey = Key.F2;
        #endregion

        #region 내부 필드
        private ItemCheatViewModel m_viewModel;
        private bool m_isVisible;
        private Vector2 m_weaponScrollPosition;
        private Vector2 m_armorScrollPosition;

        // [최적화]: 매 프레임 생성을 방지하기 위한 스타일 캐싱
        private GUIStyle m_titleStyle;
        private GUIStyle m_sectionStyle;
        private GUIStyle m_infoStyle;
        #endregion

        #region 유니티 생명주기
        private void Update()
        {
            // [안전성]: 입력 시스템 및 키 설정 유효성 검사
            if (m_showOnKeyPress && Keyboard.current != null && m_toggleKey != Key.None)
            {
                try
                {
                    var keyControl = Keyboard.current[m_toggleKey];
                    if (keyControl != null && keyControl.wasPressedThisFrame)
                    {
                        Toggle();
                    }
                }
                catch (ArgumentOutOfRangeException)
                {
             
                }
            }
        }

        private void OnGUI()
        {
            if (!m_isVisible || !Application.isPlaying) return;

            DrawDebugUI();
        }
        #endregion

        #region 초기화 및 바인딩 로직
        /// <summary>
        /// [설명]: 뷰모델을 주입받고 데이터를 초기화합니다.
        /// </summary>
        /// <param name="viewModel">아이템 치트 뷰모델</param>
        public void SetViewModel(ItemCheatViewModel viewModel)
        {
            m_viewModel = viewModel;
            m_viewModel?.RefreshData();
        }
        #endregion

        #region 공개 메서드
        /// <summary>
        /// [설명]: 치트 UI를 강제로 표시합니다.
        /// </summary>
        public void Show()
        {
            m_isVisible = true;
            m_viewModel?.RefreshData();
        }

        /// <summary>
        /// [설명]: 치트 UI를 숨깁니다.
        /// </summary>
        public void Hide()
        {
            m_isVisible = false;
        }

        /// <summary>
        /// [설명]: Chi트 UI 표시 상태를 토글합니다.
        /// </summary>
        public void Toggle()
        {
            m_isVisible = !m_isVisible;
            if (m_isVisible)
            {
                m_viewModel?.RefreshData();
            }
        }
        #endregion

        #region 내부 로직
        /// <summary>
        /// [설명]: OnGUI 기반의 디버그 레이아웃을 그립니다.
        /// </summary>
        private void DrawDebugUI()
        {
            InitStyles();

            GUILayout.BeginVertical("box", GUILayout.Width(300));
            GUILayout.Space(10);

            GUILayout.Label("═══════════════════════════════════════════");
            GUILayout.Label("아이템 치트 UI", m_titleStyle);
            GUILayout.Label("═══════════════════════════════════════════");

            DrawWeaponSection();

            GUILayout.Label("─────────────────────────────────────────");

            DrawArmorSection();

            GUILayout.Label("─────────────────────────────────────────");
            GUILayout.Label($"({m_toggleKey} 키로 숨기기)", m_infoStyle);

            GUILayout.EndVertical();
        }

        /// <summary>
        /// [설명]: 사용 가능한 무기 목록 섹션을 그립니다.
        /// </summary>
        private void DrawWeaponSection()
        {
            GUILayout.Label("무기 목록", m_sectionStyle);

            var weapons = m_viewModel?.Weapons;
            if (weapons == null || weapons.Count == 0)
            {
                GUILayout.Label("  (장비 데이터가 없습니다)");
                return;
            }

            m_weaponScrollPosition = GUILayout.BeginScrollView(m_weaponScrollPosition, GUILayout.Height(200));
            foreach (var weapon in weapons)
            {
                if (weapon == null) continue;

                string id = weapon.name;
                string name = !string.IsNullOrEmpty(weapon.WeaponName) ? weapon.WeaponName : id;
                string label = $"[{weapon.Type}] {name}";

                if (GUILayout.Button(label))
                {
                    m_viewModel?.OnClickAcquireWeapon(id);
                }
            }
            GUILayout.EndScrollView();
        }

        /// <summary>
        /// [설명]: 사용 가능한 갑주 목록 섹션을 그립니다.
        /// </summary>
        private void DrawArmorSection()
        {
            GUILayout.Label("갑주 목록", m_sectionStyle);

            var armors = m_viewModel?.Armors;
            if (armors == null || armors.Count == 0)
            {
                GUILayout.Label("  (장비 데이터가 없습니다)");
                return;
            }

            m_armorScrollPosition = GUILayout.BeginScrollView(m_armorScrollPosition, GUILayout.Height(200));
            foreach (var armor in armors)
            {
                if (armor == null) continue;

                string id = armor.name;
                string name = !string.IsNullOrEmpty(armor.ArmorName) ? armor.ArmorName : id;
                string label = $"[{armor.Category}/{armor.Type}] {name}";

                if (GUILayout.Button(label))
                {
                    m_viewModel?.OnClickAcquireArmor(id);
                }
            }
            GUILayout.EndScrollView();
        }

        /// <summary>
        /// [설명]: UI 스타일을 지연 초기화(Lazy Init)합니다.
        /// </summary>
        private void InitStyles()
        {
            if (m_titleStyle != null) return;

            m_titleStyle = new GUIStyle(GUI.skin.label);
            m_titleStyle.fontSize = 14;
            m_titleStyle.fontStyle = FontStyle.Bold;
            m_titleStyle.alignment = TextAnchor.MiddleCenter;

            m_sectionStyle = new GUIStyle(GUI.skin.label);
            m_sectionStyle.fontSize = 12;
            m_sectionStyle.fontStyle = FontStyle.Bold;

            m_infoStyle = new GUIStyle(GUI.skin.label);
            m_infoStyle.fontSize = 10;
            m_infoStyle.fontStyle = FontStyle.Italic;
            m_infoStyle.alignment = TextAnchor.MiddleRight;
        }
        #endregion
    }
}

