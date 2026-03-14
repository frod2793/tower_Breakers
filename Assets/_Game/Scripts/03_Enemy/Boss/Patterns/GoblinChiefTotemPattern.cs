using UnityEngine;
using Cysharp.Threading.Tasks;
using TowerBreakers.Enemy.View;
using System.Threading;
using TowerBreakers.Core.Events;

namespace TowerBreakers.Enemy.Logic
{
    /// <summary>
    /// [설명]: 고블린족장의 토템 소환 패턴입니다.
    /// 세 가지 타입의 토템 (Bomb, Lightning, Buff) 중 하나를 무작위로 소환합니다.
    /// </summary>
    public class GoblinChiefTotemPattern : IBossPattern
    {
        private readonly ProjectileFactory m_projectileFactory;
        private readonly IEventBus m_eventBus;

        private readonly IBossPattern[] m_totemPatterns;

        public string PatternName => "Summon Totem";

        public GoblinChiefTotemPattern(ProjectileFactory factory, IEventBus eventBus)
        {
            m_projectileFactory = factory;
            m_eventBus = eventBus;

            m_totemPatterns = new IBossPattern[]
            {
                new GoblinChiefBombPattern(eventBus),
                new GoblinChiefLightningPattern(eventBus),
                new GoblinChiefBuffPattern(eventBus)
            };
        }

        public async UniTask ExecuteAsync(EnemyController controller, CancellationToken ct)
        {
            int totemType = Random.Range(0, m_totemPatterns.Length);
            var selectedPattern = m_totemPatterns[totemType];

            Debug.Log($"[TotemPattern] 토템 선택: {selectedPattern.PatternName}");

            await selectedPattern.ExecuteAsync(controller, ct);
        }
    }
}
