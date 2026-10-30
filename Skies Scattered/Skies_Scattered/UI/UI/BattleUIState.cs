
using System.Collections.Generic;

namespace Skies_Scattered.UI
{
    public class BattleUIState
    {
        public bool IsAwaitingIntro { get; set; } = true;

        public TypewriterText Message { get; } = new TypewriterText { Text = "Here They Are" };
        public string TopMessage { get => Message.Text; set => Message.Text = value; }

        public List<CharacterStats> PartyMembers { get; set; } =
            new List<CharacterStats>();

        public List<string> CurrentMenuOptions { get; set; } =
            new List<string>
            {
                "SPELL",
                "Z-SPELL",
                "ACT",
                "DEFEND",
                "ITEMS",
                "RUN"
            };

        public int SelectedOptionIndex { get; set; } = 0;

        public int CurrentPartyMemberIndex { get; set; } = 0;

        public bool IsActionMenuOpen { get; set; } = false;

        public bool IsSelectingTarget { get; set; } = false;

        public bool IsItemMenuOpen { get; set; } = false;

        public bool IsSpellMenuOpen { get; set; } = false;

        public int SelectedSpellIndex { get; set; } = 0;

        public int SelectedEnemyIndex { get; set; } = 0;
    }

    public class CharacterStats
    {
        public string Name { get; set; }

        public int HP { get; set; }

        public int MaxHP { get; set; } = 50;

        public List<string> Items { get; set; } = new List<string>();

        public int AP { get; set; }

        public int CT { get; set; }

        public int PP { get; set; }

        public int MaxPP { get; set; }

        public List<string> Spells { get; set; } =
            new List<string>();
    }
}

