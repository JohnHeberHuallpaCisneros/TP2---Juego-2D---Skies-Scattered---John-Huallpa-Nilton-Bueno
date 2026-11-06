using Skies_Scattered.Audio;
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
    private readonly Texture2D _background;
    private Texture2D _pixel;
    private readonly List<string> _options = new List<string>
    {
        "Jugar",
        "Opciones",
        "Salir"
    };
    private int _selected = 0;
    public event Action OnStartGame;
    public event Action OnExitGame;
    public event Action OnOptions;
    public MainMenuState(SpriteFont font, Texture2D hudTexture, Texture2D background = null)
    {
        _font = font;
        _hudTexture = hudTexture;
        _background = background;
        _selected = 0;
    }



    public void Update(GameTime gameTime)
    {
        int before = _selected;
        if (Input.IsKeyPressed(Keys.Up) || Input.IsKeyPressed(Keys.W))
            _selected = (_selected - 1 + _options.Count) % _options.Count;
        if (Input.IsKeyPressed(Keys.Down) || Input.IsKeyPressed(Keys.S))
            _selected = (_selected + 1) % _options.Count;
        if (_selected != before)
            SoundEffects.Play(SoundCue.Move);
        if (Input.IsKeyPressed(Keys.Z) || Input.IsKeyPressed(Keys.Enter) || Input.IsKeyPressed(Keys.Space))
        {
            SoundEffects.Play(SoundCue.Select);
            switch (_options[_selected])
            {
                case "Jugar":
                    OnStartGame?.Invoke();
                    break;
                case "Opciones":
                    OnOptions?.Invoke();
                    break;
                case "Salir":
                    OnExitGame?.Invoke();
                    break;
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
        ScreenBackground.Draw(spriteBatch, _pixel, _background, new Rectangle(0, 0, w, h), new Color(150, 135, 175));
        if (_font == null)
            return;
        string title = "Skies Scattered";
        float titleScale = 1.6f;
        Vector2 titleSize = _font.MeasureString(title) * titleScale;
        Vector2 titlePosition = new Vector2((float)Math.Round((w - titleSize.X) / 2f), (float)Math.Round(h * 0.18f));
        spriteBatch.DrawString(_font, title, titlePosition + new Vector2(2, 2), Color.Black * .7f, 0f, Vector2.Zero, titleScale, SpriteEffects.None, 0f);
        spriteBatch.DrawString(_font, title, titlePosition, Color.White, 0f, Vector2.Zero, titleScale, SpriteEffects.None, 0f);
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
            Vector2 textPos = new Vector2(box.X + (box.Width - size.X) / 2f, box.Y + (box.Height - size.Y) / 2f);
            spriteBatch.DrawString(_font, text, textPos, Color.White, 0f, Vector2.Zero, textScale, SpriteEffects.None, 0f);
        }
    }
}
