namespace Skies_Scattered.Mechanics;

// La lógica de combate y la interfaz consultan la misma regla de coste.
public static class SpellRules
{
    public static int GetCost(string spell)
    {
        switch (spell)
        {
            case "FireBeta":
            case "FreezeAlpha":
                return 5;
            case "FireGamma":
            case "FreezeOmega":
                return 10;
            case "DefenseDownOmega":
            case "ShieldBeta":
                return 8;
            default:
                return 0;
        }
    }
}
