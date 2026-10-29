
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameLibrary;
using Skies_Scattered.Entities;
using Skies_Scattered.UI;

namespace Skies_Scattered.States
{
    public class BattleState : IGameState
    {
        private BattleUI _battleUI;
        private BattleUIState _battleState;
        private List<Enemy> _currentBattleEnemies;
        private Random _random;

        public event Action OnBattleEnded;

        private enum BattlePhase
        {
            EnemyIntro,
            PlayerTurn,
            SpellSelection,
            ItemsSelection,
            SelectingTarget,
            PlayerActionResult,
            EnemyTurn,
            BattleEnded
        }

        private enum PendingAction
        {
            BasicAttack,
            FireBeta,
            FireGamma,
            FreezeAlpha,
            FreezeOmega,
            DefenseDownOmega
        }

        private BattlePhase _currentPhase = BattlePhase.EnemyIntro;
        private PendingAction _pendingAction = PendingAction.BasicAttack;

        private float _timer = 0f;
        private const float MessageDelay = 1.2f;
        private const float EnemyTurnDelay = 1.2f;

        private int _currentPartyMemberIndex = 0;
        private int _enemyIndex = 0;

        private readonly HashSet<CharacterStats> _defending =
            new HashSet<CharacterStats>();

        private readonly HashSet<Enemy> _defenseDownEnemies =
            new HashSet<Enemy>();

        // Costes provisionales: se pueden cambiar aquí.
        private const int FireBetaCost = 5;
        private const int FireGammaCost = 10;
        private const int FreezeAlphaCost = 5;
        private const int FreezeOmegaCost = 10;
        private const int DefenseDownOmegaCost = 8;
        private const int ShieldBetaCost = 8;

        public class EnemyTemplate
        {
            public string Name { get; set; }
            public Texture2D Texture { get; set; }
            public int HP { get; set; }
        }

        public BattleState(
            SpriteFont font,
            Texture2D uiBorders,
            Texture2D earthboundHud,
            List<EnemyTemplate> enemyTemplates,
            List<CharacterStats> partyMembers)
        {
            _battleUI = new BattleUI(font, uiBorders, earthboundHud);
            _random = new Random();
            _currentBattleEnemies = new List<Enemy>();

            if (enemyTemplates == null || enemyTemplates.Count == 0)
            {
                throw new ArgumentException(
                    "Debe existir al menos una plantilla de enemigo.",
                    nameof(enemyTemplates));
            }

            int cantidad = _random.Next(1, 4);
            float startX = 200f;

            for (int i = 0; i < cantidad; i++)
            {
                int index = _random.Next(enemyTemplates.Count);
                var template = enemyTemplates[index];

                Enemy enemy = new Enemy
                {
                    Name = template.Name,
                    Texture = template.Texture,
                    HP = template.HP,
                    MaxHP = template.HP,
                    Position = new Vector2(
                        startX + (i * 120f), 180f)
                };

                _currentBattleEnemies.Add(enemy);
            }

            _battleState = new BattleUIState
            {
                TopMessage = BuildEnemyIntroMessage(),

                PartyMembers = partyMembers ?? throw new ArgumentNullException(nameof(partyMembers)),

                CurrentMenuOptions = new List<string>
                {
                    "SPELL",
                    "Z-SPELL",
                    "ACT",
                    "DEFEND",
                    "ITEMS",
                    "RUN"
                },

                IsActionMenuOpen = false,
                IsSelectingTarget = false,
                IsSpellMenuOpen = false
            };
        }

        // Se crea una vez por partida; cada batalla comparte estas estadísticas.
        public static List<CharacterStats> CreateParty()
        {
            return new List<CharacterStats>
                {
                    new CharacterStats
                    {
                        Name = "Yoko",
                        AP = 10,
                        HP = 50,
                        MaxHP = 50,
                        CT = 5,
                        PP = 30,
                        MaxPP = 30,
                        Spells = new List<string>
                        {
                            "FireBeta",
                            "FireGamma",
                            "DefenseDownOmega"
                        }
                    },

                    new CharacterStats
                    {
                        Name = "Kyoko",
                        AP = 12,
                        HP = 50,
                        MaxHP = 50,
                        CT = 4,
                        PP = 30,
                        MaxPP = 30,
                        Spells = new List<string>
                        {
                            "FreezeAlpha",
                            "FreezeOmega",
                            "ShieldBeta"
                        }
                    }
                };
        }

