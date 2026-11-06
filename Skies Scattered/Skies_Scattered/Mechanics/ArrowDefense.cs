using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Skies_Scattered.Mechanics;
public sealed class ArrowDefense : IDisposable
{
    public const int Length = 6;
    public int[] Directions { get; } = new int[Length];
    public int Correct { get; private set; }
    public int FailedIndex { get; private set; } = -1;
    public bool Finished { get; private set; }
    public bool Done => Finished && _ending >= .9f;
    public int RecoveredPP => Correct == Length ? 10 : Correct;
    public float DamageMultiplier => Correct == 0 ? 1.25f : 1f - Correct * .05f;

    private float _time, _ending, _ready = .55f;
    private Texture2D _texture;
    public ArrowDefense(Random random)
    {
        for (int i = 0; i < Length; i++)
            Directions[i] = random.Next(4);
    }

    public void Update(float dt, int direction = -1)
    {
        if (Finished)
        {
            _ending += dt;
            return;
        }

        if (_ready > 0)
        {
            _ready -= dt;
            return;
        }

        _time += dt;
        if (direction >= 0)
        {
            if (direction == Directions[Correct])
            {
                Correct++;
                _time = 0;
                if (Correct == Length)
                    Finished = true;
            }
            else
            {
                FailedIndex = Correct;
                Finished = true;
            }
        }
        else if (_time >= 1.6f)
        {
            FailedIndex = Correct;
            Finished = true;
        }
    }

    public static int ReadDirection()
    {
        int direction = -1, count = 0;
        Keys[] keys =
        {
            Keys.Right,
            Keys.Down,
            Keys.Left,
            Keys.Up
        };
        for (int i = 0; i < keys.Length; i++)
            if (MonoGameLibrary.Input.IsKeyPressed(keys[i]))
            {
                direction = i;
                count++;
            }

        return count > 1 ? 4 : direction;
    }

    public void Draw(SpriteBatch batch, GraphicsDevice graphics, Rectangle arena)
    {
        if (_texture == null)
        {
            using var stream = File.OpenRead(Path.Combine(AppContext.BaseDirectory, "Content", "Defense", "arrow.png"));
            using var original = Texture2D.FromStream(graphics, stream);
            var data = new Color[original.Width * original.Height];
            original.GetData(data);
            for (int i = 0; i < data.Length; i++)
            {
                byte alpha = (byte)(Math.Max(data[i].R, Math.Max(data[i].G, data[i].B)) * data[i].A / 255);
                data[i] = new Color(alpha, alpha, alpha, alpha);
            }

            _texture = new Texture2D(graphics, original.Width, original.Height);
            _texture.SetData(data);
        }

        int size = Math.Min(48, Math.Max(24, (arena.Width - 40) / 9));
        size = Math.Max(12, size / 12 * 12);
        int gap = size / 3, total = Length * size + (Length - 1) * gap;
        int left = arena.Center.X - total / 2;
        float fade = Finished ? MathHelper.Clamp((_ending - .35f) / .55f, 0, 1) : 0;
        for (int i = 0; i < Length; i++)
        {
            Color tint = i == FailedIndex ? new Color(255, 65, 65) : i < Correct ? Color.White : new Color(125, 125, 125);
            Vector2 center = new Vector2(left + i * (size + gap) + size / 2, arena.Center.Y - (i < Correct ? 7 : 0));
            int bands = Math.Min(16, _texture.Height);
            for (int band = 0; band < bands; band++)
            {
                if (fade > ((band * 7 % bands) + 1) / (float)bands)
                    continue;
                int y = band * _texture.Height / bands, end = (band + 1) * _texture.Height / bands;
                batch.Draw(_texture, center, new Rectangle(0, y, _texture.Width, end - y), tint, Directions[i] * MathHelper.PiOver2, new Vector2(_texture.Width / 2f, _texture.Height / 2f - y), size / (float)_texture.Width, SpriteEffects.None, 0);
            }
        }
    }

    public void Dispose()
    {
        _texture?.Dispose();
        _texture = null;
    }
}
