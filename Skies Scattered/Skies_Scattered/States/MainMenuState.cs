using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameLibrary;
using Skies_Scattered.UI;
namespace Skies_Scattered.States;

public class MainMenuState : IGameState
{
    private readonly SpriteFont _font;
    private readonly Texture2D _hudTexture;
    private Texture2D _pixel;
    private readonly List<string> _options = new List<string> { "Jugar", "Salir" };
    private int _selected = 0;

    public event Action OnStartGame;
    public event Action OnExitGame;

    public MainMenuState(SpriteFont font, Texture2D hudTexture)
    {
        _font = font;
        _hudTexture = hudTexture;
    }

    public void Initialize() { }
    public void LoadContent() { }

    public void Update(GameTime gameTime)
    {
        if (Input.IsKeyPressed(Keys.Up) || Input.IsKeyPressed(Keys.W))
            _selected = (_selected - 1 + _options.Count) % _options.Count;

        if (Input.IsKeyPressed(Keys.Down) || Input.IsKeyPressed(Keys.S))
            _selected = (_selected + 1) % _options.Count;

        if (Input.IsKeyPressed(Keys.Z) || Input.IsKeyPressed(Keys.Enter) || Input.IsKeyPressed(Keys.Space))
        {
            switch (_options[_selected])
            {
                case "Jugar": OnStartGame?.Invoke(); break;
                case "Salir": OnExitGame?.Invoke(); break;
            }
        }
    }

    public void Draw(SpriteBatch spriteBatch, GraphicsDevice graphicsDevice)
{
    if (_pixel == null)
    {
        _pixel = new Texture2D(graphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
    }

    int w = graphicsDevice.Viewport.Width;
    int h = graphicsDevice.Viewport.Height;

    // Fondo oscuro
    spriteBatch.Draw(_pixel, new Rectangle(0, 0, w, h), new Color(10, 10, 40));

    if (_font == null) return;

    // Título centrado
    string title = "Skies Scattered";
    float titleScale = 1.6f;
    Vector2 titleSize = _font.MeasureString(title) * titleScale;
    spriteBatch.DrawString(_font, title,
        new Vector2((w - titleSize.X) / 2f, h * 0.18f),
        Color.White, 0f, Vector2.Zero, titleScale, SpriteEffects.None, 0f);

    // Opciones, cada una dentro de su recuadro
    const int boxW = 220;
    const int boxH = 56;
    const int spacing = 20;
    const float textScale = 0.85f;
    float startY = h * 0.5f;

    for (int i = 0; i < _options.Count; i++)
    {
        bool isSelected = i == _selected;

        Rectangle box = new Rectangle((w - boxW) / 2, (int)startY + i * (boxH + spacing), boxW, boxH);
        EarthboundBox.Draw(spriteBatch, _hudTexture, _pixel, box);
        string text = isSelected ? "> " + _options[i] : _options[i];
        Vector2 size = _font.MeasureString(text) * textScale;
        Vector2 textPos = new Vector2(box.X + (box.Width - size.X) / 2f,
                                      box.Y + (box.Height - size.Y) / 2f);

        spriteBatch.DrawString(_font, text, textPos,
            isSelected ? Color.Cyan : Color.White,
            0f, Vector2.Zero, textScale, SpriteEffects.None, 0f);
    }
}
}