        private string BuildEnemyIntroMessage()
        {
            if (_currentBattleEnemies.Count == 1)
                return $"¡Apareció {_currentBattleEnemies[0].Name}!";

            var names = _currentBattleEnemies.ConvertAll(enemy => enemy.Name);
            return "¡Aparecieron " + string.Join(", ", names.GetRange(0, names.Count - 1)) +
                   " y " + names[names.Count - 1] + "!";
        }

        public void Initialize() { }

        public void LoadContent() { }

        public void Update(GameTime gameTime)
        {
            _battleUI.Update(gameTime);

            float dt =
                (float)gameTime.ElapsedGameTime.TotalSeconds;

            switch (_currentPhase)
            {
                case BattlePhase.EnemyIntro:
                    if (Input.IsKeyPressed(Keys.Z))
                    {
                        _timer = 0f;
                        OpenPlayerTurn();
                    }
                    break;

                case BattlePhase.PlayerTurn:
                    UpdatePlayerTurn();
                    break;

                case BattlePhase.SpellSelection:
                    UpdateSpellSelection();
                    break;

                case BattlePhase.ItemsSelection:
                    if (Input.IsKeyPressed(Keys.X) || Input.IsKeyPressed(Keys.Back) ||
                        Input.IsKeyPressed(Keys.Enter) || Input.IsKeyPressed(Keys.Z) ||
                        Input.IsKeyPressed(Keys.Space))
                    {
                        _battleState.IsItemMenuOpen = false;
                        ReturnToActionMenu();
                    }
                    break;

                case BattlePhase.SelectingTarget:
                    UpdateTargetSelection();
                    break;

                case BattlePhase.PlayerActionResult:
                    _timer += dt;

                    if (_timer >= MessageDelay)
                    {
                        _timer = 0f;
                        AdvanceToNextHero();
                    }
                    break;

                case BattlePhase.EnemyTurn:
                    UpdateEnemyTurn(dt);
                    break;

                case BattlePhase.BattleEnded:
                    _timer += dt;
                    if (_timer >= MessageDelay)
                    {
                        var ended = OnBattleEnded;
                        OnBattleEnded = null;
                        _battleUI.Dispose();
                        ended?.Invoke();
                    }
                    break;
            }
        }

        private CharacterStats CurrentHero
        {
            get
            {
                return _battleState.PartyMembers[
                    _currentPartyMemberIndex];
            }
        }

        private void OpenPlayerTurn()
        {
            _battleState.IsAwaitingIntro = false;
            int firstAlive = _battleState.PartyMembers.FindIndex(
                p => p.HP > 0);

            if (firstAlive < 0)
            {
                EndBattle("Tu grupo ha caído...");
                return;
            }

            _currentPartyMemberIndex = firstAlive;
            _battleState.CurrentPartyMemberIndex = firstAlive;
            _battleState.SelectedOptionIndex = 0;
            _battleState.SelectedSpellIndex = 0;

            _battleState.IsSelectingTarget = false;
            _battleState.IsSpellMenuOpen = false;
            _battleState.IsActionMenuOpen = true;

            _battleState.TopMessage =
                $"¿Qué hará {CurrentHero.Name}?";

            _currentPhase = BattlePhase.PlayerTurn;
        }

