using System;

namespace TowerBreakers.UI.Model
{
    public enum SkillState
    {
        Ready,
        OnCooldown
    }

    public class SkillStateModel
    {
        public string SkillName { get; private set; }
        public float CooldownTime { get; private set; }
        public float RemainingCooldown { get; private set; }
        public SkillState State { get; private set; }

        public event Action<SkillStateModel> OnStateChanged;

        public SkillStateModel(string skillName, float cooldownTime)
        {
            SkillName = skillName;
            CooldownTime = cooldownTime;
            RemainingCooldown = 0f;
            State = SkillState.Ready;
        }

        public void StartCooldown()
        {
            if (State == SkillState.Ready)
            {
                RemainingCooldown = CooldownTime;
                State = SkillState.OnCooldown;
                OnStateChanged?.Invoke(this);
            }
        }

        public void UpdateCooldown(float deltaTime)
        {
            if (State == SkillState.OnCooldown && RemainingCooldown > 0)
            {
                RemainingCooldown -= deltaTime;
                if (RemainingCooldown <= 0)
                {
                    RemainingCooldown = 0;
                    State = SkillState.Ready;
                    OnStateChanged?.Invoke(this);
                }
            }
        }

        public float GetCooldownRatio()
        {
            if (CooldownTime <= 0) return 0f;
            return RemainingCooldown / CooldownTime;
        }

        public bool CanUse()
        {
            return State == SkillState.Ready;
        }
    }

    public class BattleUIModel
    {
        public SkillStateModel Dash { get; private set; }
        public SkillStateModel Parry { get; private set; }
        public SkillStateModel Attack { get; private set; }
        public SkillStateModel Skill1 { get; private set; }
        public SkillStateModel Skill2 { get; private set; }
        public SkillStateModel Skill3 { get; private set; }

        public BattleUIModel()
        {
            Dash = new SkillStateModel("Dash", 2f);
            Parry = new SkillStateModel("Parry", 3f);
            Attack = new SkillStateModel("Attack", 0.5f);
            Skill1 = new SkillStateModel("Skill1", 5f);
            Skill2 = new SkillStateModel("Skill2", 8f);
            Skill3 = new SkillStateModel("Skill3", 10f);
        }

        public void Update(float deltaTime)
        {
            Dash.UpdateCooldown(deltaTime);
            Parry.UpdateCooldown(deltaTime);
            Attack.UpdateCooldown(deltaTime);
            Skill1.UpdateCooldown(deltaTime);
            Skill2.UpdateCooldown(deltaTime);
            Skill3.UpdateCooldown(deltaTime);
        }
    }
}
