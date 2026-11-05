namespace Skies_Scattered.Mechanics;

// Contadores por personaje, locales a la batalla. Una ronda incluye a todos los enemigos.
public sealed class DefenseStance
{
    public const int BonusCT = 4;
    public int ActiveRounds { get; private set; }
    public int CooldownRounds { get; private set; }
    public int CurrentBonusCT => ActiveRounds > 0 ? BonusCT : 0;
    public bool TryActivate()
    {
        if (CooldownRounds > 0) return false;
        ActiveRounds = 3;
        CooldownRounds = 4;
        return true;
    }
    public void EndEnemyRound()
    {
        if (ActiveRounds > 0) ActiveRounds--;
        if (CooldownRounds > 0) CooldownRounds--;
    }
}
