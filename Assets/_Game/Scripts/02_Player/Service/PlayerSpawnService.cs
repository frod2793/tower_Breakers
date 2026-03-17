using System;
using UnityEngine;
using DG.Tweening;
using Cysharp.Threading.Tasks;

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

        public event Action OnSpawnComplete;

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

            Vector3 startPos = m_spawnPoint.position;
            startPos.y += m_yOffset;
            m_playerTransform.position = startPos;
            m_playerTransform.gameObject.SetActive(true);

            Vector3 endPos = m_arrivalPoint.position;
            endPos.y += m_yOffset;

            float distance = Vector3.Distance(startPos, endPos);
            float duration = distance / m_dashSpeed * m_dashDuration;

            await m_playerTransform.DOMove(endPos, duration)
                .SetEase(Ease.OutQuad)
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
