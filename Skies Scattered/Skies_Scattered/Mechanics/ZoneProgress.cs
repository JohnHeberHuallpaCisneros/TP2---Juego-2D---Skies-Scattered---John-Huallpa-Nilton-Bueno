using System.Collections.Generic;
using System.Linq;
using Skies_Scattered.UI;

namespace Skies_Scattered.Mechanics;

public sealed class ZoneProgress
{
    public const int Population = 20;
    public const int GrowthBudget = 20;
    public const int CatharsisLimitPerHero = 5;
    public int Defeated { get; set; }
    public int Spared { get; set; }
    public Dictionary<string, int> APGains { get; set; } = new() { ["Yoko"] = 0, ["Kyoko"] = 0 };
    public Dictionary<string, int> CTGains { get; set; } = new() { ["Yoko"] = 0, ["Kyoko"] = 0 };
    public int RemainingEnemies => Population - Defeated;
    public int GrowthUsed => APGains.Values.Sum() + CTGains.Values.Sum();

    public bool RecordDefeat(CharacterStats hero)
    {
        if (RemainingEnemies <= 0) return false;
        Defeated++;
        hero.EnemiesDefeated++;
        if (GrowthUsed >= GrowthBudget) return false;
        APGains[hero.Name]++;
        hero.AP++;
        return true;
    }

    public bool RecordSpare(CharacterStats hero)
    {
        Spared++;
        hero.EnemiesSpared++;
        if (GrowthUsed >= GrowthBudget || CTGains[hero.Name] >= CatharsisLimitPerHero) return false;
        CTGains[hero.Name]++;
        hero.CT++;
        return true;
    }

    public bool IsValid()
    {
        if (Defeated < 0 || Defeated > Population || Spared < 0 || APGains == null || CTGains == null ||
            APGains.Count != 2 || CTGains.Count != 2) return false;
        foreach (string name in new[] { "Yoko", "Kyoko" })
            if (!APGains.TryGetValue(name, out int ap) || ap < 0 || ap > GrowthBudget ||
                !CTGains.TryGetValue(name, out int ct) || ct < 0 || ct > CatharsisLimitPerHero) return false;
        return GrowthUsed <= GrowthBudget && APGains.Values.Sum() <= Defeated && CTGains.Values.Sum() <= Spared;
    }
}
