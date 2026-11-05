
using System.Collections.Generic;

namespace Skies_Scattered.UI
{
    public class BattleUIState
    {
        public bool IsAwaitingIntro { get; set; } = true;
        public bool IsAwaitingBattleEnd { get; set; }

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

        public int DefendCooldown { get; set; }
        public int ImpactedPartyIndex { get; set; } = -1;
        public Microsoft.Xna.Framework.Vector2 PartyImpactOffset { get; set; }

        public int SelectedOptionIndex { get; set; } = 0;

        public int CurrentPartyMemberIndex { get; set; } = 0;

        public bool IsActionMenuOpen { get; set; } = false;

        public bool IsSelectingTarget { get; set; } = false;

        public bool IsItemMenuOpen { get; set; } = false;
        public bool IsActMenuOpen { get; set; }

        public bool IsSpellMenuOpen { get; set; } = false;

        public int SelectedItemIndex { get; set; }

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
        public int EnemiesDefeated { get; set; }
        public int EnemiesSpared { get; set; }

        public int PP { get; set; }

        public int MaxPP { get; set; }

        public List<string> Spells { get; set; } =
            new List<string>();
    }
}

