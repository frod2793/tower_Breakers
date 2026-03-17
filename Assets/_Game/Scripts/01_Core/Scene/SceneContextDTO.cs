using System.Collections.Generic;

namespace TowerBreakers.Core.Scene
{
    /// <summary>
    /// [기능]: 씬 전환 시 전달할 데이터를 담는 DTO
    /// </summary>
    public class SceneContextDTO
    {
        public int CurrentStage
        {
            get;
            set;
        }

        public int DifficultyLevel
        {
            get;
            set;
        }

        public int PlayerGold
        {
            get;
            set;
        }

        public List<string> EarnedItemIds
        {
            get;
            set;
        }

        public bool IsVictory
        {
            get;
            set;
        }

        public int Score
        {
            get;
            set;
        }

        public SceneContextDTO()
        {
            CurrentStage = 1;
            DifficultyLevel = 1;
            PlayerGold = 0;
            EarnedItemIds = new List<string>();
            IsVictory = false;
            Score = 0;
        }

        public static SceneContextDTO CreateDefault()
        {
            return new SceneContextDTO();
        }

        public static SceneContextDTO CreateForBattle(int stage, int difficulty)
        {
            return new SceneContextDTO
            {
                CurrentStage = stage,
                DifficultyLevel = difficulty
            };
        }

        public static SceneContextDTO CreateForResult(int stage, bool isVictory, int score, int gold, List<string> items)
        {
            return new SceneContextDTO
            {
                CurrentStage = stage,
                IsVictory = isVictory,
                Score = score,
                PlayerGold = gold,
                EarnedItemIds = items ?? new List<string>()
            };
        }
    }
}
