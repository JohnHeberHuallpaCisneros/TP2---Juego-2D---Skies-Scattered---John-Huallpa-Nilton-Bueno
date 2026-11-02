namespace Skies_Scattered.UI;

public static class SpellNames
{
    public static string Display(string id) => id switch
    {
        "FireBeta" => "Fire Beta",
        "FireGamma" => "Fire Gamma",
        "FreezeAlpha" => "Freeze Alpha",
        "FreezeOmega" => "Freeze Omega",
        "DefenseDownOmega" => "Defense Down Omega",
        "ShieldBeta" => "Shield Beta",
        "EnemyBeam" => "Enemy Beam",
        _ => id
    };
}
