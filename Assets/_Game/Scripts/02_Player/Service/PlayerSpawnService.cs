using System;
using UnityEngine;
using DG.Tweening;
using Cysharp.Threading.Tasks;
using TowerBreakers.Player.Logic;
using TowerBreakers.Player.DTO;

namespace TowerBreakers.Tower.Service
{
    /// <summary>
    /// [기능]: 플레이어 스폰/등장 연출 서비스
    /// </summary>
    public class PlayerSpawnService : MonoBehaviour
    {
        [Header("스폰 포인트")]
        [Tooltip("플레이어 등장 시작 위치")]
        [SerializeField] private Transform m_spawnPoint;

        [Tooltip("플레이어 도착 위치")]
        [SerializeField] private Transform m_arrivalPoint;

        [Header("대시 연출 설정")]
        [Tooltip("대시 속도")]
        [SerializeField] private float m_dashSpeed = 10f;

        [Tooltip("대시 거리당 시간")]
        [SerializeField] private float m_dashDuration = 0.5f;

        [Tooltip("플레이어 Y 위치 오프셋")]
        [SerializeField] private float m_yOffset = 0f;

        [Header("참조")]
        [Tooltip("플레이어 트랜스폼")]
        [SerializeField] private Transform m_playerTransform;

        [Tooltip("플레이어 설정 DTO")]
        [SerializeField] private PlayerConfigDTO m_playerConfig;

        private PlayerLogic m_playerLogic;

        public event Action OnSpawnComplete;

        #region 초기화
        /// <summary>
        /// [설명]: 플레이어 스폰 서비스를 초기화합니다.
        /// </summary>
        /// <param name="logic">연결된 플레이어 로직</param>
        public void Initialize(PlayerLogic logic)
        {
            if (logic == null) return;
            m_playerLogic = logic;
            
            // [참고]: PlayerPushReceiver의 초기화는 별도로 진행되거나 
            // 뷰 초기화 시점에 logic/config가 주입됨
        }
        #endregion

        [VContainer.Inject]
        public void SetPlayerConfig(PlayerConfigDTO config)
        {
            m_playerConfig = config;
        }

        public void SetSpawnPoint(Transform spawnPoint)
        {
            m_spawnPoint = spawnPoint;
        }

        public void SetArrivalPoint(Transform arrivalPoint)
        {
            m_arrivalPoint = arrivalPoint;
        }

        public void SetPlayerTransform(Transform player)
        {
            m_playerTransform = player;
        }

        public void SetReferences(Transform spawn, Transform arrival, Transform player)
        {
            m_spawnPoint = spawn;
            m_arrivalPoint = arrival;
            m_playerTransform = player;
        }

        public async void PlaySpawnAnimation()
        {
            if (m_playerTransform == null)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    m_playerTransform = player.transform;
                }
            }

            if (m_playerTransform == null || m_spawnPoint == null || m_arrivalPoint == null)
            {
                Debug.LogWarning("[PlayerSpawnService] 스폰 포인트가 설정되지 않았습니다.");
                OnSpawnComplete?.Invoke();
                return;
            }

            // 초기 위치 설정 및 논리 좌표 동기화
            Vector3 startPos = m_spawnPoint.position;
            startPos.y += m_yOffset;
            m_playerTransform.position = startPos;
            
            if (m_playerLogic != null)
            {
                m_playerLogic.SetPosition(new Vector2(startPos.x, startPos.y));
            }

            // 방향 전환 (오른쪽을 바라보도록 설정)
            m_playerTransform.localScale = new Vector3(Mathf.Abs(m_playerTransform.localScale.x), m_playerTransform.localScale.y, m_playerTransform.localScale.z);
            
            m_playerTransform.gameObject.SetActive(true);

            Vector3 endPos = m_arrivalPoint.position;
            endPos.y += m_yOffset;

            float distance = Vector3.Distance(startPos, endPos);
            float duration = distance / m_dashSpeed * m_dashDuration;

            // 이동 애니메이션 수행 및 매 프레임 논리 좌표 업데이트
            await m_playerTransform.DOMove(endPos, duration)
                .SetEase(Ease.OutQuad)
                .OnUpdate(() => 
                {
                    if (m_playerLogic != null)
                    {
                        m_playerLogic.SetPosition(new Vector2(m_playerTransform.position.x, m_playerTransform.position.y));
                    }
                })
                .ToUniTask();

            OnSpawnComplete?.Invoke();
        }

        public async UniTask PlaySpawnAnimationAsync()
        {
            PlaySpawnAnimation();
            await UniTask.Delay(100);
        }
    }
}
