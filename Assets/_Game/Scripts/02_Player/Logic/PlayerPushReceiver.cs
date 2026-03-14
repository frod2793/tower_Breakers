using UnityEngine;
using TowerBreakers.Player.Data.Models;
using TowerBreakers.Core.Events;
using System;

namespace TowerBreakers.Player.Logic
{
    /// <summary>
    /// [설명]: 적의 밀기 힘을 수신하여 플레이어의 위치를 조정하고 벽 충돌을 판정하는 클래스입니다.
    /// </summary>
    public class PlayerPushReceiver : MonoBehaviour
    {
        #region 에디터 설정
        [Header("경계 설정")]
        [SerializeField, Tooltip("카메라 범위를 기준으로 왼쪽 벽을 자동 설정할지 여부")]
        private bool m_useCameraBounds = true;

        [SerializeField, Tooltip("왼쪽 벽(경계)의 X 좌표 (m_useCameraBounds가 false일 때만 사용)")]
        private float m_leftWallXThreshold = -2.5f;

        [SerializeField, Tooltip("화면 끝에서의 여유 거리")]
        private float m_screenEdgeOffset = 0.5f;

        [SerializeField, Tooltip("이 지점(X)을 넘어서 적진에 있을 때 패링하면 백플립 활성화")]
        private float m_backflipThresholdX = 4.0f;

        [SerializeField, Tooltip("현재 위치 보정(Clamping) 활성화 여부")]
        private bool m_isClampingEnabled = true;
        #endregion

        #region 내부 필드
        private PlayerModel m_model;
        private IEventBus m_eventBus;
        private Camera m_mainCamera;
        private Transform m_cachedTransform;

        // [최적화]: 카메라 위치 캐싱 (변경 시에만 재계산)
        private Vector3 m_cachedCameraPosition;
        private bool m_isCameraThresholdValid;

        private float m_mapLeftLimit = -100f; // 맵의 물리적 하한선
        #endregion

        #region 초기화
        /// <summary>
        /// [설명]: 플레이어 모델과 이벤트 버스를 주입받아 초기화합니다.
        /// </summary>
        /// <param name="model">플레이어 데이터 모델</param>
        /// <param name="eventBus">이벤트 시스템 버스</param>
        public void Initialize(PlayerModel model, IEventBus eventBus)
        {
            m_model = model;
            m_eventBus = eventBus;
            
            // [최적화]: 자주 접근하는 참조 캐싱
            m_cachedTransform = transform;
            m_mainCamera = Camera.main;

            if (m_model != null)
            {
                m_model.Position = m_cachedTransform.position;
            }

            if (m_useCameraBounds)
            {
                UpdateLeftWallThresholdFromCamera();
            }
        }
        #endregion

        #region 유니티 생명주기
        private void LateUpdate()
        {
            if (!m_isClampingEnabled) return;

            if (m_useCameraBounds)
            {
                if (m_mainCamera == null)
                {
                    m_mainCamera = Camera.main;
                }

                // [최적화]: 카메라 위치가 변경되었을 때만 임계값 재계산
                if (m_mainCamera != null)
                {
                    Vector3 currentCamPos = m_mainCamera.transform.position;
                    if (!m_isCameraThresholdValid || Vector3.Distance(m_cachedCameraPosition, currentCamPos) > 0.01f)
                    {
                        m_cachedCameraPosition = currentCamPos;
                        m_isCameraThresholdValid = true;
                        UpdateLeftWallThresholdFromCamera();
                    }
                }
            }

            // 플레이어가 현재 벽 임계값을 벗어났다면 위치 보정
            if (m_model != null && m_model.Position.x < m_leftWallXThreshold)
            {
                var correctedPos = m_model.Position;
                correctedPos.x = m_leftWallXThreshold;
                m_model.Position = correctedPos;
                
                var worldPos = m_cachedTransform.position;
                worldPos.x = correctedPos.x;
                worldPos.y = correctedPos.y;
                m_cachedTransform.position = worldPos;
            }
        }
        #endregion

        #region 프로퍼티
        /// <summary>
        /// [설명]: 위치 보정(Clamping) 활성 여부를 제어합니다. 연출 시 false로 설정하여 경계 밖 이동을 허용합니다.
        /// </summary>
        public bool IsClampingEnabled
        {
            get => m_isClampingEnabled;
            set => m_isClampingEnabled = value;
        }

        /// <summary>
        /// [설명]: 플레이어가 현재 왼쪽 벽(경계)에 닿아 있는지 여부를 반환합니다.
        /// </summary>
        public bool IsAtWall => m_model != null && m_model.Position.x <= m_leftWallXThreshold + 0.01f;

        /// <summary>
        /// [설명]: 현재 유효한 왼쪽 벽(경계)의 X 좌표를 반환합니다.
        /// </summary>
        public float LeftWallThreshold => m_leftWallXThreshold;

        /// <summary>
        /// [설명]: 백플립이 활성화되는 X 좌표 기점을 반환합니다.
        /// </summary>
        public float BackflipThresholdX => m_backflipThresholdX;
        #endregion

        #region 공개 API
        /// <summary>
        /// [설명]: 맵 구역의 왼쪽 물리 경계를 설정합니다.
        /// </summary>
        /// <param name="limit">X축 하한선</param>
        public void SetMapLimit(float limit)
        {
            m_mapLeftLimit = limit;
        }

