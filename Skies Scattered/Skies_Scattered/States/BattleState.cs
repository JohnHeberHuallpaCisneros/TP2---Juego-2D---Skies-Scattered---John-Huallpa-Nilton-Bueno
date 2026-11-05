using Skies_Scattered.Audio;

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameLibrary;
using Skies_Scattered.Entities;
using Skies_Scattered.UI;
using Skies_Scattered.Graphics;
using Skies_Scattered.Mechanics;

namespace Skies_Scattered.States
{
    public class BattleState : IGameState
    {
        private BattleUI _battleUI;
        private BattleUIState _battleState;
        private List<Enemy> _currentBattleEnemies;
        private Random _random;
        private readonly SpellEffectPlayer _spellEffect;
        private readonly SpellEffectLibrary _effectLibrary;
        private float _flashRemaining, _flashStrength;
        private const float FlashDuration = 0.22f;
        public Color ScreenFlashColor { get; private set; } = Color.White;
        public float ScreenFlashOpacity => _flashStrength * MathHelper.Clamp(_flashRemaining / FlashDuration, 0, 1);
        private ArrowDefense _arrowDefense;
        private Action _afterDefense;
        private Action _afterSpell;
        private Enemy _effectTarget;
        private readonly ZoneProgress _zone;
        private readonly BattleShake _screenShake = new BattleShake();
        private readonly BattleShake _partyShake = new BattleShake();
        public Vector2 ShakeOffset => _screenShake.Offset;

        public event Action OnBattleEnded;
        public event Action OnBattleResolved;

        private enum BattlePhase
        {
            EnemyIntro,
            PlayerTurn,
            SpellSelection,
            SpellAnimation,
            ItemsSelection,
            ActSelection,
            SelectingTarget,
            PlayerActionResult,
            EnemyTurn,
            ArrowDefense,
            BattleEnded,
            BattleRewards
        }

        private enum PendingAction
        {
            BasicAttack,
            Spare,
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
        private readonly List<int> _initialAP = new List<int>();
        private readonly List<int> _initialCT = new List<int>();
        private int _rewardHeroIndex;
        private bool _battleClosed;

        private readonly Dictionary<CharacterStats, DefenseStance> _stances = new();
        private DefenseStance Stance(CharacterStats hero)
        {
            if (!_stances.TryGetValue(hero, out var stance))
                _stances.Add(hero, stance = new DefenseStance());
            return stance;
        }

        // Shield Beta conserva su protección propia durante una ronda.
        private readonly HashSet<CharacterStats> _defending =
            new HashSet<CharacterStats>();

        private readonly HashSet<Enemy> _defenseDownEnemies =
            new HashSet<Enemy>();

        // Los costes compartidos con la interfaz se definen en SpellRules.

        public class EnemyTemplate
        {
            public bool Floats { get; set; }
            public string Name { get; set; }
            public Texture2D Texture { get; set; }
            public int HP { get; set; }
            public int Attack { get; set; } = 8;
            public int Defense { get; set; }
        }

        public BattleState(
            SpriteFont font,
            Texture2D uiBorders,
            Texture2D earthboundHud,
            List<EnemyTemplate> enemyTemplates,
            List<CharacterStats> partyMembers,
            SpellEffectLibrary spellEffects = null,
            ZoneProgress zone = null)
        {
            _zone = zone ?? new ZoneProgress();
            _effectLibrary = spellEffects;
            if (spellEffects != null) _spellEffect = new SpellEffectPlayer(spellEffects);
            _battleUI = new BattleUI(font, uiBorders, earthboundHud);
            _random = new Random();
            _currentBattleEnemies = new List<Enemy>();

            if (enemyTemplates == null || enemyTemplates.Count == 0)
            {
                throw new ArgumentException(
                    "Debe existir al menos una plantilla de enemigo.",
                    nameof(enemyTemplates));
            }

            int cantidad = Math.Min(_random.Next(1, 4), _zone.RemainingEnemies);
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
                    Attack = template.Attack,
                    Defense = template.Defense,
                    Floats = template.Floats,
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
            foreach (CharacterStats hero in _battleState.PartyMembers)
            {
                _initialAP.Add(hero.AP);
                _initialCT.Add(hero.CT);
            }
            if (_currentBattleEnemies.Count == 0) EndBattle("No quedan enemigos en esta zona.");
        }

