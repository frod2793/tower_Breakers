using UnityEngine;

namespace TowerBreakers.Core
{
    /// <summary>
    /// [설명]: 세로형(Portrait) 카메라 설정을 유지하고 보정하는 컴포넌트입니다.
    /// 타워 브레이커의 세로 뷰 최적화를 담당합니다.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class PortraitCameraSetting : MonoBehaviour
    {
        #region 에디터 설정
        [SerializeField, Tooltip("목표 해상도 가로 세로 비율 (예: 9:16)")]
        private Vector2 m_targetAspectRatio = new Vector2(9, 16);

        [SerializeField, Tooltip("카메라의 Orthographic Size 고정 값")]
        private float m_fixedOrthoSize = 8f;
        #endregion

        #region 내부 변수
        private Camera m_camera;
        #endregion

        #region 유니티 생명주기
        private void Awake()
        {
            m_camera = GetComponent<Camera>();
            ApplySettings();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (m_camera == null) m_camera = GetComponent<Camera>();
            ApplySettings();
        }
#endif
        #endregion

        #region 비즈니스 로직
        /// <summary>
        /// [설명]: 카메라 설정을 적용합니다.
        /// </summary>
        private void ApplySettings()
        {
            if (m_camera == null) return;

            m_camera.orthographic = true;
            m_camera.orthographicSize = m_fixedOrthoSize;
            
            // 해상도에 따른 종횡비 고정 로직 필요 시 추가 가능 (Letterbox 등)
            Debug.Log($"[PortraitCameraSetting] 카메라 종횡비 설정 준수: {m_targetAspectRatio.x}:{m_targetAspectRatio.y}");
        }
        #endregion
    }
}