        /// <summary>
        /// [설명]: 왼쪽 벽(경계)의 임계값을 직접 설정합니다. (카메라 미사용 시)
        /// </summary>
        /// <param name="threshold">X축 임계값</param>
        public void SetWallThreshold(float threshold)
        {
            m_leftWallXThreshold = threshold;
        }

        /// <summary>
        /// [설명]: 백플립 연출이 활성화되는 기준 지점(X 월드 좌표)을 설정합니다.
        /// </summary>
        /// <param name="thresholdX">기준 X 좌표</param>
        public void SetBackflipThreshold(float thresholdX)
        {
            m_backflipThresholdX = thresholdX;
        }

        #region 에디터 지원
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;
            float x = m_backflipThresholdX;
            Gizmos.DrawLine(new Vector3(x, -5f, 0f), new Vector3(x, 5f, 0f));
            
            Gizmos.color = Color.red;
            float wallX = m_leftWallXThreshold;
            Gizmos.DrawLine(new Vector3(wallX, -5f, 0f), new Vector3(wallX, 5f, 0f));
        }
        #endregion
        #endregion

        #region 공개 메서드
        /// <summary>
        /// [설명]: 특정 변위만큼 플레이어를 강제로 이동시킵니다. (적 전진과 1:1 위치 동기화용)
        /// </summary>
        /// <param name="deltaX">이동할 X축 변위 (왼쪽은 음수)</param>
        public void SyncPositionDelta(float deltaX)
        {
            if (m_model == null) return;

            float resistance = m_model != null ? m_model.FinalPushResistance : 0f;
            float adjustedDelta = deltaX * (1.0f - resistance);

            Vector2 newPosition = m_model.Position + Vector2.right * adjustedDelta;
            ApplyPositionWithLimit(newPosition, Mathf.Abs(adjustedDelta));
        }

        /// <summary>
        /// [설명]: 특정 힘으로 플레이어를 왼쪽으로 밀어냅니다. (deltaTime이 적용된 힘 기반 방식)
        /// </summary>
        /// <param name="pushForce">밀어내는 힘의 크기</param>
        public void ApplyPushForce(float pushForce)
        {
            if (m_model == null) return;

            float resistance = m_model != null ? m_model.FinalPushResistance : 0f;
            float moveAmount = (pushForce * Time.deltaTime) * (1.0f - resistance);
            
            Vector2 newPosition = m_model.Position + Vector2.left * moveAmount;
            ApplyPositionWithLimit(newPosition, moveAmount);
        }

        /// <summary>
        /// [설명]: 투사체에 의한 즉시 및 고정 거리 밀기 연출입니다. 벽 충돌 시 데미지 이벤트는 발생하지 않습니다.
        /// </summary>
        /// <param name="distance">밀려날 거리</param>
        public void ApplyProjectileKnockback(float distance)
        {
            if (m_model == null) return;
            
            float resistance = m_model != null ? m_model.FinalPushResistance : 0f;
            float adjustedDistance = distance * (1.0f - resistance);
            
            Vector2 newPos = m_model.Position + Vector2.left * adjustedDistance;
            
            if (newPos.x < m_leftWallXThreshold)
                newPos.x = m_leftWallXThreshold;
                
            m_model.Position = newPos;
            
            var worldPos = m_cachedTransform.position;
            worldPos.x = newPos.x;
            worldPos.y = newPos.y;
            m_cachedTransform.position = worldPos;
        }
        #endregion

        #region 내부 메서드
        /// <summary>
        /// [설명]: 메인 카메라의 뷰포트를 기준으로 왼쪽 벽 임계값을 동적으로 계산합니다.
        /// </summary>
        private void UpdateLeftWallThresholdFromCamera()
        {
            if (m_mainCamera == null)
            {
                m_mainCamera = Camera.main;
                if (m_mainCamera == null) return;
            }

            Vector3 leftEdgeWorld = m_mainCamera.ViewportToWorldPoint(new Vector3(0f, 0.5f, m_mainCamera.nearClipPlane));
            float cameraThreshold = leftEdgeWorld.x + m_screenEdgeOffset;
            
            m_leftWallXThreshold = Mathf.Max(cameraThreshold, m_mapLeftLimit);
        }

        /// <summary>
        /// [설명]: 한계치를 제한하며 플레이어 위치를 적용하고 필요시 이벤트를 발행합니다.
        /// </summary>
        private void ApplyPositionWithLimit(Vector2 newPosition, float moveDelta)
        {
            // Clamping이 비활성화된 상태라면 이벤트 발행 및 위치 누적을 수행하지 않음
            if (!m_isClampingEnabled)
            {
                m_model.Position = newPosition;
                
                var tp = m_cachedTransform.position;
                tp.x = newPosition.x;
                tp.y = newPosition.y;
                m_cachedTransform.position = tp;
                return;
            }

            bool isHittingWall = newPosition.x <= m_leftWallXThreshold + 0.01f;
            if (isHittingWall && moveDelta > 0.001f)
            {
                if (newPosition.x < m_leftWallXThreshold)
                    newPosition.x = m_leftWallXThreshold;
                
                m_eventBus?.Publish(new OnPlayerPushed(moveDelta));
            }

            m_model.Position = newPosition;
            
            var worldPos = m_cachedTransform.position;
            worldPos.x = newPosition.x;
            worldPos.y = newPosition.y;
            m_cachedTransform.position = worldPos;
        }
        #endregion
    }
}
