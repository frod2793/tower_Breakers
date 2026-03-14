// Lightweight context for parry-related state
namespace TowerBreakers.Core
{
    public static class ParryContext
    {
        // Indicates whether the player is currently performing a parry/defense action
        public static bool IsParrying { get; set; } = false;
    }
}
