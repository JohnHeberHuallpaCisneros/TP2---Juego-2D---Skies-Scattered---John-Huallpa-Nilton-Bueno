using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameLibrary;
using Skies_Scattered.UI;

namespace Skies_Scattered.States;

public class GameOverState : IGameState
{
    private readonly SpriteFont _font;
    private readonly Texture2D _hud;
    private readonly Texture2D _pixel;
    public event Action OnRetry;
    public event Action OnMainMenu;

    public GameOverState(SpriteFont font, Texture2D hud, Texture2D pixel)
    {
        _font = font;
        _hud = hud;
        _pixel = pixel;
    }

    public void Initialize() { }
    public void LoadContent() { }

    public void Update(GameTime gameTime)
    {
        if (Input.IsKeyPressed(Keys.Z)) OnRetry?.Invoke();
        else if (Input.IsKeyPressed(Keys.Escape)) OnMainMenu?.Invoke();
    }

    public void Draw(SpriteBatch batch, GraphicsDevice graphics)
    {
        int width = Math.Min(420, graphics.Viewport.Width - 32);
        Rectangle box = new Rectangle((graphics.Viewport.Width - width) / 2,
            (graphics.Viewport.Height - 190) / 2, width, 190);
        EarthboundBox.Draw(batch, _hud, _pixel, box);
        Center(batch, "GAME OVER", box, 25, Color.IndianRed);
        Center(batch, "Yoko y Kyoko han caído.", box, 65, Color.White);
        Center(batch, "Z: Reiniciar partida", box, 112, Color.White);
        Center(batch, "Escape: Volver al menú", box, 144, Color.LightGray);
    }

    private void Center(SpriteBatch batch, string text, Rectangle box, int offset, Color color)
    {
        Vector2 size = _font.MeasureString(text);
        float scale = Math.Min(1f, (box.Width - 32) / Math.Max(1, size.X));
        batch.DrawString(_font, text,
            new Vector2((int)(box.Center.X - size.X * scale / 2), box.Y + offset),
            color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0);
    }
}
