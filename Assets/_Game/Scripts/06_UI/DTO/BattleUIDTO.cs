using System;

namespace TowerBreakers.UI.DTO
{
    #region 데이터 모델 (DTO)
    /// <summary>
    /// [설명]: 개별 스킬의 초기 설정 및 상태를 담는 DTO 클래스입니다.
    /// </summary>
    [Serializable]
    public class SkillDTO
    {
        public string Name;
        public float CooldownTime;
    }

    /// <summary>
    /// [설명]: 전체 전투 UI의 기본 설정을 담는 DTO 클래스입니다.
    /// </summary>
    [Serializable]
    public class BattleUIDTO
    {
        public SkillDTO DashSkill = new SkillDTO { Name = "Dash", CooldownTime = 2f };
        public SkillDTO ParrySkill = new SkillDTO { Name = "Parry", CooldownTime = 3f };
        public SkillDTO AttackSkill = new SkillDTO { Name = "Attack", CooldownTime = 0.5f };
        public SkillDTO Skill1 = new SkillDTO { Name = "Skill1", CooldownTime = 5f };
        public SkillDTO Skill2 = new SkillDTO { Name = "Skill2", CooldownTime = 10f };
        public SkillDTO Skill3 = new SkillDTO { Name = "Skill3", CooldownTime = 15f };
    }
    #endregion
}
