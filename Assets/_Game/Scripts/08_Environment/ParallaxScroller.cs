using UnityEngine;

namespace TowerBreakers.Environment
{
    /// <summary>
    /// [설명]: 가로 스크롤에 따른 원경 배경의 깊이감(Parallax)을 연출하는 클래스입니다.
    /// 카메라의 이동량에 비례하여 배경 이미지를 이동시킵니다.
    /// </summary>
    public class ParallaxScroller : MonoBehaviour
    {
        #region 에디터 설정
        [SerializeField, Tooltip("참조할 카메라 (보통 Main Camera)")]
        private Transform m_cameraTransform;

        [SerializeField, Range(0f, 1f), Tooltip("스크롤 제동 계수 (0: 고정, 1: 카메라와 동일 속도)")]
        private float m_parallaxEffect;
        #endregion

        #region 내부 변수
        private Vector3 m_lastCameraPos;
        #endregion

        #region 유니티 생명주기
        private void Start()
        {
            if (m_cameraTransform == null)
            {
                m_cameraTransform = Camera.main.transform;
            }

            m_lastCameraPos = m_cameraTransform.position;
        }

        private void LateUpdate()
        {
            if (m_cameraTransform == null) return;

            Vector3 currentCameraPos = m_cameraTransform.position;
            Vector3 delta = currentCameraPos - m_lastCameraPos;
            
            // 카메라가 이동한 방향의 반대로, 보정 계수만큼 오프셋 적용
            // 수평 전투 이동(delta.x)과 수직 층간 이동(delta.y) 모두 대응
            transform.position += new Vector3(delta.x * m_parallaxEffect, delta.y * m_parallaxEffect, 0f);
            
            m_lastCameraPos = currentCameraPos;
        }
        #endregion
    }
}