        private void UpdatePlayerTurn()
        {
            if (!_battleState.IsActionMenuOpen)
                return;

            int count = _battleState.CurrentMenuOptions.Count;

            if (count == 0)
                return;

            if (Input.IsKeyPressed(Keys.Left) ||
                Input.IsKeyPressed(Keys.A))
            {
                _battleState.SelectedOptionIndex =
                    Math.Max(
                        0,
                        _battleState.SelectedOptionIndex - 1);
            }

            if (Input.IsKeyPressed(Keys.Right) ||
                Input.IsKeyPressed(Keys.D))
            {
                _battleState.SelectedOptionIndex =
                    Math.Min(
                        count - 1,
                        _battleState.SelectedOptionIndex + 1);
            }

            if (Input.IsKeyPressed(Keys.Up) ||
                Input.IsKeyPressed(Keys.W))
            {
                _battleState.SelectedOptionIndex =
                    Math.Max(
                        0,
                        _battleState.SelectedOptionIndex - 2);
            }

            if (Input.IsKeyPressed(Keys.Down) ||
                Input.IsKeyPressed(Keys.S))
            {
                _battleState.SelectedOptionIndex =
                    Math.Min(
                        count - 1,
                        _battleState.SelectedOptionIndex + 2);
            }

            if (Input.IsKeyPressed(Keys.Back))
            {
                EndBattle("La batalla terminó.");
                return;
            }

            if (Input.IsKeyPressed(Keys.Z) ||
                Input.IsKeyPressed(Keys.Enter) ||
                Input.IsKeyPressed(Keys.Space))
            {
                int selected = Math.Clamp(
                    _battleState.SelectedOptionIndex,
                    0,
                    count - 1);

                string action =
                    _battleState.CurrentMenuOptions[selected];

                switch (action)
                {
                    case "SPELL":
                        _pendingAction = PendingAction.BasicAttack;
                        OpenTargetSelection();
                        break;

                    case "Z-SPELL":
                        OpenSpellMenu();
                        break;

                    case "ACT":
                        _battleState.TopMessage =
                            $"{CurrentHero.Name} intentó ACT.";

                        FinishPlayerAction();
                        break;

                    case "DEFEND":
                        _defending.Add(CurrentHero);

                        _battleState.TopMessage =
                            $"¡{CurrentHero.Name} se defendió!";

                        FinishPlayerAction();
                        break;

                    case "ITEMS":
                        _battleState.IsActionMenuOpen = false;
                        _battleState.IsItemMenuOpen = true;
                        _battleState.TopMessage = "No tienes objetos";
                        _currentPhase = BattlePhase.ItemsSelection;
                        break;

                    case "RUN":
                        EndBattle(
                            "¡El grupo escapó de la batalla!");
                        break;
                }
            }
        }

        private void OpenSpellMenu()
        {
            _battleState.IsActionMenuOpen = false;
            _battleState.IsSpellMenuOpen = true;
            _battleState.SelectedSpellIndex = 0;

            _battleState.TopMessage =
                $"{CurrentHero.Name}: elegí un hechizo.";

            _currentPhase = BattlePhase.SpellSelection;
        }

        private void UpdateSpellSelection()
        {
            List<string> spells = CurrentHero.Spells;
            int count = spells.Count;

            if (count == 0)
                return;

            if (Input.IsKeyPressed(Keys.Up) ||
                Input.IsKeyPressed(Keys.W))
            {
                _battleState.SelectedSpellIndex =
                    (_battleState.SelectedSpellIndex - 1 + count)
                    % count;
            }

            if (Input.IsKeyPressed(Keys.Down) ||
                Input.IsKeyPressed(Keys.S))
            {
                _battleState.SelectedSpellIndex =
                    (_battleState.SelectedSpellIndex + 1)
                    % count;
            }

            if (Input.IsKeyPressed(Keys.X) ||
                Input.IsKeyPressed(Keys.Back))
            {
                ReturnToActionMenu();
                return;
            }

            if (Input.IsKeyPressed(Keys.Z) ||
                Input.IsKeyPressed(Keys.Enter) ||
                Input.IsKeyPressed(Keys.Space))
            {
                string spell = spells[
                    _battleState.SelectedSpellIndex];

                int cost = GetSpellCost(spell);

                if (CurrentHero.PP < cost)
                {
                    _battleState.TopMessage =
                        $"¡No hay suficientes PP para {spell}!";

                    return;
                }

                // ShieldBeta protege al personaje que lo utiliza.
                if (spell == "ShieldBeta")
                {
                    CurrentHero.PP -= cost;
                    _defending.Add(CurrentHero);

                    _battleState.TopMessage =
                        $"¡{CurrentHero.Name} usó ShieldBeta!";

                    FinishPlayerAction();
                    return;
                }

                switch (spell)
                {
                    case "FireBeta":
                        _pendingAction = PendingAction.FireBeta;
                        break;

                    case "FireGamma":
                        _pendingAction = PendingAction.FireGamma;
                        break;

                    case "FreezeAlpha":
                        _pendingAction = PendingAction.FreezeAlpha;
                        break;

                    case "FreezeOmega":
                        _pendingAction = PendingAction.FreezeOmega;
                        break;

                    case "DefenseDownOmega":
                        _pendingAction =
                            PendingAction.DefenseDownOmega;
                        break;

                    default:
                        _battleState.TopMessage =
                            "Ese hechizo todavía no está implementado.";
                        return;
                }

                OpenTargetSelection();
            }
        }

