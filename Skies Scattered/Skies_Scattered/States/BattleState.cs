using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameLibrary;
using Skies_Scattered.Entities;
using Skies_Scattered.UI;

namespace Skies_Scattered.States;

public class BattleState : IGameState
{
    private BattleUI _battleUI;
    private BattleUIState _battleState;
    private List<Enemy> _currentBattleEnemies;
    private Random _random;
    public event Action OnBattleEnded;

    // Fases y control de turnos del grupo
    private enum BattlePhase
    {
        PlayerTurn,
        SelectingTarget,

        PlayerActionResult, // muestra el resultado de la acción del jugador
        EnemyTurn,
        BattleEnded
    }

    private BattlePhase _currentPhase = BattlePhase.PlayerTurn;
    private float _timer = 0f;
    private const float MessageDelay = 1.2f;
    private const float EnemyTurnDelay = 1.2f;
    private int _currentPartyMemberIndex = 0;
    private int _enemyIndex = 0;
    private readonly HashSet<CharacterStats> _defending = new HashSet<CharacterStats>();

    public class EnemyTemplate
    {
        public string Name { get; set; }
        public Texture2D Texture { get; set; }
        public int HP { get; set; }
    }

    public BattleState(SpriteFont font, Texture2D uiBorders, Texture2D earthboundHud, List<EnemyTemplate> enemyTemplates)
    {
        _battleUI = new BattleUI(font, uiBorders, earthboundHud);
        _random = new Random();
        _currentBattleEnemies = new List<Enemy>();

        // Seleccionar aleatoriamente 1 o 2 enemigos de la lista general
        int cantidad = _random.Next(1, 3);
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
                Position = new Vector2(startX + (i * 120f), 180f)
            };

            _currentBattleEnemies.Add(enemy);
        }

        _battleState = new BattleUIState  
        {  
            TopMessage = $"¡Apareció un {_currentBattleEnemies[0].Name}!",  
            IsActionMenuOpen = false,  
            PartyMembers = new List<CharacterStats>  
            {  
                new CharacterStats { Name = "Yoko", AP = 10, HP = 50, CT = 5 },  
                new CharacterStats { Name = "Kyoko", AP = 12, HP = 50, CT = 4 }  
            },
            CurrentMenuOptions = new List<string> { "Bash", "Goods", "PSI", "Defend" }
        };
        _battleState.IsActionMenuOpen = true;
        _battleState.TopMessage = $"¿Qué hará {_battleState.PartyMembers[0].Name}?";
    }

    public void Initialize() { }
    public void LoadContent() { }

    public void Update(GameTime gameTime)
{
    _battleUI.Update(gameTime);
    float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

    switch (_currentPhase)
    {
        case BattlePhase.SelectingTarget:
        UpdateTargetSelection();
        break;

        case BattlePhase.BattleEnded:
            return;

        case BattlePhase.PlayerTurn:
            UpdatePlayerTurn();
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
            
    }
}

private void UpdatePlayerTurn()
{
    if (!_battleState.IsActionMenuOpen) return;

    int count = _battleState.CurrentMenuOptions.Count;

    if (Input.IsKeyPressed(Keys.Left) || Input.IsKeyPressed(Keys.A))
        _battleState.SelectedOptionIndex = Math.Max(0, _battleState.SelectedOptionIndex - 1);
    if (Input.IsKeyPressed(Keys.Right) || Input.IsKeyPressed(Keys.D))
        _battleState.SelectedOptionIndex = Math.Min(count - 1, _battleState.SelectedOptionIndex + 1);
    if (Input.IsKeyPressed(Keys.Up) || Input.IsKeyPressed(Keys.W))
        _battleState.SelectedOptionIndex = Math.Max(0, _battleState.SelectedOptionIndex - 2);
    if (Input.IsKeyPressed(Keys.Down) || Input.IsKeyPressed(Keys.S))
        _battleState.SelectedOptionIndex = Math.Min(count - 1, _battleState.SelectedOptionIndex + 2);

    if (Input.IsKeyPressed(Keys.Back))
    {
        _currentPhase = BattlePhase.BattleEnded;
        OnBattleEnded?.Invoke();
        return;
    }

    if (Input.IsKeyPressed(Keys.Z) || Input.IsKeyPressed(Keys.Enter) || Input.IsKeyPressed(Keys.Space))
    {
        string action = _battleState.CurrentMenuOptions[_battleState.SelectedOptionIndex];
        var hero = _battleState.PartyMembers[_currentPartyMemberIndex];
        _battleState.IsActionMenuOpen = false;

        switch (action)
        {
            case "Bash":
                 if (_currentBattleEnemies.Count > 0)
    {
        _battleState.SelectedEnemyIndex = 0;
        _battleState.IsSelectingTarget = true;
        _battleState.TopMessage = $"¿A quién atacará {hero.Name}?  {_currentBattleEnemies[0].Name}";
        _currentPhase = BattlePhase.SelectingTarget;
    }
    return;

            case "Goods":
                _battleState.TopMessage = $"¡{hero.Name} usó un objeto!";
                break;

            case "PSI":
                _battleState.TopMessage = $"¡{hero.Name} usó PSI!";
                break;

            case "Defend":
                _defending.Add(hero);
                _battleState.TopMessage = $"¡{hero.Name} se defendió!";
                break;
        }

        // Primero mostramos el resultado, luego se avanza
        _timer = 0f;
        _currentPhase = BattlePhase.PlayerActionResult;
    }
}

