#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace TowerBreakers.Enemy.Boss.View
{
    /// <summary>
    /// [설명]: BossIntroData 인스펙터 편집용 에디터 스크립트입니다.
    /// </summary>
    [CustomEditor(typeof(BossIntroData))]
    public class BossIntroDataEditor : Editor
    {
        private SerializedProperty m_startScale;
        private SerializedProperty m_endScale;
        private SerializedProperty m_scaleDuration;
        private SerializedProperty m_scaleEase;

        private SerializedProperty m_startPositionOffset;
        private SerializedProperty m_positionDuration;
        private SerializedProperty m_positionEase;

        private SerializedProperty m_useFadeIn;
        private SerializedProperty m_fadeInDuration;

        private SerializedProperty m_useShake;
        private SerializedProperty m_shakeIntensity;
        private SerializedProperty m_shakeDuration;

        private SerializedProperty m_introSoundKey;
        private SerializedProperty m_bgmKey;
        private SerializedProperty m_bgmFadeInDuration;

        private SerializedProperty m_totalDuration;

        private void OnEnable()
        {
            m_startScale = serializedObject.FindProperty("m_startScale");
            m_endScale = serializedObject.FindProperty("m_endScale");
            m_scaleDuration = serializedObject.FindProperty("m_scaleDuration");
            m_scaleEase = serializedObject.FindProperty("m_scaleEase");

            m_startPositionOffset = serializedObject.FindProperty("m_startPositionOffset");
            m_positionDuration = serializedObject.FindProperty("m_positionDuration");
            m_positionEase = serializedObject.FindProperty("m_positionEase");

            m_useFadeIn = serializedObject.FindProperty("m_useFadeIn");
            m_fadeInDuration = serializedObject.FindProperty("m_fadeInDuration");

            m_useShake = serializedObject.FindProperty("m_useShake");
            m_shakeIntensity = serializedObject.FindProperty("m_shakeIntensity");
            m_shakeDuration = serializedObject.FindProperty("m_shakeDuration");

            m_introSoundKey = serializedObject.FindProperty("m_introSoundKey");
            m_bgmKey = serializedObject.FindProperty("m_bgmKey");
            m_bgmFadeInDuration = serializedObject.FindProperty("m_bgmFadeInDuration");

            m_totalDuration = serializedObject.FindProperty("m_totalDuration");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUIUtility.labelWidth = 160f;

            EditorGUILayout.Space(5f);
            EditorGUILayout.LabelField("=== 스케일 애니메이션 ===", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_startScale);
            EditorGUILayout.PropertyField(m_endScale);
            EditorGUILayout.PropertyField(m_scaleDuration);
            EditorGUILayout.PropertyField(m_scaleEase);

            EditorGUILayout.Space(5f);
            EditorGUILayout.LabelField("=== 위치 애니메이션 ===", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_startPositionOffset);
            EditorGUILayout.PropertyField(m_positionDuration);
            EditorGUILayout.PropertyField(m_positionEase);

            EditorGUILayout.Space(5f);
            EditorGUILayout.LabelField("=== 페이드인 ===", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_useFadeIn);
            if (m_useFadeIn.boolValue)
            {
                EditorGUILayout.PropertyField(m_fadeInDuration);
            }

            EditorGUILayout.Space(5f);
            EditorGUILayout.LabelField("=== 쉐이크 효과 ===", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_useShake);
            if (m_useShake.boolValue)
            {
                EditorGUILayout.PropertyField(m_shakeIntensity);
                EditorGUILayout.PropertyField(m_shakeDuration);
            }

            EditorGUILayout.Space(5f);
            EditorGUILayout.LabelField("=== 사운드 ===", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_introSoundKey);

            EditorGUILayout.Space(5f);
            EditorGUILayout.LabelField("=== BGM ===", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_bgmKey);
            if (!string.IsNullOrEmpty(m_bgmKey.stringValue))
            {
                EditorGUILayout.PropertyField(m_bgmFadeInDuration);
            }

            EditorGUILayout.Space(5f);
            EditorGUILayout.LabelField("=== 타이머 ===", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_totalDuration);

            EditorGUILayout.Space(10f);
            DrawDurationInfo();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawDurationInfo()
        {
            float autoDuration = Mathf.Max(m_scaleDuration.floatValue, m_positionDuration.floatValue) + m_fadeInDuration.floatValue;
            float totalDuration = m_totalDuration.floatValue > 0 ? m_totalDuration.floatValue : autoDuration;

            EditorGUILayout.HelpBox(
                $"예상 총 시간: {totalDuration:F2}초\n(자동 계산: {autoDuration:F2}초)",
                MessageType.Info);
        }
    }
}
#endif