        // Se crea una vez por partida; cada batalla comparte estas estadísticas.
        public static List<CharacterStats> CreateParty()
        {
            var party = new List<CharacterStats>
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
            GameItem.ShareInventory(party);
            return party;
        }

        private string BuildEnemyIntroMessage()
        {
            if (_currentBattleEnemies.Count == 0) return "No quedan enemigos en esta zona.";
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
            if (_battleClosed) return;
            _battleState.DefendCooldown = Stance(CurrentHero).CooldownRounds;
            _battleUI.Update(gameTime);

            float dt =
                (float)gameTime.ElapsedGameTime.TotalSeconds;
            UpdateVisualEffects(dt);
            var previousPhase = _currentPhase;
            int previousOption = _battleState.SelectedOptionIndex;
            int previousSpell = _battleState.SelectedSpellIndex;
            int previousTarget = _battleState.SelectedEnemyIndex;
            int previousItem = _battleState.SelectedItemIndex;
            if (HandleTypingInput()) return;

            bool menuPhase = IsMenuPhase(_currentPhase);
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

                case BattlePhase.ActSelection:
                    UpdateActSelection();
                    break;

                case BattlePhase.SpellAnimation:
                    UpdateSpellAnimation(dt);
                    break;

                case BattlePhase.ItemsSelection:
                    UpdateItemsSelection();
                    break;

                case BattlePhase.SelectingTarget:
                    UpdateTargetSelection();
                    break;

                case BattlePhase.PlayerActionResult:
                    UpdatePlayerActionResult(dt);
                    break;

                case BattlePhase.ArrowDefense:
                    UpdateArrowDefense(dt);
                    break;

                case BattlePhase.EnemyTurn:
                    UpdateEnemyTurn(dt);
                    break;

                case BattlePhase.BattleEnded:
                    UpdateBattleEnd();
                    break;

                case BattlePhase.BattleRewards:
                    if (Input.IsKeyPressed(Keys.Z))
                    {
                        _rewardHeroIndex++;
                        if (_rewardHeroIndex >= _battleState.PartyMembers.Count) CompleteBattle();
                        else ShowHeroRewards();
                    }
                    break;
            }
            if (menuPhase && (_currentPhase != previousPhase || _battleClosed) &&
                (ConfirmPressed() || CancelPressed()))
                SoundEffects.Play(SoundCue.Select);
            if (menuPhase && _currentPhase == previousPhase &&
                (previousOption != _battleState.SelectedOptionIndex || previousSpell != _battleState.SelectedSpellIndex ||
                 previousTarget != _battleState.SelectedEnemyIndex || previousItem != _battleState.SelectedItemIndex))
                SoundEffects.Play(SoundCue.Move);
        }

        private void UpdateActSelection()
        {
            if (CancelPressed())
                ReturnToActionMenu();
            else if (ConfirmPressed())
            {
                _pendingAction = PendingAction.Spare;
                OpenTargetSelection();
            }
        }

        private void UpdateSpellAnimation(float dt)
        {
            _spellEffect.Update(dt);
            if (!_spellEffect.IsPlaying)
            {
                var resolve = _afterSpell;
                _afterSpell = null;
                resolve?.Invoke();
            }
        }

        private void UpdatePlayerActionResult(float dt)
        {
            if (!_battleState.Message.IsComplete) return;
            _timer += dt;

            if (_timer >= MessageDelay)
            {
                _timer = 0f;
                AdvanceToNextHero();
            }
        }

        private void UpdateArrowDefense(float dt)
        {
            _arrowDefense.Update(dt, Mechanics.ArrowDefense.ReadDirection());
            if (_arrowDefense.Done)
            {
                var resolveDefense = _afterDefense;
                _afterDefense = null;
                resolveDefense?.Invoke();
                _arrowDefense.Dispose();
                _arrowDefense = null;
            }
        }

        private void UpdateBattleEnd()
        {
            if (Input.IsKeyPressed(Keys.Z))
            {
                if (IsPartyDefeated()) CompleteBattle();
                else
                {
                    _rewardHeroIndex = 0;
                    ShowHeroRewards();
                }
            }
        }

        private void UpdateVisualEffects(float dt)
        {
            _screenShake.Update(dt);
            _partyShake.Update(dt);
            _battleState.PartyImpactOffset = _partyShake.Offset;
            _flashRemaining = Math.Max(0, _flashRemaining - dt);
            foreach (var enemy in _currentBattleEnemies) enemy.UpdateImpact(dt);
            _battleState.Message.Update(dt);
        }

