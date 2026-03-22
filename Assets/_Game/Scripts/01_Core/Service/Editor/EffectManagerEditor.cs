using UnityEngine;
using UnityEditor;
using TowerBreakers.Core.Service;

namespace TowerBreakers.Core.Editor
{
    /// <summary>
    /// [설명]: EffectManager 인스펙터에 테스트용 버튼을 추가하는 에디터 스크립트입니다.
    /// </summary>
    [CustomEditor(typeof(EffectManager))]
    public class EffectManagerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            // 기본 인스펙터 속성들을 먼저 그립니다.
            base.OnInspectorGUI();

            EffectManager manager = (EffectManager)target;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("테스트 도구 (런타임 전용)", EditorStyles.boldLabel);

            GUI.enabled = Application.isPlaying;
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("카메라 줌 테스트", GUILayout.Height(30)))
            {
                // Reflection을 통해 private 메서드 호출 혹은 public 메서드 호출
                // EffectManager에 이미 ContextMenu용으로 만든 로직이 있으므로 이를 활용하거나 
                // 해당 클래스의 public 메서드를 호출합니다.
                
                // manager.PlayCameraZoom(...)은 public이므로 직접 호출 가능하지만 
                // 인스펙터에 설정된 테스트 값을 사용해야 하므로 
                // EffectManager 내부에 테스트용 public 메서드를 하나 더 두는 것이 깔끔합니다.
                
                // 여기서는 일단 ContextMenu와 동일한 로직을 수행하도록 target의 메서드를 호출합니다.
                System.Reflection.MethodInfo method = typeof(EffectManager).GetMethod("ManualZoomTest", 
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                method?.Invoke(manager, null);
            }

            if (GUILayout.Button("카메라 리셋", GUILayout.Height(30)))
            {
                System.Reflection.MethodInfo method = typeof(EffectManager).GetMethod("ManualResetTest", 
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                method?.Invoke(manager, null);
            }
            EditorGUILayout.EndHorizontal();

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("테스트 버튼은 플레이 모드에서만 활성화됩니다.", MessageType.Info);
            }
            
            GUI.enabled = true;
        }
    }
}