        private int GetSpellCost(string spell)
        {
            switch (spell)
            {
                case "FireBeta":
                    return FireBetaCost;

                case "FireGamma":
                    return FireGammaCost;

                case "FreezeAlpha":
                    return FreezeAlphaCost;

                case "FreezeOmega":
                    return FreezeOmegaCost;

                case "DefenseDownOmega":
                    return DefenseDownOmegaCost;

                case "ShieldBeta":
                    return ShieldBetaCost;

                default:
                    return 0;
            }
        }

        private void OpenTargetSelection()
        {
            _battleState.IsActionMenuOpen = false;
            _battleState.IsSpellMenuOpen = false;
            _battleState.IsSelectingTarget = true;
            _battleState.SelectedEnemyIndex = 0;

            _currentPhase = BattlePhase.SelectingTarget;
        }

        private void UpdateTargetSelection()
        {
            int count = _currentBattleEnemies.Count;

            if (count == 0)
            {
                EndBattle("¡Todos los enemigos han sido derrotados!");
                return;
            }

            if (Input.IsKeyPressed(Keys.Left) ||
                Input.IsKeyPressed(Keys.A))
            {
                _battleState.SelectedEnemyIndex =
                    (_battleState.SelectedEnemyIndex - 1 + count)
                    % count;
            }

            if (Input.IsKeyPressed(Keys.Right) ||
                Input.IsKeyPressed(Keys.D))
            {
                _battleState.SelectedEnemyIndex =
                    (_battleState.SelectedEnemyIndex + 1)
                    % count;
            }

            Enemy target = _currentBattleEnemies[
                _battleState.SelectedEnemyIndex];

            _battleState.TopMessage =
                $"¿A quién atacará {CurrentHero.Name}? {target.Name}";

            if (Input.IsKeyPressed(Keys.X) ||
                Input.IsKeyPressed(Keys.Back))
            {
                _battleState.IsSelectingTarget = false;

                if (CurrentHero.Spells.Contains(
                    GetSpellName(_pendingAction)))
                {
                    _battleState.IsSpellMenuOpen = true;
                    _currentPhase = BattlePhase.SpellSelection;
                }
                else
                {
                    ReturnToActionMenu();
                }

                return;
            }

            if (Input.IsKeyPressed(Keys.Z) ||
                Input.IsKeyPressed(Keys.Enter) ||
                Input.IsKeyPressed(Keys.Space))
            {
                ExecuteSelectedAction(target);
            }
        }

        private string GetSpellName(PendingAction action)
        {
            switch (action)
            {
                case PendingAction.FireBeta:
                    return "FireBeta";

                case PendingAction.FireGamma:
                    return "FireGamma";

                case PendingAction.FreezeAlpha:
                    return "FreezeAlpha";

                case PendingAction.FreezeOmega:
                    return "FreezeOmega";

                case PendingAction.DefenseDownOmega:
                    return "DefenseDownOmega";

                default:
                    return "";
            }
        }

        private void ExecuteSelectedAction(Enemy target)
        {
            string spell = GetSpellName(_pendingAction);
            int damage;

            if (_pendingAction == PendingAction.BasicAttack)
            {
                damage = 10;
            }
            else
            {
                int cost = GetSpellCost(spell);

                if (CurrentHero.PP < cost)
                {
                    _battleState.TopMessage =
                        "¡No hay suficientes PP!";

                    ReturnToActionMenu();
                    return;
                }

                CurrentHero.PP -= cost;

                switch (_pendingAction)
                {
                    case PendingAction.FireBeta:
                        damage = 15;
                        break;

                    case PendingAction.FireGamma:
                        damage = 25;
                        break;

                    case PendingAction.FreezeAlpha:
                        damage = 15;
                        break;

                    case PendingAction.FreezeOmega:
                        damage = 25;
                        break;

                    case PendingAction.DefenseDownOmega:
                        _defenseDownEnemies.Add(target);
                        _battleState.TopMessage =
                            $"¡La defensa de {target.Name} disminuyó!";
                        FinishPlayerAction();
                        return;

                    default:
                        damage = 10;
                        break;
                }
            }

            target.HP = Math.Max(0, target.HP - damage);

            if (target.HP <= 0)
            {
                string defeatedName = target.Name;
                _currentBattleEnemies.Remove(target);
                _defenseDownEnemies.Remove(target);

                if (_currentBattleEnemies.Count == 0)
                {
                    EndBattle(
                        "¡Todos los enemigos han sido derrotados!");
                    return;
                }

                _battleState.SelectedEnemyIndex = 0;
                _battleState.TopMessage =
                    $"¡{defeatedName} fue derrotado!";
            }
            else if (_pendingAction == PendingAction.BasicAttack)
            {
                _battleState.TopMessage =
                    $"¡{CurrentHero.Name} atacó a {target.Name}!";
            }
            else
            {
                _battleState.TopMessage =
                    $"¡{CurrentHero.Name} usó {spell}!";
            }

            FinishPlayerAction();
        }