        private bool HandleTypingInput()
        {
            if (_battleState.Message.IsComplete) return false;
            if (Input.IsKeyPressed(Keys.X))
            {
                _battleState.Message.Reveal();
                return true;
            }
            return ConfirmPressed() || Input.IsKeyPressed(Keys.Back);
        }

        private static bool ConfirmPressed()
        {
            return Input.IsKeyPressed(Keys.Z) || Input.IsKeyPressed(Keys.Enter) ||
                Input.IsKeyPressed(Keys.Space);
        }

        private static bool CancelPressed()
        {
            return Input.IsKeyPressed(Keys.X) || Input.IsKeyPressed(Keys.Back);
        }

        private bool IsPartyDefeated()
        {
            foreach (CharacterStats hero in _battleState.PartyMembers)
            {
                if (hero.HP > 0) return false;
            }
            return true;
        }

        private static bool IsMenuPhase(BattlePhase phase)
        {
            switch (phase)
            {
                case BattlePhase.PlayerTurn:
                case BattlePhase.SpellSelection:
                case BattlePhase.SelectingTarget:
                case BattlePhase.ItemsSelection:
                case BattlePhase.ActSelection:
                case BattlePhase.EnemyIntro:
                case BattlePhase.BattleEnded:
                case BattlePhase.BattleRewards:
                    return true;
                default:
                    return false;
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
            _spellEffect?.Stop();
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
            _battleState.IsActMenuOpen = false;
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

            if (ConfirmPressed())
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
                        OpenActMenu();
                        break;

                    case "DEFEND":
                        if (!Stance(CurrentHero).TryActivate())
                        {
                            _battleState.TopMessage = $"DEFEND se recarga en {Stance(CurrentHero).CooldownRounds} turnos.";
                            return;
                        }
                        SoundEffects.Play(SoundCue.Defend);
                        _battleState.DefendCooldown = Stance(CurrentHero).CooldownRounds;
                        _battleState.TopMessage =
                            $"¡{CurrentHero.Name}: CT +{DefenseStance.BonusCT} durante 3 turnos!";

                        FinishPlayerAction();
                        break;

                    case "ITEMS":
                        _battleState.IsActionMenuOpen = false;
                        _battleState.IsItemMenuOpen = true;
                        _battleState.SelectedItemIndex = 0;
                        _battleState.TopMessage = CurrentHero.Items.Count == 0 ? "No tienes objetos" : "Elige un objeto para usar.";
                        _currentPhase = BattlePhase.ItemsSelection;
                        break;

                    case "RUN":
                        AttemptEscape();
                        break;
                }
            }
        }

        private void AttemptEscape()
        {
            const double chance = .65;
            if (_random.NextDouble() < chance)
            {
                SoundEffects.Play(SoundCue.Escape);
                EndBattle("¡El grupo escapó de la batalla!");
            }
            else
            {
                _battleState.TopMessage = "¡No pudieron escapar!";
                FinishPlayerAction();
            }
        }

        private void UpdateItemsSelection()
        {
            if (CancelPressed())
            {
                _battleState.IsItemMenuOpen = false;
                ReturnToActionMenu(); return;
            }
            int count = CurrentHero.Items.Count;
            if (count == 0)
            {
                if (Input.IsKeyPressed(Keys.Z)) { _battleState.IsItemMenuOpen = false; ReturnToActionMenu(); }
                return;
            }
            if (Input.IsKeyPressed(Keys.Up) || Input.IsKeyPressed(Keys.W))
                _battleState.SelectedItemIndex = (_battleState.SelectedItemIndex - 1 + count) % count;
            if (Input.IsKeyPressed(Keys.Down) || Input.IsKeyPressed(Keys.S))
                _battleState.SelectedItemIndex = (_battleState.SelectedItemIndex + 1) % count;
            if (Input.IsKeyPressed(Keys.Z) || Input.IsKeyPressed(Keys.Enter))
            {
                bool used = GameItem.TryUse(CurrentHero, _battleState.SelectedItemIndex, out string message);
                _battleState.TopMessage = message;
                if (used) { _battleState.IsItemMenuOpen = false; FinishPlayerAction(); }
            }
        }

        private void OpenActMenu()
        {
            _battleState.IsActionMenuOpen = false;
            _battleState.IsActMenuOpen = true;
            _battleState.TopMessage = "PERDONAR: el enemigo debe tener 25% de HP o menos.";
            _currentPhase = BattlePhase.ActSelection;
        }

