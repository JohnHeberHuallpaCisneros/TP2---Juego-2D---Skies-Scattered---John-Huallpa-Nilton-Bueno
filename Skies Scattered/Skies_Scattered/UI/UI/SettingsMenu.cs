using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameLibrary;
using Skies_Scattered.Audio;

namespace Skies_Scattered.UI;
public class SettingsMenu
{
    private int _selected;
    public bool IsOpen { get; private set; }
    public bool CrtEnabled { get; set; } = true;

    public void Open()
    {
        _selected = 0;
        IsOpen = true;
    }

    public void Close()
    {
        IsOpen = false;
    }

    public void Update()
    {
        if (!IsOpen)
            return;
        if (Input.IsKeyPressed(Keys.X) || Input.IsKeyPressed(Keys.Escape))
        {
            SoundEffects.Play(SoundCue.Select);
            Close();
            return;
        }

        int previous = _selected;
        if (Input.IsKeyPressed(Keys.Up))
            _selected = (_selected + 3) % 4;
        if (Input.IsKeyPressed(Keys.Down))
            _selected = (_selected + 1) % 4;
        if (previous != _selected)
            SoundEffects.Play(SoundCue.Move);
        int direction = 0;
        if (Input.IsKeyPressed(Keys.Left))
            direction = -1;
        if (Input.IsKeyPressed(Keys.Right))
            direction = 1;
        bool confirm = Input.IsKeyPressed(Keys.Z) || Input.IsKeyPressed(Keys.Enter);
        if (_selected == 0 && direction != 0)
        {
            float previousVolume = BattleMusic.Volume;
            BattleMusic.Volume = Math.Clamp((float)Math.Round(BattleMusic.Volume + direction * .1f, 1), 0f, 1f);
            if (previousVolume != BattleMusic.Volume)
                SoundEffects.Play(SoundCue.Move);
        }
        else if (_selected == 1 && direction != 0)
        {
            float previousVolume = SoundEffects.Volume;
            SoundEffects.Volume = Math.Clamp((float)Math.Round(SoundEffects.Volume + direction * .1f, 1), 0f, 1f);
            if (previousVolume != SoundEffects.Volume)
                SoundEffects.Play(SoundCue.Move);
        }
        else if (_selected == 2 && (direction != 0 || confirm))
        {
            CrtEnabled = !CrtEnabled;
            SoundEffects.Play(SoundCue.Select);
        }
        else if (_selected == 3 && confirm)
        {
            SoundEffects.Play(SoundCue.Select);
            Close();
        }
    }

    public void Draw(SpriteBatch batch, Texture2D pixel, Texture2D hud, SpriteFont font, Vector2 screen)
    {
        int width = Math.Min(480, (int)screen.X - 32);
        var box = new Rectangle(((int)screen.X - width) / 2, ((int)screen.Y - 350) / 2, width, 350);
        EarthboundBox.Draw(batch, hud, pixel, box);
        Write(batch, font, "OPCIONES", box.X + 24, box.Y + 22, Color.White);
        string[] labels =
        {
            $"Musica: {BattleMusic.Volume:P0}",
            $"Efectos: {SoundEffects.Volume:P0}",
            "Filtro CRT: " + (CrtEnabled ? "Activado" : "Desactivado"),
            "Volver"
        };
        for (int i = 0; i < labels.Length; i++)
            Write(batch, font, (i == _selected ? "> " : "  ") + labels[i], box.X + 24, box.Y + 74 + i * 40, Color.White);
        Write(batch, font, "Arriba/Abajo: Elegir   Izq/Der: Ajustar", box.X + 24, box.Y + 247, Color.LightGray, .8f);
        Write(batch, font, "Z: Confirmar   X: Volver", box.X + 24, box.Y + 270, Color.LightGray, .8f);
        Write(batch, font, "Juego: Flechas/WASD | Z: Accion | C: Menu", box.X + 24, box.Y + 310, Color.LightGray, .8f);
    }

    private static void Write(SpriteBatch batch, SpriteFont font, string text, int x, int y, Color color, float scale = 1f)
    {
        batch.DrawString(font, text, new Vector2(x, y), color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
    }
}
