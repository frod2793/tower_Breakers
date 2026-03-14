#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace TowerBreakers.Enemy.Boss.View
{
    /// <summary>
    /// [설명]: BossIntroCutscene 인스펙터 편집용 에디터 스크립트입니다.
    /// </summary>
    [CustomEditor(typeof(BossIntroCutscene))]
    public class BossIntroCutsceneEditor : Editor
    {
        private BossIntroCutscene m_target;
        private SerializedProperty m_useTimelineProp;
        private SerializedProperty m_directorProp;
        private SerializedProperty m_useCameraMoveProp;

        private void OnEnable()
        {
            m_target = (BossIntroCutscene)target;
            m_useTimelineProp = serializedObject.FindProperty("m_useTimeline");
            m_directorProp = serializedObject.FindProperty("m_director");
            m_useCameraMoveProp = serializedObject.FindProperty("m_useCameraMove");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.Space(10f);
            EditorGUIUtility.labelWidth = 200f;

            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_introData"));

            EditorGUILayout.Space(5f);
            EditorGUILayout.LabelField("=== Timeline 설정 ===", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_useTimelineProp);
            if (m_useTimelineProp.boolValue)
            {
                EditorGUILayout.PropertyField(m_directorProp);
            }

            EditorGUILayout.Space(5f);
            EditorGUILayout.LabelField("=== 대상 설정 ===", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_targetTransform"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_targetRenderers"));

            EditorGUILayout.Space(5f);
            EditorGUILayout.LabelField("=== 카메라 설정 ===", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_useCameraMoveProp);
            if (m_useCameraMoveProp.boolValue)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_cameraTargetPosition"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_cameraMoveDuration"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_cameraEase"));

                if (GUILayout.Button("카메라 위치를 보스 위치로 설정"))
                {
                    m_target.SetCameraToBoss();
                }
            }

            EditorGUILayout.Space(5f);
            EditorGUILayout.LabelField("=== 이벤트 설정 ===", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_eventBus"));

            EditorGUILayout.Space(10f);
            DrawPlayButtons();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawPlayButtons()
        {
            EditorGUILayout.Space(5f);
            EditorGUILayout.LabelField("=== 테스트 ===", EditorStyles.boldLabel);

            using (new EditorGUI.DisabledGroupScope(!Application.isPlaying))
            {
                if (m_target.IsPlaying)
                {
                    GUI.backgroundColor = Color.red;
                    if (GUILayout.Button("등장연출 중단"))
                    {
                        m_target.StopAllIntro();
                    }
                    GUI.backgroundColor = Color.white;
                }
                else
                {
                    if (GUILayout.Button("등장연출 재생"))
                    {
                        m_target.PlayIntroAsync(() =>
                        {
                            Debug.Log("[BossIntroCutsceneEditor] 등장연출 완료");
                        }).Forget();
                    }
                }
            }

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("재생 테스트는 플레이 모드에서만 가능합니다.", MessageType.Info);
            }
        }
    }
}
#endif
