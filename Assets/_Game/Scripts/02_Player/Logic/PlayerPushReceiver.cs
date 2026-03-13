using UnityEngine;
using TowerBreakers.Player.Data;
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
        private Transform m_backflipStarts;

        [SerializeField, Tooltip("현재 위치 보정(Clamping) 활성화 여부")]
        private bool m_isClampingEnabled = true;
        #endregion

        #region 내부 필드
        private PlayerModel m_model;
        private IEventBus m_eventBus;
        private float m_mapLeftLimit = -100f; // 맵의 물리적 하한선
        #endregion

        #region 초기화
        public void Initialize(PlayerModel model, IEventBus eventBus)
        {
            m_model = model;
            m_eventBus = eventBus;

            // [추가]: 모델의 초기 위치를 현재 씬의 실제 좌표와 동기화 (순간이동 방지)
            if (m_model != null)
            {
                m_model.Position = transform.position;
                // Debug.Log($"[PlayerPushReceiver] 모델 좌표 동기화 완료: {m_model.Position.x:F2}");
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
            // 위치 보정이 비활성화되어 있다면 처리 스킵 (연출 중 등)
            if (!m_isClampingEnabled) return;

            // 매 프레임 카메라와 맵의 경계 중 더 보수적인(오른쪽인) 곳을 실제 벽으로 설정
            if (m_useCameraBounds)
            {
                UpdateLeftWallThresholdFromCamera();
            }

            // 플레이어가 현재 벽 임계값을 벗어났다면 (예: 카메라가 앞서 나가서 플레이어가 강제로 벽에 걸림)
            // 위치를 즉시 보정하여 화면 밖으로 이탈 차단
            if (m_model != null && m_model.Position.x < m_leftWallXThreshold)
            {
                var correctedPos = m_model.Position;
                correctedPos.x = m_leftWallXThreshold;
                m_model.Position = correctedPos;
                
                var worldPos = transform.position;
                worldPos.x = correctedPos.x;
                worldPos.y = correctedPos.y;
                transform.position = worldPos;
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
        public float BackflipThresholdX
        {
            get
            {
                // 트랜스폼이 할당되어 있다면 해당 지점 사용
                if (m_backflipStarts != null)
                {
                    return m_backflipStarts.position.x;
                }
                
                // 할당되지 않았다면 벽으로부터 7.5 유닛 떨어진 지점을 기본값으로 사용
                return m_leftWallXThreshold + 7.5f;
            }
        }
        #endregion

        #region 공개 API
        /// <summary>
        /// [설명]: 맵 구역의 왼쪽 물리 경계를 설정합니다.
        /// </summary>
        /// <param name="limit">X축 하한선</param>
        public void SetMapLimit(float limit)
        {
            m_mapLeftLimit = limit;
            Debug.Log($"[PlayerPushReceiver] 맵 경계 제한 설정: {limit:F2}");
        }

        /// <summary>
        /// [설명]: 왼쪽 벽(경계)의 임계값을 직접 설정합니다. (카메라 미사용 시)
        /// </summary>
        /// <param name="threshold">X축 임계값</param>
        public void SetWallThreshold(float threshold)
        {
            m_leftWallXThreshold = threshold;
            Debug.Log($"[PlayerPushReceiver] 벽 임계값 강제 설정: {threshold:F2}");
        }

        /// <summary>
        /// [설명]: 백플립 연출이 활성화되는 기준 지점(Transform)을 설정합니다.
        /// </summary>
        /// <param name="threshold">기준 위치를 가진 트랜스폼</param>
        public void SetBackflipThreshold(Transform threshold)
        {
            m_backflipStarts = threshold;
            if (m_backflipStarts != null)
            {
                Debug.Log($"[PlayerPushReceiver] 백플립 기점 설정 완료: {m_backflipStarts.position.x:F2}");
            }
        }
        #endregion

        #region 공개 메서드
        /// <summary>
        /// [설명]: 특정 변위만큼 플레이어를 강제로 이동시킵니다. (적 전진과 1:1 위치 동기화용)
        /// </summary>
        /// <param name="deltaX">이동할 X축 변위 (왼쪽은 음수)</param>
        public void SyncPositionDelta(float deltaX)
        {
            if (m_model == null) return;

            Vector2 newPosition = m_model.Position + Vector2.right * deltaX;
            ApplyPositionWithLimit(newPosition, Mathf.Abs(deltaX));
        }

        /// <summary>
        /// [설명]: 특정 힘으로 플레이어를 왼쪽으로 밀어냅니다. (deltaTime이 적용된 힘 기반 방식)
        /// </summary>
        /// <param name="pushForce">밀어내는 힘의 크기</param>
        public void ApplyPushForce(float pushForce)
        {
            if (m_model == null) return;

            float moveAmount = pushForce * Time.deltaTime;
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
            
            Vector2 newPos = m_model.Position + Vector2.left * distance;
            
            // 벽 클램프만 수행 (OnPlayerPushed 발행하지 않음)
            if (newPos.x < m_leftWallXThreshold)
                newPos.x = m_leftWallXThreshold;
                
            m_model.Position = newPos;
            
            var worldPos = transform.position;
            worldPos.x = newPos.x;
            worldPos.y = newPos.y;
            transform.position = worldPos;
            
            Debug.Log($"[PlayerPushReceiver] 투사체 피격 노크백: Distance={distance}");
        }
        #endregion

        #region 내부 메서드
        /// <summary>
        /// [설명]: 메인 카메라의 뷰포트를 기준으로 왼쪽 벽 임계값을 동적으로 계산합니다.
        /// </summary>
        private void UpdateLeftWallThresholdFromCamera()
        {
            Camera mainCam = Camera.main;
            if (mainCam == null) return;

            // 뷰포트 좌표 (0, 0.5)은 화면 왼쪽 중앙의 월드 좌표로 변환
            Vector3 leftEdgeWorld = mainCam.ViewportToWorldPoint(new Vector3(0f, 0.5f, mainCam.nearClipPlane));
            
            float cameraThreshold = leftEdgeWorld.x + m_screenEdgeOffset;
            
            // 카메라 경계와 맵 경계 중 더 오른쪽인(보수적인) 곳을 최종 경계로 채택
            m_leftWallXThreshold = Mathf.Max(cameraThreshold, m_mapLeftLimit);
            
            // Debug.Log($"[PlayerPushReceiver] 벽 임계값 계산: {m_leftWallXThreshold:F2} (Cam: {cameraThreshold:F2}, Map: {m_mapLeftLimit:F2})");
        }

        /// <summary>
        /// [설명]: 한계치를 제한하며 플레이어 위치를 적용하고 필요시 이벤트를 발행합니다.
        /// </summary>
        private void ApplyPositionWithLimit(Vector2 newPosition, float moveDelta)
        {
            // [추가]: 만약 카메라가 이동하는 환경이라면 매 프레임 또는 필요 시 갱신 가능
            // 타워 브레이커는 수직 이동이 메인이므로 가로 범위는 보통 고정되지만, 안전을 위해 체크 가능
            // if (m_useCameraBounds) UpdateLeftWallThresholdFromCamera();

            // 왼쪽 벽 충돌 판정 (IsAtWall 판정과 동일한 0.01f 여유분 적용하여 데드 존 제거)
            bool isHittingWall = newPosition.x <= m_leftWallXThreshold + 0.01f;
            if (isHittingWall)
            {
                if (newPosition.x < m_leftWallXThreshold)
                    newPosition.x = m_leftWallXThreshold;
                // 벽에 닿은 상태에서 계속 밀리면 데미지/충격 이벤트 발생 (필요 시 주석 해제)
                // Debug.Log($"[PlayerPushReceiver] 벽 충돌 감지! OnPlayerPushed 발행 (Delta: {moveDelta:F4})");
                m_eventBus?.Publish(new OnPlayerPushed(moveDelta));
            }

            // Debug.Log($"[PlayerPushReceiver] 위치 적용: {newPosition.x:F2} (Threshold: {m_leftWallXThreshold:F2}, 벽충돌: {isHittingWall})");

            m_model.Position = newPosition;
            
            var worldPos = transform.position;
            worldPos.x = newPosition.x;
            worldPos.y = newPosition.y;
            transform.position = worldPos;
        }
        #endregion
    }
}
