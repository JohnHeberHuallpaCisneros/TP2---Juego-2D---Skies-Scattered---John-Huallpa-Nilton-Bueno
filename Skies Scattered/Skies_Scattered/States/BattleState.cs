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
    }

    public void Initialize() { }
    public void LoadContent() { }

    public void Update(GameTime gameTime)
    {
        if (Input.IsKeyPressed(Keys.Z))
        {
            _battleState.IsActionMenuOpen = !_battleState.IsActionMenuOpen;
        }

        if (_battleState.IsActionMenuOpen && _battleState.CurrentMenuOptions.Count > 0)
        {
            if (Input.IsKeyPressed(Keys.Left) || Input.IsKeyPressed(Keys.A))
                _battleState.SelectedOptionIndex = Math.Max(0, _battleState.SelectedOptionIndex - 1);
            
            if (Input.IsKeyPressed(Keys.Right) || Input.IsKeyPressed(Keys.D))
                _battleState.SelectedOptionIndex = Math.Min(_battleState.CurrentMenuOptions.Count - 1, _battleState.SelectedOptionIndex + 1);
            
            if (Input.IsKeyPressed(Keys.Up) || Input.IsKeyPressed(Keys.W))
                _battleState.SelectedOptionIndex = Math.Max(0, _battleState.SelectedOptionIndex - 2);
            
            if (Input.IsKeyPressed(Keys.Down) || Input.IsKeyPressed(Keys.S))
                _battleState.SelectedOptionIndex = Math.Min(_battleState.CurrentMenuOptions.Count - 1, _battleState.SelectedOptionIndex + 2);

            if (Input.IsKeyPressed(Keys.Enter) || Input.IsKeyPressed(Keys.Space))
            {
                string selectedAction = _battleState.CurrentMenuOptions[_battleState.SelectedOptionIndex];
                _battleState.TopMessage = $"¡Yoko usó {selectedAction}!";
            }
        }

        if (Input.IsKeyPressed(Keys.Back))
        {  
            OnBattleEnded?.Invoke(); // Vuelve al mapa de exploración
        }
    }

    public void Draw(SpriteBatch spriteBatch, GraphicsDevice graphicsDevice)
    {
        Vector2 screenSize = new Vector2(graphicsDevice.Viewport.Width, graphicsDevice.Viewport.Height);  
        _battleUI.Draw(spriteBatch, _battleState, _currentBattleEnemies, screenSize);  
    }
}