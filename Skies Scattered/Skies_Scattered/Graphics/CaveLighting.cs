using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Skies_Scattered.Graphics;
public sealed class CaveLighting : IDisposable
{
    private Texture2D _gradient;
    public const int Radius = 148;
    public void Draw(SpriteBatch batch, GraphicsDevice graphics, Texture2D pixel, Vector2 center)
    {
        if (_gradient == null)
        {
            const int size = 256;
            _gradient = new Texture2D(graphics, size, size);
            var data = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x + .5f, y + .5f), new Vector2(128)) / 128f;
                    float fade = MathHelper.Clamp((distance - .52f) / .48f, 0, 1);
                    fade = fade * fade * (3 - 2 * fade);
                    byte alpha = (byte)(Math.Round(fade * 16) / 16 * 255);
                    data[y * size + x] = new Color(0, 0, 0, (int)alpha);
                }

            _gradient.SetData(data);
        }

        int width = graphics.Viewport.Width, height = graphics.Viewport.Height;
        int x0 = (int)Math.Round(center.X) - Radius, y0 = (int)Math.Round(center.Y) - Radius;
        int left = Math.Clamp(x0, 0, width), right = Math.Clamp(x0 + Radius * 2, 0, width);
        int top = Math.Clamp(y0, 0, height), bottom = Math.Clamp(y0 + Radius * 2, 0, height);
        if (top > 0)
            batch.Draw(pixel, new Rectangle(0, 0, width, top), Color.Black);
        if (bottom < height)
            batch.Draw(pixel, new Rectangle(0, bottom, width, height - bottom), Color.Black);
        if (left > 0)
            batch.Draw(pixel, new Rectangle(0, top, left, bottom - top), Color.Black);
        if (right < width)
            batch.Draw(pixel, new Rectangle(right, top, width - right, bottom - top), Color.Black);
        batch.Draw(_gradient, new Rectangle(x0, y0, Radius * 2, Radius * 2), Color.White);
    }

    public void Dispose()
    {
        _gradient?.Dispose();
        _gradient = null;
    }
}
