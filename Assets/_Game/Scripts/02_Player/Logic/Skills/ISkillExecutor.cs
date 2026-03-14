using Cysharp.Threading.Tasks;
using UnityEngine;
using TowerBreakers.Player.View;
using TowerBreakers.Player.Data.Models;
using TowerBreakers.Player.Data.SO;
using TowerBreakers.Core.Events;
using TowerBreakers.Effects;
using TowerBreakers.Tower;
using TowerBreakers.Tower.Logic;

namespace TowerBreakers.Player.Logic.Skills
{
    /// <summary>
    /// [설명]: 플레이어 스킬의 실행 로직을 정의하는 인터페이스입니다.
    /// 전략 패턴(Strategy Pattern)을 사용하여 각 스킬의 복잡한 로직을 독립적으로 관리합니다.
    /// </summary>
    public interface ISkillExecutor
    {
        /// <summary>
        /// [설명]: 현재 스킬이 쿨다운 중인지 여부입니다.
        /// </summary>
        bool IsOnCooldown { get; }

        /// <summary>
        /// [설명]: 필요한 의존성을 주입받아 초기화합니다.
        /// </summary>
        void Initialize(PlayerView view, PlayerModel model, PlayerData data, IEventBus eventBus, PlayerProjectileFactory factory, EffectManager effectManager, Core.CooldownSystem cooldownSystem, TowerManager towerManager = null);

        /// <summary>
        /// [설명]: 스킬을 비동기로 실행합니다.
        /// </summary>
        UniTask ExecuteAsync();

        /// <summary>
        /// [설명]: 매 프레임 업데이트 로직을 수행합니다.
        /// </summary>
        void OnTick(float deltaTime);
    }
}
