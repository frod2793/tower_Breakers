using System.Collections.Generic;
using UnityEngine;
using TowerBreakers.Enemy.Data;
using TowerBreakers.Core.Events;
using TowerBreakers.Enemy.Logic;

namespace TowerBreakers.Enemy.Boss.AI.FSM
{
    /// <summary>
    /// [설명]: 보스 이름별 페이즈 생성을 담당하는 팩토리 구현체입니다.
    /// OCP 원칙을 적용하여 새로운 보스 타입 추가 시 코드 변경 없이 확장 가능합니다.
    /// </summary>
    public sealed class BossPhaseFactory : IBossPhaseFactory
    {
        private readonly Dictionary<string, System.Func<BossFSM, EnemyData, List<IBossPhase>>> m_phaseCreators;

        public BossPhaseFactory()
        {
            m_phaseCreators = new Dictionary<string, System.Func<BossFSM, EnemyData, List<IBossPhase>>>
            {
                { "Goblin", CreateGoblinPhases },
                { "Kraken", CreateKrakenPhases },
                { "Robot", CreateRobotPhases }
            };
        }

        public List<IBossPhase> CreatePhases(EnemyData data, BossFSM controller)
        {
            var phases = new List<IBossPhase>();
            if (data == null) return phases;

            string enemyName = data.EnemyName;

            foreach (var kvp in m_phaseCreators)
            {
                if (enemyName.Contains(kvp.Key))
                {
                    return kvp.Value(controller, data);
                }
            }

            return CreateDefaultPhases(controller, data);
        }

        private List<IBossPhase> CreateGoblinPhases(BossFSM controller, EnemyData data)
        {
            var phases = new List<IBossPhase>
            {
                new GoblinChiefPhase1(controller.ProjectileFactory, controller.EventBus),
                new GoblinChiefPhase2(controller.ProjectileFactory, controller.EventBus)
            };
            return phases;
        }

        private List<IBossPhase> CreateKrakenPhases(BossFSM controller, EnemyData data)
        {
            var krakenState = new KrakenBossState();

            if (controller.EventBus != null)
            {
                controller.EventBus.Subscribe<OnKrakenTentacleDestroyed>(evt =>
                {
                    krakenState?.DecrementTentacleCount(evt.FloorIndex);
                });

                controller.EventBus.Subscribe<OnPlayerFloorChanged>(evt =>
                {
                    krakenState?.SetPlayerFloorIndex(evt.NewFloorIndex);
                });

                controller.EventBus.Subscribe<OnKrakenSummonDamaged>(evt =>
                {
                    controller.TakeDamage((int)evt.Damage);
                });
            }

            var phases = new List<IBossPhase>
            {
                new KrakenPhase1(controller.ProjectileFactory, controller.EventBus, krakenState),
                new KrakenPhase2(controller.ProjectileFactory, controller.EventBus, krakenState)
            };
            return phases;
        }

        private List<IBossPhase> CreateDefaultPhases(BossFSM controller, EnemyData data)
        {
            return new List<IBossPhase>();
        }

        private List<IBossPhase> CreateRobotPhases(BossFSM controller, EnemyData data)
        {
            var phases = new List<IBossPhase>();
            string enemyName = data.EnemyName;

            if (enemyName.Contains("Sword"))
            {
                phases.Add(new RobotSwordPhase1(controller.EventBus));
            }
            else if (enemyName.Contains("Gunner"))
            {
                phases.Add(new RobotGunnerPhase1(controller.ProjectileFactory, controller.EventBus));
            }
            else if (enemyName.Contains("Shield"))
            {
                phases.Add(new RobotShieldPhase1(controller.EventBus));
            }

            return phases;
        }
    }
}
