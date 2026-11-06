using System.Collections.Generic;
using Skies_Scattered.UI;

namespace Skies_Scattered.Mechanics;
public sealed class ZoneProgress
{
    public const int Population = 20;
    public const int GrowthBudget = 20;
    public const int CatharsisLimitPerHero = 5;
    public int Defeated { get; set; }
    public Dictionary<string, int> APGains { get; set; } = new()
    {
        ["Yoko"] = 0,
        ["Kyoko"] = 0
    };
    public Dictionary<string, int> CTGains { get; set; } = new()
    {
        ["Yoko"] = 0,
        ["Kyoko"] = 0
    };
    public int RemainingEnemies => Population - Defeated;
    public int GrowthUsed
    {
        get
        {
            int total = 0;
            foreach (int gain in APGains.Values) total += gain;
            foreach (int gain in CTGains.Values) total += gain;
            return total;
        }
    }

    public void RecordDefeat(CharacterStats hero)
    {
        if (RemainingEnemies <= 0)
            return;
        Defeated++;
        if (GrowthUsed >= GrowthBudget)
            return;
        APGains[hero.Name]++;
        hero.AP++;
    }

    public void RecordSpare(CharacterStats hero)
    {
        if (GrowthUsed >= GrowthBudget || CTGains[hero.Name] >= CatharsisLimitPerHero)
            return;
        CTGains[hero.Name]++;
        hero.CT++;
    }
}
