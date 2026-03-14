using UnityEditor;
using UnityEngine;

namespace TowerBreakers.Player.Logic
{
    /// <summary>
    /// [설명]: PlayerDebugger 컴포넌트의 인스펙터를 커스터마이징합니다.
    /// 세션 초기화 버튼 등을 추가하여 테스트 편의성을 높입니다.
    /// </summary>
    [CustomEditor(typeof(PlayerDebugger))]
    public class PlayerDebuggerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            // 기본 인스펙터 요소 출력
            DrawDefaultInspector();

            PlayerDebugger debugger = (PlayerDebugger)target;

            GUILayout.Space(10);
            EditorGUILayout.LabelField("세션 데이터 관리", EditorStyles.boldLabel);

            // 가로 레이아웃으로 버튼 배치
            EditorGUILayout.BeginHorizontal();
            
            GUI.color = new Color(1f, 0.8f, 0.4f); // 밝은 주황색 (경고 느낌)
            if (GUILayout.Button("보유 무기 초기화", GUILayout.Height(25)))
            {
                if (EditorUtility.DisplayDialog("무기 목록 초기화", "보유한 무기 목록만 삭제하시겠습니까?", "네", "아니오"))
                {
                    debugger.ResetOwnedWeapons();
                }
            }

            if (GUILayout.Button("보유 갑주 초기화", GUILayout.Height(25)))
            {
                if (EditorUtility.DisplayDialog("갑주 목록 초기화", "보유한 갑주 목록만 삭제하시겠습니까?", "네", "아니오"))
                {
                    debugger.ResetOwnedArmors();
                }
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(5);
            GUI.color = new Color(1f, 0.4f, 0.4f); // 붉은색 (위험 느낌)
            if (GUILayout.Button("전체 세션 데이터 강제 초기화 (위험)", GUILayout.Height(30)))
            {
                if (EditorUtility.DisplayDialog("전체 초기화", "모든 세션 데이터(소유 아이템, 장착 상태 등)를 완전히 삭제하시겠습니까?", "네, 초기화합니다", "아니오"))
                {
                    debugger.ResetFullSession();
                }
            }

            GUI.color = Color.white;
            GUILayout.Space(5);
            EditorGUILayout.HelpBox("초기화 후 아웃게임 화면에서 데이터가 즉시 갱신되지 않으면 씬을 다시 로드하거나 인벤토리에 다시 진입해 주세요.", MessageType.Info);
        }
    }
}
