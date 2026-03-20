using UnityEngine;
using UnityEditor;
using TowerBreakers.Player.Controller;

namespace TowerBreakers.Editor
{
    [CustomEditor(typeof(PlayerDebugger))]
    public class PlayerDebuggerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            PlayerDebugger debugger = (PlayerDebugger)target;

            GUILayout.Space(10);
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("Add Selected Item", GUILayout.Height(30)))
            {
                debugger.AddSelectedItem();
            }

            GUILayout.Space(5);
            GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
            if (GUILayout.Button("Reset Inventory & Equipment", GUILayout.Height(30)))
            {
                if (EditorApplication.isPlaying)
                {
                    if (EditorUtility.DisplayDialog("인벤토리 초기화", "정말로 모든 인벤토리와 장착 장비를 삭제하시겠습니까?", "예", "아니오"))
                    {
                        debugger.ClearInventory();
                    }
                }
                else
                {
                    Debug.LogWarning("[PlayerDebugger] 초기화는 플레이 모드에서만 가능합니다.");
                }
            }
            GUI.backgroundColor = Color.white;
        }
    }
}