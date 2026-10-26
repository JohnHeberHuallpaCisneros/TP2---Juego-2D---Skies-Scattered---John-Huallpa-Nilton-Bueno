using System.Collections.Generic;

namespace Skies_Scattered.UI
{
public class BattleUIState
    {
        public string TopMessage { get; set; } = "Here They Are";
        public int SharedPP { get; set; } = 50;
        public List<CharacterStats> PartyMembers { get; set; } = new List<CharacterStats>();
        public List<string> CurrentMenuOptions { get; set; } = new List<string> { "Spell", "Z-Spell", "Act", "Run", "Defend", "Auto Fight" };
        public int SelectedOptionIndex { get; set; } = 0;
        
        // Nuevo estado para alternar entre la descripción central y el menú superior estilo EarthBound
        public bool IsActionMenuOpen { get; set; } = false; 
        public bool IsSelectingTarget { get; set; } = false;
public int SelectedEnemyIndex { get; set; } = 0;
    }

    public class CharacterStats
    {
        public string Name { get; set; }
        public int HP { get; set; }
        public int AP { get; set; }
        public int CT { get; set; }
    }
}