        private void ReturnToActionMenu()
        {
            _battleState.IsSpellMenuOpen = false;
            _battleState.IsSelectingTarget = false;
            _battleState.IsActionMenuOpen = true;
            _battleState.SelectedOptionIndex = 0;
            _battleState.TopMessage =
                $"¿Qué hará {CurrentHero.Name}?";

            _currentPhase = BattlePhase.PlayerTurn;
        }

        private void FinishPlayerAction()
        {
            _battleState.IsActionMenuOpen = false;
            _battleState.IsSpellMenuOpen = false;
            _battleState.IsSelectingTarget = false;

            _timer = 0f;
            _currentPhase = BattlePhase.PlayerActionResult;
        }

        private void AdvanceToNextHero()
        {
            int next = _currentPartyMemberIndex + 1;

            while (next < _battleState.PartyMembers.Count &&
                   _battleState.PartyMembers[next].HP <= 0)
            {
                next++;
            }

            if (next < _battleState.PartyMembers.Count)
            {
                _currentPartyMemberIndex = next;
                _battleState.CurrentPartyMemberIndex = next;
                _battleState.SelectedOptionIndex = 0;
                _battleState.IsActionMenuOpen = true;
                _battleState.IsSpellMenuOpen = false;
                _battleState.IsSelectingTarget = false;

                _battleState.TopMessage =
                    $"¿Qué hará {CurrentHero.Name}?";

                _currentPhase = BattlePhase.PlayerTurn;
            }
            else
            {
                _enemyIndex = 0;
                _timer = 0f;
                _currentPhase = BattlePhase.EnemyTurn;
            }
        }

        private void UpdateEnemyTurn(float dt)
        {
            _timer += dt;

            if (_timer < EnemyTurnDelay)
                return;

            _timer = 0f;

            if (_enemyIndex < _currentBattleEnemies.Count)
            {
                Enemy enemy = _currentBattleEnemies[_enemyIndex];
                _enemyIndex++;

                List<CharacterStats> aliveHeroes =
                    _battleState.PartyMembers.FindAll(p => p.HP > 0);

                if (aliveHeroes.Count == 0)
                {
                    EndBattle("Tu grupo ha caído...");
                    return;
                }

                CharacterStats target =
                    aliveHeroes[_random.Next(aliveHeroes.Count)];

                int damage = _random.Next(5, 12);

                // DefenseDownOmega aumenta el daño recibido por el enemigo
                // en el siguiente ataque de ese enemigo.
                if (_defenseDownEnemies.Contains(enemy))
                    damage += 0;

                if (_defending.Contains(target))
                    damage = Math.Max(1, damage / 2);

                target.HP = Math.Max(0, target.HP - damage);

                _battleState.TopMessage =
                    $"¡{enemy.Name} atacó a {target.Name} y le hizo " +
                    $"{damage} de daño!";

                if (_battleState.PartyMembers.TrueForAll(p => p.HP <= 0))
                {
                    EndBattle("Tu grupo ha caído...");
                }
            }
            else
            {
                _defending.Clear();
                OpenPlayerTurn();
            }
        }

        private void EndBattle(string message)
        {
            _battleState.TopMessage = message;
            _battleState.IsActionMenuOpen = false;
            _battleState.IsSpellMenuOpen = false;
            _battleState.IsSelectingTarget = false;

            _currentPhase = BattlePhase.BattleEnded;
            _timer = 0f;
        }

        public void Draw(
            SpriteBatch spriteBatch,
            GraphicsDevice graphicsDevice)
        {
            Vector2 screenSize = new Vector2(
                graphicsDevice.Viewport.Width,
                graphicsDevice.Viewport.Height);

            _battleUI.Draw(
                spriteBatch,
                _battleState,
                _currentBattleEnemies,
                screenSize);
        }
    }
}