        private void TrySpare(Enemy target)
        {
            if (!CombatRules.CanSpare(target))
            {
                _battleState.TopMessage = $"{target.Name} aún no está listo para ser perdonado.";
                FinishPlayerAction();
                return;
            }
            _zone.RecordSpare(CurrentHero);
            _currentBattleEnemies.Remove(target);
            _defenseDownEnemies.Remove(target);
            _battleState.SelectedEnemyIndex = 0;
            if (_currentBattleEnemies.Count == 0)
                EndBattle($"¡{CurrentHero.Name} perdonó a {target.Name}! La batalla terminó.");
            else
            {
                _battleState.TopMessage = $"¡{CurrentHero.Name} perdonó a {target.Name}!";
                FinishPlayerAction();
            }
        }

        private void DamageEnemy(Enemy target, int power)
        {
            if (target.HP <= 0) return;
            int damage = CombatRules.DamageToEnemy(power, target, _defenseDownEnemies.Contains(target));
            target.HP = Math.Max(0, target.HP - damage);
            string attack = _pendingAction == PendingAction.BasicAttack ? "BasicAttack" : GetSpellName(_pendingAction);
            TriggerAttackFeedback(attack);
            target.TriggerImpact(damage, ScreenFlashColor, attack);
            if (target.HP == 0)
            {
                _zone.RecordDefeat(CurrentHero);
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

            if (CancelPressed())
            {
                ReturnToActionMenu();
                return;
            }

            if (ConfirmPressed())
            {
                string spell = spells[
                    _battleState.SelectedSpellIndex];

                int cost = SpellRules.GetCost(spell);

                if (CurrentHero.PP < cost)
                {
                    _battleState.TopMessage =
                        $"¡No hay suficientes PP para {SpellNames.Display(spell)}!";

                    return;
                }

                // ShieldBeta protege al personaje que lo utiliza.
                if (spell == "ShieldBeta")
                {
                    CurrentHero.PP -= cost;
                    _defending.Add(CurrentHero);

                    _battleState.TopMessage =
                        $"¡{CurrentHero.Name} usó ShieldBeta!";

                    StartSpellAnimation("ShieldBeta", FinishPlayerAction);
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

                if (_pendingAction == PendingAction.FireBeta || _pendingAction == PendingAction.FireGamma)
                    AnimateSelectedAction(null);
                else
                    OpenTargetSelection();
            }
        }



        private void OpenTargetSelection()
        {
            _battleState.IsActionMenuOpen = false;
            _battleState.IsSpellMenuOpen = false;
            _battleState.IsActMenuOpen = false;
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

            _battleState.TopMessage = _pendingAction == PendingAction.Spare
                ? $"¿A quién perdonará {CurrentHero.Name}? {target.Name}" + (CombatRules.CanSpare(target) ? " - Listo" : " - Aún no")
                : $"¿A quién atacará {CurrentHero.Name}? {target.Name}";

            if (CancelPressed())
            {
                _battleState.IsSelectingTarget = false;

                if (_pendingAction == PendingAction.Spare)
                {
                    OpenActMenu();
                }
                else if (CurrentHero.Spells.Contains(
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

            if (ConfirmPressed())
            {
                AnimateSelectedAction(target);
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
                damage = CombatRules.BasicPower(CurrentHero);
            }
            else
            {
                int cost = SpellRules.GetCost(spell);

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
                        damage = CombatRules.SpellPower(CurrentHero, 15);
                        break;

                    case PendingAction.FireGamma:
                        damage = CombatRules.SpellPower(CurrentHero, 25);
                        break;

                    case PendingAction.FreezeAlpha:
                        damage = CombatRules.SpellPower(CurrentHero, 15);
                        break;

                    case PendingAction.FreezeOmega:
                        damage = CombatRules.SpellPower(CurrentHero, 25);
                        break;

                    case PendingAction.DefenseDownOmega:
                        _defenseDownEnemies.Add(target);
                        TriggerAttackFeedback("DefenseDownOmega");
                        target.TriggerImpact(0, ScreenFlashColor, "DefenseDownOmega");
                        _battleState.TopMessage =
                            $"¡La defensa de {target.Name} disminuyó!";
                        FinishPlayerAction();
                        return;

                    default:
                        damage = CombatRules.BasicPower(CurrentHero);
                        break;
                }
            }

            if (_pendingAction == PendingAction.FireBeta || _pendingAction == PendingAction.FireGamma)
            {
                foreach (var enemy in _currentBattleEnemies)
                    DamageEnemy(enemy, damage);
                _currentBattleEnemies.RemoveAll(enemy => enemy.HP <= 0);
                _defenseDownEnemies.RemoveWhere(enemy => enemy.HP <= 0);
                _battleState.SelectedEnemyIndex = 0;
                if (_currentBattleEnemies.Count == 0)
                    EndBattle("¡Todos los enemigos han sido derrotados!");
                else
                {
                    _battleState.TopMessage = $"¡{CurrentHero.Name} usó {SpellNames.Display(spell)} contra todos los enemigos!";
                    FinishPlayerAction();
                }
                return;
            }

            DamageEnemy(target, damage);

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
                    $"¡{CurrentHero.Name} usó {SpellNames.Display(spell)}!";
            }

            FinishPlayerAction();
        }

        private void AnimateSelectedAction(Enemy target)
        {
            if (_pendingAction == PendingAction.Spare)
            {
                TrySpare(target);
                return;
            }
            if (_pendingAction == PendingAction.BasicAttack)
            {
                SoundEffects.Play(SoundCue.BasicAttack);
                SoundEffects.Play(SoundCue.Damage);
                ExecuteSelectedAction(target);
                return;
            }
            string spell = GetSpellName(_pendingAction);
            if (CurrentHero.PP < SpellRules.GetCost(spell))
            {
                ReturnToActionMenu();
                _battleState.TopMessage = "¡No hay suficientes PP!";
                return;
            }
            _battleState.TopMessage = $"¡{CurrentHero.Name} usó {SpellNames.Display(spell)}!";
            bool affectsAll = _pendingAction == PendingAction.FireBeta || _pendingAction == PendingAction.FireGamma;
            StartSpellAnimation(spell, () => ExecuteSelectedAction(target), affectsAll ? null : target);
        }

        private void StartSpellAnimation(string spell, Action resolve, Enemy effectTarget = null)
        {
            SoundEffects.PlaySpell(spell, _spellEffect == null ? 0 : _spellEffect.DurationFor(spell));
            Action finish = () =>
            {
                if (spell == "ShieldBeta") TriggerAttackFeedback(spell);
                if (spell == "FireGamma" || spell == "FreezeOmega")
                    SoundEffects.Play(SoundCue.FireBeta, -.45f);
                if (spell == "FireBeta" || spell == "FireGamma" ||
                    spell == "FreezeAlpha" || spell == "FreezeOmega")
                    SoundEffects.Play(SoundCue.Damage);
                resolve();
            };
            if (_spellEffect == null) { finish(); return; }
            _battleState.IsActionMenuOpen = false;
            _battleState.IsSpellMenuOpen = false;
            _battleState.IsActMenuOpen = false;
            _battleState.IsSelectingTarget = false;
            _spellEffect.Play(spell);
            _effectTarget = effectTarget;
            _afterSpell = finish;
            _currentPhase = BattlePhase.SpellAnimation;
        }

        private void TriggerAttackFeedback(string attack, float intensity = 1f)
        {
            _screenShake.Trigger(attack, intensity: intensity);
            ScreenFlashColor = attack == "BasicAttack" || _effectLibrary == null
                ? new Color(255, 245, 220) : _effectLibrary.GetFlashColor(attack);
            _flashStrength = attack switch
            {
                "FireGamma" => 0.16f, "FireBeta" => 0.13f,
                "FreezeOmega" => 0.13f, "FreezeAlpha" => 0.10f,
                "EnemyBeam" => 0.12f, "ShieldBeta" => 0.08f,
                "DefenseDownOmega" => 0.09f, _ => 0.06f
            };
            _flashRemaining = FlashDuration;
        }

        private void ReturnToActionMenu()
        {
            _spellEffect?.Stop();
            _battleState.IsSpellMenuOpen = false;
            _battleState.IsActMenuOpen = false;
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
            _battleState.IsActMenuOpen = false;
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
            _battleState.IsActMenuOpen = false;
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
            if (!_battleState.Message.IsComplete) return;
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

                int damage = Math.Max(1, enemy.Attack + _random.Next(-2, 3));

                damage = CombatRules.DamageToHero(damage, target, _defending.Contains(target), Stance(target).CurrentBonusCT);

                _battleState.TopMessage = $"¡{enemy.Name} apunta a {target.Name}! Sigue las flechas.";
                _battleState.Message.Reveal();
                _arrowDefense = new ArrowDefense(_random);
                _currentPhase = BattlePhase.ArrowDefense;
                _afterDefense = () =>
                {
                    int correct = _arrowDefense.Correct;
                    int pp = Math.Min(_arrowDefense.RecoveredPP, Math.Max(0, target.MaxPP - target.PP));
                    target.PP += pp;
                    int finalDamage = Math.Max(1, (int)Math.Round(damage * _arrowDefense.DamageMultiplier,
                        MidpointRounding.AwayFromZero));
                    _battleState.TopMessage = $"{correct}/6 aciertos. {target.Name}: PP +{pp}.";
                    StartSpellAnimation("EnemyBeam", () =>
                    {
                        float impact = MathHelper.Clamp(finalDamage / 8f, .25f, 1.6f);
                        TriggerAttackFeedback("EnemyBeam", impact);
                        _battleState.ImpactedPartyIndex = _battleState.PartyMembers.IndexOf(target);
                        _partyShake.Trigger("EnemyBeam", intensity: impact * 1.5f);
                        SoundEffects.Play(SoundCue.Damage);
                        target.HP = Math.Max(0, target.HP - finalDamage);
                        _battleState.TopMessage = $"¡{enemy.Name} atacó a {target.Name} y le hizo {finalDamage} de daño!";
                        _timer = 0f;
                        if (IsPartyDefeated())
                            EndBattle("Tu grupo ha caído...");
                        else
                            _currentPhase = BattlePhase.EnemyTurn;
                    });
                };
            }
            else
            {
                _defending.Clear();
                foreach (var stance in _stances.Values) stance.EndEnemyRound();
                OpenPlayerTurn();
            }
        }

        private void EndBattle(string message)
        {
            _battleState.TopMessage = "¡El combate finalizó!\n" + message;
            _battleState.IsAwaitingBattleEnd = true;
            _battleState.IsItemMenuOpen = false;
            _battleState.IsActionMenuOpen = false;
            _battleState.IsSpellMenuOpen = false;
            _battleState.IsActMenuOpen = false;
            _battleState.IsSelectingTarget = false;

            _currentPhase = BattlePhase.BattleEnded;
            _timer = 0f;
            OnBattleResolved?.Invoke();
        }

        private void ShowHeroRewards()
        {
            CharacterStats hero = _battleState.PartyMembers[_rewardHeroIndex];
            int gainedAP = Math.Max(0, hero.AP - _initialAP[_rewardHeroIndex]);
            int gainedCT = Math.Max(0, hero.CT - _initialCT[_rewardHeroIndex]);
            if (gainedAP > 0 || gainedCT > 0) SoundEffects.Play(SoundCue.StatGain);
            _battleState.TopMessage = $"{hero.Name} ganó en este combate:\nAP +{gainedAP}    CT +{gainedCT}";
            _currentPhase = BattlePhase.BattleRewards;
        }

        private void CompleteBattle()
        {
            if (_battleClosed) return;
            _battleClosed = true;
            var ended = OnBattleEnded;
            OnBattleEnded = null;
            _battleUI.Dispose();
            ended?.Invoke();
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
            if (_currentPhase == BattlePhase.ArrowDefense)
                _arrowDefense?.Draw(spriteBatch, graphicsDevice,
                    new Rectangle(0, BattleUI.TopBandHeight, (int)screenSize.X,
                        (int)screenSize.Y - BattleUI.TopBandHeight - BattleUI.BottomBandHeight));
            if (_currentPhase == BattlePhase.SpellAnimation)
            {
                Rectangle arena = new Rectangle(0, BattleUI.TopBandHeight, (int)screenSize.X,
                    (int)screenSize.Y - BattleUI.TopBandHeight - BattleUI.BottomBandHeight);
                Rectangle effectArea = arena;
                if (_effectTarget != null)
                {
                    // El lienzo incluye margen transparente; el hielo visible ocupa el centro.
                    const int size = 384;
                    effectArea = new Rectangle((int)_effectTarget.Position.X - size / 2,
                        (int)_effectTarget.Position.Y - size / 2, size, size);
                }
                _spellEffect?.Draw(spriteBatch, effectArea, arena);
            }
        }
    }
}


