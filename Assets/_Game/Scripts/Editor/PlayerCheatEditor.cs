using UnityEngine;
using UnityEditor;
using TowerBreakers.Player.View;
using TowerBreakers.Player.Data;
using TowerBreakers.Player.Service;
using TowerBreakers.Player.Logic;

namespace TowerBreakers.Editor
{
    [CustomEditor(typeof(PlayerView))]
    public class PlayerCheatEditor : UnityEditor.Editor
    {
        private EquipmentDatabase m_db;

        private void OnEnable()
        {
            // DB 에셋 자동 찾기
            string[] guids = AssetDatabase.FindAssets("t:EquipmentDatabase");
            if (guids.Length > 0)
            {
                m_db = AssetDatabase.LoadAssetAtPath<EquipmentDatabase>(AssetDatabase.GUIDToAssetPath(guids[0]));
            }
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (!Application.isPlaying) return;

            PlayerView view = (PlayerView)target;
            if (view.Logic == null) return;
            
            GUILayout.Space(20);
            GUILayout.Label("--- Cheat Tool (Inspector) ---", EditorStyles.boldLabel);

            if (GUILayout.Button("Full Health"))
            {
                view.Logic.InitializeHealth(view.Logic.State.MaxHealth);
            }

            if (m_db == null)
            {
                EditorGUILayout.HelpBox("EquipmentDatabase를 찾을 수 없습니다.", MessageType.Warning);
                return;
            }

            GUILayout.Space(10);
            GUILayout.Label("Equip Weapons:");

            foreach (var weapon in m_db.Weapons)
            {
                if (weapon == null) continue;

                if (GUILayout.Button($"Equip: {weapon.ItemName}"))
                {
                    // 치트 장착 로직: EquipmentService를 통해 장착
                    // DI를 통해 주입된 서비스를 가져오거나, Logic을 통해 접근
                    view.CheatEquip(weapon.ID);
                    Debug.Log($"[Cheat] 장착 완료: {weapon.ItemName}");
                    // 실제 장착은 IEquipmentService가 필요함. PlayerView에서 노출하거나 VContainer 캐시 확인 필요
                }
            }
        }
    }
}