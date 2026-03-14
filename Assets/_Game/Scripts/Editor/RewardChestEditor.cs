using UnityEngine;
using UnityEditor;
using TowerBreakers.Interactions.View;
using TowerBreakers.Core.Events;

namespace TowerBreakers.Editor
{
    /// <summary>
    /// [설명]: RewardChestView의 연출을 에디터 상에서 즉시 테스트하기 위한 커스텀 에디터입니다.
    /// </summary>
    [CustomEditor(typeof(RewardChestView))]
    public class RewardChestEditor : UnityEditor.Editor
    {
        #region 내부 필드
        private RewardChestView m_target;
        private string m_testRewardKey = "ShortSword"; // 기본 테스트 키
        #endregion

        #region 에디터 초기화
        private void OnEnable()
        {
            m_target = (RewardChestView)target;
        }
        #endregion

        #region 인스펙터 UI 구현
        public override void OnInspectorGUI()
        {
            // 기본 인스펙터 출력
            base.OnInspectorGUI();

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("--- 테스트 컨트롤 (Runtime & Editor) ---", EditorStyles.boldLabel);

            // 테스트용 키 입력
            m_testRewardKey = EditorGUILayout.TextField("테스트 보상 키 (Name)", m_testRewardKey);

            if (GUILayout.Button("1. 상자 등장 (Activate)", GUILayout.Height(30)))
            {
                m_target.Debug_Activate();
            }

            if (GUILayout.Button("2. 피격 연출 (Hit)", GUILayout.Height(30)))
            {
                m_target.Debug_Hit();
            }

            if (GUILayout.Button("3. 상자 개방 (Open)", GUILayout.Height(30)))
            {
                m_target.Debug_Open();
            }

            EditorGUILayout.Space(5);

            if (GUILayout.Button("4. 아이템 팝업 테스트", GUILayout.Height(30)))
            {
                m_target.Debug_PopItem(m_testRewardKey);
            }

            EditorGUILayout.Space(10);
            EditorGUILayout.HelpBox("참고: 런타임이 아닐 때 ViewModel이 초기화되지 않아 일부 로직이 동작하지 않을 수 있습니다.", MessageType.Info);
        }
        #endregion
    }
}
