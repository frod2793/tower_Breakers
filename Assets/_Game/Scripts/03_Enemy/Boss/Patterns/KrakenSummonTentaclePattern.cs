using UnityEngine;
using Cysharp.Threading.Tasks;
using TowerBreakers.Enemy.View;
using System.Threading;
using TowerBreakers.Core.Events;

namespace TowerBreakers.Enemy.Logic
{
    /// <summary>
    /// [설명]: 크라켄의 촉수 소환 패턴입니다.
    /// </summary>
    public class KrakenSummonTentaclePattern : IBossPattern
    {
        private readonly IEventBus m_eventBus;
        private readonly KrakenBossState m_krakenState;

        public string PatternName => "Summon Tentacle";

        public KrakenSummonTentaclePattern(IEventBus eventBus, KrakenBossState krakenState)
        {
            m_eventBus = eventBus;
            m_krakenState = krakenState;
        }

        public async UniTask ExecuteAsync(EnemyController controller, CancellationToken ct)
        {
            var view = controller.CachedView;
            if (view == null) return;

            // 이미 촉수가 너무 많으면 소환 건너뜀 (레거시 로직 복원)
            if (m_krakenState != null && m_krakenState.TotalTentacleCount >= 5)
            {
                Debug.Log("[KrakenSummonTentaclePattern] 촉수가 이미 필드에 가득 차 있어 소환을 건너뜁니다.");
                return;
            }

            int floorIndex = Random.Range(0, 3);
            float spawnX = Random.Range(-5f, 5f);
            Vector3 spawnPos = new Vector3(spawnX, 2.3f, 0f);

            view.PlayAnimation(global::PlayerState.ATTACK, 3);

            await UniTask.Delay(500, cancellationToken: ct);

            m_eventBus?.Publish(new OnSoundRequested("Kraken_Summon"));
            m_eventBus?.Publish(new OnKrakenSummonRequested(OnKrakenSummonRequested.SummonType.Tentacle, floorIndex, spawnPos));
            
            // 상태 갱신
            m_krakenState?.IncrementTentacleCount(floorIndex);

            Debug.Log($"[KrakenSummonTentaclePattern] 촉수 소환: 층={floorIndex}, 위치={spawnPos}, 현재 총계={m_krakenState?.TotalTentacleCount}");

            await UniTask.Delay(1000, cancellationToken: ct);
            view.PlayAnimation(global::PlayerState.IDLE);
        }
    }
}