private void AdvanceToNextHero()
{
    // Buscar el siguiente héroe vivo
    int next = _currentPartyMemberIndex + 1;
    while (next < _battleState.PartyMembers.Count && _battleState.PartyMembers[next].HP <= 0)
        next++;

    if (next < _battleState.PartyMembers.Count)
    {
        _currentPartyMemberIndex = next;
        _battleState.IsActionMenuOpen = true;
        _battleState.SelectedOptionIndex = 0;
        _battleState.TopMessage = $"¿Qué hará {_battleState.PartyMembers[next].Name}?";
        _currentPhase = BattlePhase.PlayerTurn;
    }
    else
    {
        // Turno de los enemigos
        _currentPhase = BattlePhase.EnemyTurn;
        _enemyIndex = 0;
        _timer = EnemyTurnDelay; // el primer enemigo ataca de inmediato
    }
}

private void UpdateEnemyTurn(float dt)
{
    _timer += dt;
    if (_timer < EnemyTurnDelay) return;
    _timer = 0f;

    if (_enemyIndex < _currentBattleEnemies.Count)
    {
        var enemy = _currentBattleEnemies[_enemyIndex];
        _enemyIndex++;

        var aliveHeroes = _battleState.PartyMembers.FindAll(p => p.HP > 0);
        if (aliveHeroes.Count == 0) return;

        var target = aliveHeroes[_random.Next(aliveHeroes.Count)];
        int damage = _random.Next(5, 12);
        if (_defending.Contains(target)) damage /= 2;

        target.HP = Math.Max(0, target.HP - damage);
        _battleState.TopMessage = $"¡{enemy.Name} atacó a {target.Name} y le hizo {damage} de daño!";

        // ¿Cayó todo el grupo?
        if (_battleState.PartyMembers.TrueForAll(p => p.HP <= 0))
        {
            _battleState.TopMessage = "Tu grupo ha caído...";
            _currentPhase = BattlePhase.BattleEnded;
            OnBattleEnded?.Invoke();
        }
    }
    else
    {
        // Todos los enemigos ya atacaron: nueva ronda
        _defending.Clear();
        _currentPartyMemberIndex = _battleState.PartyMembers.FindIndex(p => p.HP > 0);
        _battleState.IsActionMenuOpen = true;
        _battleState.SelectedOptionIndex = 0;
        _battleState.TopMessage = $"¿Qué hará {_battleState.PartyMembers[_currentPartyMemberIndex].Name}?";
        _currentPhase = BattlePhase.PlayerTurn;
    }
}

private void UpdateTargetSelection()
{
    int count = _currentBattleEnemies.Count;
    if (count == 0) return;

    var hero = _battleState.PartyMembers[_currentPartyMemberIndex];

    if (Input.IsKeyPressed(Keys.Left) || Input.IsKeyPressed(Keys.A))
        _battleState.SelectedEnemyIndex = (_battleState.SelectedEnemyIndex - 1 + count) % count;

    if (Input.IsKeyPressed(Keys.Right) || Input.IsKeyPressed(Keys.D))
        _battleState.SelectedEnemyIndex = (_battleState.SelectedEnemyIndex + 1) % count;

    _battleState.SelectedEnemyIndex = Math.Clamp(_battleState.SelectedEnemyIndex, 0, count - 1);
    var target = _currentBattleEnemies[_battleState.SelectedEnemyIndex];
    _battleState.TopMessage = $"¿A quién atacará {hero.Name}?  {target.Name}";

    // Cancelar: volver al menú
    if (Input.IsKeyPressed(Keys.X) || Input.IsKeyPressed(Keys.Back))
    {
        _battleState.IsSelectingTarget = false;
        _battleState.IsActionMenuOpen = true;
        _battleState.TopMessage = $"¿Qué hará {hero.Name}?";
        _currentPhase = BattlePhase.PlayerTurn;
        return;
    }

    // Confirmar ataque
    if (Input.IsKeyPressed(Keys.Z) || Input.IsKeyPressed(Keys.Enter) || Input.IsKeyPressed(Keys.Space))
    {
        _battleState.IsSelectingTarget = false;
        target.HP -= 10;

        if (target.HP <= 0)
        {
            _currentBattleEnemies.Remove(target);

            if (_currentBattleEnemies.Count == 0)
            {
                _battleState.TopMessage = "¡Todos los enemigos han sido derrotados!";
                _currentPhase = BattlePhase.BattleEnded;
                OnBattleEnded?.Invoke();
                return;
            }
            _battleState.TopMessage = $"¡{target.Name} fue derrotado!";
        }
        else
        {
            _battleState.TopMessage = $"¡{hero.Name} atacó a {target.Name}!";
        }

        _timer = 0f;
        _currentPhase = BattlePhase.PlayerActionResult;
    }
}
    public void Draw(SpriteBatch spriteBatch, GraphicsDevice graphicsDevice)
    {
        Vector2 screenSize = new Vector2(graphicsDevice.Viewport.Width, graphicsDevice.Viewport.Height);  
        _battleUI.Draw(spriteBatch, _battleState, _currentBattleEnemies, screenSize);  
    }
}
