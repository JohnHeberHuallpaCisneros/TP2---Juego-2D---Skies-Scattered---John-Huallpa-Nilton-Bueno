using System.Collections.Generic;

namespace Skies_Scattered.UI
{
    public class BattleUIState
    {
        public string TopMessage { get; set; } = "¡Un enemigo salvaje apareció!";
        public List<string> CurrentMenuOptions { get; set; } = new List<string> { "Spell", "Z-Spell", "Items", "Act", "Auto", "Run" };
        public int SelectedOptionIndex { get; set; } = 0;
        public int SharedPP { get; set; } = 50;
        public List<CharacterStats> PartyMembers { get; set; } = new List<CharacterStats>();
    }

    public class CharacterStats
    {
        public string Name { get; set; }
        public int HP { get; set; }
        public int AP { get; set; }
        public int CT { get; set; }
    }
}