using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameLibrary;

namespace Skies_Scattered.UI;

public class OverworldUI
{
    public bool IsOpen { get; private set; }
    private int _heroIndex;
    private int _option = 1;
    private bool _details;

    public void Toggle()
    {
        IsOpen = !IsOpen;
        if (IsOpen)
        {
            _heroIndex = 0;
            _option = 1;
            _details = false;
        }
    }

    public void Update(List<CharacterStats> party)
    {
        if (!IsOpen || party.Count == 0) return;
        if (Input.IsKeyPressed(Keys.X) || Input.IsKeyPressed(Keys.Escape))
        {
            if (_details) _details = false;
            else IsOpen = false;
            return;
        }
        if (Input.IsKeyPressed(Keys.Right))
            _heroIndex = (_heroIndex + 1) % party.Count;
        if (Input.IsKeyPressed(Keys.Left))
            _heroIndex = (_heroIndex - 1 + party.Count) % party.Count;

        CharacterStats hero = party[_heroIndex];
        if (!_details)
        {
            if (Input.IsKeyPressed(Keys.Up) || Input.IsKeyPressed(Keys.Down))
                _option = hero.Items.Count == 0 ? 1 : 1 - _option;
            if (hero.Items.Count == 0) _option = 1;
            if (Input.IsKeyPressed(Keys.Z) && (_option == 1 || hero.Items.Count > 0))
                _details = true;
        }
    }

    private static void Box(SpriteBatch batch, Texture2D pixel, Texture2D hud, Rectangle box)
    {
        EarthboundBox.Draw(batch, hud, pixel, box);
    }

    private static void Text(SpriteBatch batch, SpriteFont font, string text,
        Vector2 position, float width, float height, Color color, float preferred = 1f)
    {
        Vector2 size = font.MeasureString(text);
        float scale = Math.Min(preferred, Math.Min(width / Math.Max(1, size.X),
            height / Math.Max(1, size.Y)));
        batch.DrawString(font, text, position, color, 0, Vector2.Zero, scale,
            SpriteEffects.None, 0);
    }

    public void Draw(SpriteBatch batch, Texture2D pixel, Texture2D hud, SpriteFont font,
        List<CharacterStats> party, Vector2 screen)
    {
        if (!IsOpen || party.Count == 0 || font == null) return;
        CharacterStats hero = party[_heroIndex];
        batch.Draw(pixel, new Rectangle(0, 0, (int)screen.X, (int)screen.Y), Color.Black * 0.65f);

        int x = ((int)screen.X - 576) / 2;
        int y = ((int)screen.Y - 392) / 2;
        Rectangle summary = new Rectangle(x, y, 164, 150);
        Rectangle actions = new Rectangle(x, y + 162, 164, 120);
        Rectangle detail = new Rectangle(x + 180, y, 396, 352);
        Box(batch, pixel, hud, summary);
        Box(batch, pixel, hud, actions);

        Text(batch, font, hero.Name, new Vector2(x + 14, y + 14), 136, 28, Color.White);
        Text(batch, font, $"HP {hero.HP}/{hero.MaxHP}", new Vector2(x + 14, y + 62), 136, 25, Color.White);
        Text(batch, font, $"PP {hero.PP}/{hero.MaxPP}", new Vector2(x + 14, y + 98), 136, 25, Color.White);
        bool itemsEnabled = hero.Items.Count > 0;
        Text(batch, font, (_option == 0 ? "> " : "  ") + "ITEM",
            new Vector2(x + 18, actions.Y + 22), 130, 28,
            itemsEnabled ? (_option == 0 ? Color.Cyan : Color.White) : Color.Gray);
        Text(batch, font, (_option == 1 ? "> " : "  ") + "STAT",
            new Vector2(x + 18, actions.Y + 68), 130, 28,
            _option == 1 ? Color.Cyan : Color.White);

        Text(batch, font, "C: Cerrar   X: Volver   Z: Abrir   Izq / Der: Personaje",
            new Vector2(x, y + 368), 576, 24, Color.White);
        if (!_details) return;

        // El panel de información se abre al confirmar STAT, no al abrir el menú.
        if (_option == 0) detail = new Rectangle(detail.X, detail.Y, 396, 320);
        Box(batch, pixel, hud, detail);
        int dx = detail.X + 20;
        Text(batch, font, $"<  {hero.Name}  >", new Vector2(dx, y + 18), 356, 30, Color.White);
        if (_details && _option == 0)
        {
            Text(batch, font, "ITEM", new Vector2(dx, y + 66), 356, 28, Color.Cyan);
            if (hero.Items.Count == 0)
                Text(batch, font, "No tienes objetos", new Vector2(dx, y + 112), 356, 26, Color.Gray);
            else
                for (int i = 0; i < Math.Min(hero.Items.Count, 6); i++)
                    Text(batch, font, hero.Items[i], new Vector2(dx, y + 108 + i * 30), 356, 26, Color.White);
        }
        else if (_details)
        {
            Text(batch, font, $"HP {hero.HP} / {hero.MaxHP}    PP {hero.PP} / {hero.MaxPP}",
                new Vector2(dx, y + 70), 356, 26, Color.White);
            Text(batch, font, $"AP {hero.AP}     CT {hero.CT}",
                new Vector2(dx, y + 116), 356, 28, Color.White);
            Text(batch, font, "HECHIZOS", new Vector2(dx, y + 170), 356, 25, Color.Cyan);
            for (int i = 0; i < Math.Min(hero.Spells.Count, 4); i++)
                Text(batch, font, hero.Spells[i], new Vector2(dx, y + 207 + i * 30), 356, 26, Color.White);
        }
    }
}
