using Cysharp.Threading.Tasks;
using TowerBreakers.Core.Events;
using UnityEngine;
using TowerBreakers.Player.View;
using TowerBreakers.Player.Data.Models;
using TowerBreakers.Player.Data.SO;
using TowerBreakers.Tower;
using TowerBreakers.Tower.Logic;

namespace TowerBreakers.Player.Logic.Skills
{
    /// <summary>
    /// [설명]: 스킬 3 - 관통 참격(Slash)의 실행 로직을 담당하는 클래스입니다.
    /// </summary>
    public class SlashSkillExecutor : ISkillExecutor
    {
        #region 내부 필드
        private PlayerView m_view;
        private PlayerModel m_model;
        private PlayerData m_data;
        private PlayerProjectileFactory m_factory;
        private Effects.EffectManager m_effectManager;
        private Core.CooldownSystem m_cooldownSystem;
        private Core.Events.IEventBus m_eventBus;
        private const string SKILL_NAME = "Slash";
        #endregion

        #region 프로퍼티
        public bool IsOnCooldown => m_cooldownSystem != null && m_cooldownSystem.IsOnCooldown(SKILL_NAME);
        #endregion

        #region 초기화
        public void Initialize(PlayerView view, PlayerModel model, PlayerData data, Core.Events.IEventBus eventBus, PlayerProjectileFactory factory, Effects.EffectManager effectManager, Core.CooldownSystem cooldownSystem, TowerManager towerManager = null)
        {
            m_view = view;
            m_model = model;
            m_data = data;
            m_eventBus = eventBus;
            m_factory = factory;
            m_effectManager = effectManager;
            m_cooldownSystem = cooldownSystem;
        }
        #endregion

        #region 공개 API
        public async UniTask ExecuteAsync()
        {
            if (IsOnCooldown) return;

            // [추가]: 스킬 사운드 출력
            m_eventBus?.Publish(new OnSoundRequested("Slash"));

            if (m_factory == null)
            {
                UnityEngine.Debug.LogWarning("[SlashSkillExecutor] ProjectileFactory가 설정되지 않았습니다.");
                return;
            }

            var skillData = m_data.SkillData;
            if (skillData == null) return;

            float cooldown = skillData.Skill3Cooldown;
            m_cooldownSystem?.SetCooldown(SKILL_NAME, cooldown);

            int attackPower = m_model.FinalAttackPower(m_data.AttackPower);
            int damage = (int)(attackPower * skillData.Skill3Multiplier);

            if (skillData.Skill3SlashPrefab != null)
            {
                m_factory.SetSlashPrefab(skillData.Skill3SlashPrefab);
            }
            m_factory.Initialize();

            var slash = m_factory.GetSlash();
            if (slash != null)
            {
                // [설명]: 발사체가 플레이어의 이동에 영향을 받지 않도록 부모 관계를 끊어 월드 공간에서 독립적으로 움직이게 합니다.
                slash.transform.SetParent(null);

                // 플레이어 방향 확인
                float facingDir = TowerBreakers.Core.Utilities.DirectionHelper.GetFacingSign(m_view.transform);
                Vector3 spawnPos = m_view.transform.position + Vector3.right * 0.5f * facingDir;
                slash.transform.position = spawnPos;

                slash.InitializeWithSlow(
                    damage, 
                    skillData.Skill3SlashSpeed, 
                    skillData.Skill3SlashLifetime, 
                    m_view.gameObject.layer, 
                    skillData.Skill3SlashDistance, 
                    skillData.Skill3SlowMultiplier, 
                    skillData.Skill3SlowDuration, 
                    m_effectManager,
                    skillData.Skill3KnockbackDistance,
                    skillData.Skill3KnockbackDuration,
                    skillData.Skill3StunDuration
                );
                slash.SetDirection(Vector2.right * facingDir);
                slash.Activate();
            }

            await UniTask.CompletedTask;
        }

        public void OnTick(float deltaTime)
        {
        }
        #endregion
    }
}
