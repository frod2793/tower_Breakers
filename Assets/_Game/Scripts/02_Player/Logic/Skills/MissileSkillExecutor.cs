using UnityEngine;
using TowerBreakers.Player.View;
using TowerBreakers.Player.Data.Models;
using TowerBreakers.Player.Data.SO;
using TowerBreakers.Tower;
using System;
using Cysharp.Threading.Tasks;
using TowerBreakers.Tower.Logic;

namespace TowerBreakers.Player.Logic.Skills
{
    /// <summary>
    /// [설명]: 스킬 2 - 가이드 미사일(Missile)의 실행 로직을 담당하는 클래스입니다.
    /// 발사체 팩토리를 통해 미사일을 생성하고 초기화합니다.
    /// </summary>
    public class MissileSkillExecutor : ISkillExecutor
    {
        #region 내부 필드
        private PlayerView m_view;
        private PlayerModel m_model;
        private PlayerData m_data;
        private PlayerProjectileFactory m_factory;
        private Effects.EffectManager m_effectManager;
        private Core.CooldownSystem m_cooldownSystem;
        private Core.Events.IEventBus m_eventBus;
        private TowerManager m_towerManager;
        private const string SKILL_NAME = "Missile";
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
            m_towerManager = towerManager;
        }
        #endregion

        #region 공개 API
        public async UniTask ExecuteAsync()
        {
            if (IsOnCooldown) return;

            // [추가]: 스킬 사운드 출력 (미사일도 범용 스킬 사운드 사용)
            m_eventBus?.Publish(new Core.Events.OnSoundRequested("Slash"));

            if (m_factory == null)
            {
       
                UnityEngine.Debug.LogWarning("[MissileSkillExecutor] ProjectileFactory가 설정되지 않았습니다.");
                return;
            }

            var skillData = m_data.SkillData;
            if (skillData == null) return;

            float cooldown = skillData.Skill2Cooldown;
            m_cooldownSystem?.SetCooldown(SKILL_NAME, cooldown);

            int attackPower = m_model.FinalAttackPower(m_data.AttackPower);
            int damage = (int)(attackPower * skillData.Skill2Multiplier);

            if (skillData.Skill2MissilePrefab != null)
            {
                m_factory.SetMissilePrefab(skillData.Skill2MissilePrefab);
            }
            m_factory.Initialize();

            int count = skillData.Skill2MissileCount;
            int playerLayer = m_view.gameObject.layer;
            int currentFloor = m_towerManager != null ? m_towerManager.CurrentFloorIndex : 0;
            
            // 플레이어朝向
            float facingDir = TowerBreakers.Core.Utilities.DirectionHelper.GetFacingSign(m_view.transform);

            for (int i = 0; i < count; i++)
            {
                var missile = m_factory.GetMissile();
                if (missile == null) continue;

                // [설명]: 발사체가 플레이어의 이동에 영향을 받지 않도록 부모 관계를 끊어 월드 공간에서 독립적으로 움직이게 합니다.
                missile.transform.SetParent(null);

                // 플레이어 머리 위에서 수직 발사 (X축 분산)
                float xSpread = (i - (count - 1) * 0.5f) * 0.8f * facingDir;
                Vector3 spawnPos = m_view.transform.position + Vector3.up * 1.5f + Vector3.right * xSpread;
                missile.transform.position = spawnPos;

                // 초기 방향을 위쪽으로 설정 (90도)
                missile.transform.rotation = Quaternion.Euler(0f, 0f, 90f);

                missile.InitializeWithWaveAndAfterimage(
                    damage, 
                    skillData.Skill2MissileSpeed, 
                    skillData.Skill2MissileLifetime, 
                    playerLayer, 
                    skillData.Skill2MissileTurnSpeed, 
                    skillData.Skill2MissileWaveAmplitude, 
                    skillData.Skill2MissileWaveFrequency, 
                    skillData.Skill2MissileAfterimageInterval, 
                    m_effectManager,
                    m_eventBus
                );
                
                // 발사 페이즈 설정 (수직 상승 → 추적 모드)
                missile.SetLaunchParameters(skillData.Skill2LaunchHeight, skillData.Skill2LaunchDuration);
                missile.SetFloorIndex(currentFloor);
                missile.Activate();

                // 순차 발사 (0.1초 간격, 마지막 미사일은 대기 불필요)
                if (i < count - 1)
                {
                    await UniTask.Delay(100, cancellationToken: m_view.GetCancellationTokenOnDestroy());
                }
            }

            await UniTask.CompletedTask;
        }

        public void OnTick(float deltaTime)
        {
        }
        #endregion
    }
}
