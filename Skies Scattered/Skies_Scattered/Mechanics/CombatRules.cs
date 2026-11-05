using System;
using Skies_Scattered.Entities;
using Skies_Scattered.UI;

namespace Skies_Scattered.Mechanics;

public static class CombatRules
{
    public static int BasicPower(CharacterStats hero) => Math.Max(1, hero.AP);
    public static int SpellPower(CharacterStats hero, int basePower) => basePower + Math.Max(0, hero.AP) / 2;
    public static int DamageToEnemy(int power, Enemy enemy, bool defenseDown)
    {
        int damage = Math.Max(1, power - Math.Max(0, enemy.Defense));
        return defenseDown ? Math.Max(1, (int)Math.Ceiling(damage * 1.5)) : damage;
    }
    public static int DamageToHero(int power, CharacterStats hero, bool defending)
    {
        return DamageToHero(power, hero, defending, 0);
    }
    public static int DamageToHero(int power, CharacterStats hero, bool defending, int bonusCT)
    {
        int damage = Math.Max(1, power - (Math.Max(0, hero.CT) + Math.Max(0, bonusCT)) / 2);
        return defending ? Math.Max(1, damage / 2) : damage;
    }
    public static bool CanSpare(Enemy enemy) => enemy.HP > 0 && (long)enemy.HP * 4 <= enemy.MaxHP;
}
