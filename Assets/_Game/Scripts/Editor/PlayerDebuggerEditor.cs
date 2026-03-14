using UnityEngine;
using UnityEditor;
using TowerBreakers.Player.Logic;
using TowerBreakers.Player.Data.SO;
using System.Collections.Generic;
using System.Linq;

namespace TowerBreakers.Editor
{
    /// <summary>
    /// [설명]: PlayerDebugger 컴포넌트를 위한 커스텀 에디터입니다.
    /// 실시간 스탯 표시 및 장비 강제 교체 기능을 제공합니다.
    /// </summary>
    [CustomEditor(typeof(PlayerDebugger))]
    public class PlayerDebuggerEditor : UnityEditor.Editor
    {
        #region 내부 필드
        private PlayerDebugger m_target;
        private List<WeaponData> m_allWeapons;
        private List<ArmorData> m_allArmors;
        private bool m_showStats = true;
        private bool m_showInventory = true;
        private bool m_showAssetBrowser = true;
        #endregion

        #region 유니티 생명주기
        private void OnEnable()
        {
            m_target = (PlayerDebugger)target;
            RefreshAssetLists();
        }

        public override void OnInspectorGUI()
        {
            if (m_target == null) return;

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("⚔️ Tower Breakers Player Debugger", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("플레이 모드에서만 실시간 데이터를 확인할 수 있습니다.", MessageType.Info);
                return;
            }

            if (m_target.PlayerModel == null)
            {
                EditorGUILayout.HelpBox("PlayerModel이 주입되지 않았습니다. (Dependency Injection 확인 필요)", MessageType.Warning);
                return;
            }

            DrawStatsSection();
            DrawInventorySection();
            DrawAssetBrowserSection();

            if (GUI.changed)
            {
                Repaint();
            }
        }
        #endregion

        #region 내부 로직
        /// <summary>
        /// [설명]: 플레이어의 현재 스탯을 표 형식으로 표시합니다.
        /// </summary>
        private void DrawStatsSection()
        {
            m_showStats = EditorGUILayout.BeginFoldoutHeaderGroup(m_showStats, "📊 실시간 스탯 (Runtime Stats)");
            if (m_showStats)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                
                var model = m_target.PlayerModel;
                var data = m_target.PlayerData;

                DrawStatRow("💖 생명력 (Life)", $"{model.CurrentLifeCount} / {model.MaxLifeCount}");
                
                if (data != null)
                {
                    int finalAttack = model.FinalAttackPower(data.AttackPower);
                    float finalRange = model.FinalAttackRange(data.AttackRange);
                    float finalSpeed = model.FinalAttackSpeed(data.AttackSpeed);
                    float finalMove = model.FinalMoveSpeed(data.MoveSpeed);

                    DrawStatRow("⚔️ 공격력 (Atk)", $"{finalAttack} (기본 {data.AttackPower})");
                    DrawStatRow("📏 공격 사거리 (Range)", $"{finalRange:F1} (기본 {data.AttackRange:F1})");
                    DrawStatRow("⚡ 공격 속도 (Speed)", $"{finalSpeed:F1} (기본 {data.AttackSpeed:F1})");
                    DrawStatRow("🏃 이동 속도 (Move)", $"{finalMove:F1} (기본 {data.MoveSpeed:F1})");
                    DrawStatRow("➕ 생명력 보너스 (Life Bonus)", $"+{model.FinalLifeBonus}");
                    DrawStatRow("💪 밀기 저항 (Push Resistance)", $"{(model.FinalPushResistance * 100f):F0}%");
                }

                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        /// <summary>
        /// [설명]: 현재 인벤토리 모델의 아이템 목록을 표시하고 즉시 장착 기능을 제공합니다.
        /// </summary>
        private void DrawInventorySection()
        {
            m_showInventory = EditorGUILayout.BeginFoldoutHeaderGroup(m_showInventory, "🎒 현재 인벤토리 (Inventory Model)");
            if (m_showInventory)
            {
                var inv = m_target.InventoryModel;
                if (inv == null)
                {
                    EditorGUILayout.LabelField("InventoryModel이 null입니다.");
                }
                else
                {
                    EditorGUILayout.LabelField($"보유 무기: {inv.OwnedWeapons.Count}개", EditorStyles.miniLabel);
                    foreach (var w in inv.OwnedWeapons)
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField($"- {w.WeaponName} ({w.Type})");
                        if (GUILayout.Button("장착 (Equip)", GUILayout.Width(80)))
                        {
                            m_target.ForceEquipWeapon(w);
                        }
                        EditorGUILayout.EndHorizontal();
                    }

                    EditorGUILayout.Space(5);
                    EditorGUILayout.LabelField($"보유 갑주: {inv.OwnedArmors.Count}개", EditorStyles.miniLabel);
                    foreach (var a in inv.OwnedArmors)
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField($"- {a.ArmorName} ({a.Category})");
                        if (GUILayout.Button("장착 (Equip)", GUILayout.Width(80)))
                        {
                            m_target.ForceEquipArmor(a);
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        /// <summary>
        /// [설명]: 프로젝트 내의 모든 장비 에셋을 검색하여 인벤토리에 추가할 수 있게 합니다.
        /// </summary>
        private void DrawAssetBrowserSection()
        {
            m_showAssetBrowser = EditorGUILayout.BeginFoldoutHeaderGroup(m_showAssetBrowser, "📁 장비 에셋 브라우저 (All Assets)");
            if (m_showAssetBrowser)
            {
                if (GUILayout.Button("에셋 목록 새로고침 (Refresh Asset List)"))
                {
                    RefreshAssetLists();
                }

                EditorGUILayout.Space(2);
                EditorGUILayout.LabelField("프로젝트의 모든 무기 데이터:", EditorStyles.miniBoldLabel);
                foreach (var w in m_allWeapons)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(w.WeaponName, GUILayout.Width(150));
                    if (GUILayout.Button("인벤토리 추가", GUILayout.Width(100)))
                    {
                        m_target.AddToInventory(w);
                    }
                    if (GUILayout.Button("즉시 장착", GUILayout.Width(80)))
                    {
                        m_target.AddToInventory(w);
                        m_target.ForceEquipWeapon(w);
                    }
                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("프로젝트의 모든 갑주 데이터:", EditorStyles.miniBoldLabel);
                foreach (var a in m_allArmors)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"{a.ArmorName} ({a.Category})", GUILayout.Width(150));
                    if (GUILayout.Button("인벤토리 추가", GUILayout.Width(100)))
                    {
                        m_target.AddToInventory(a);
                    }
                    if (GUILayout.Button("즉시 장착", GUILayout.Width(80)))
                    {
                        m_target.AddToInventory(a);
                        m_target.ForceEquipArmor(a);
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void DrawStatRow(string label, string value)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.Width(150));
            EditorGUILayout.LabelField(value, EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();
        }

        private void RefreshAssetLists()
        {
            m_allWeapons = AssetDatabase.FindAssets("t:WeaponData")
                .Select(guid => AssetDatabase.LoadAssetAtPath<WeaponData>(AssetDatabase.GUIDToAssetPath(guid)))
                .ToList();

            m_allArmors = AssetDatabase.FindAssets("t:ArmorData")
                .Select(guid => AssetDatabase.LoadAssetAtPath<ArmorData>(AssetDatabase.GUIDToAssetPath(guid)))
                .ToList();
        }
        #endregion
    }
}
