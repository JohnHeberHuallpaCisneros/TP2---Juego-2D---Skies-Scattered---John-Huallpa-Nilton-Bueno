using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Skies_Scattered.UI;

public static class EarthboundBox
{
    public static void Draw(SpriteBatch spriteBatch, Texture2D texture, Texture2D pixel, Rectangle rect)
    {
        // Fallback si no hay textura del HUD
        if (texture == null)
        {
            if (pixel != null)
            {
                spriteBatch.Draw(pixel, rect, Color.White);
                spriteBatch.Draw(pixel,
                    new Rectangle(rect.X + 3, rect.Y + 3, rect.Width - 6, rect.Height - 6),
                    new Color(20, 20, 60));
            }
            return;
        }

        int atlasX = 976;
        int atlasY = 143;
        int width = 56;
        int height = 64;
        int c = 12; // tamaño de esquina

        Rectangle topLeftSrc = new Rectangle(atlasX, atlasY, c, c);
        Rectangle topRightSrc = new Rectangle(atlasX + width - c, atlasY, c, c);
        Rectangle bottomLeftSrc = new Rectangle(atlasX, atlasY + height - c, c, c);
        Rectangle bottomRightSrc = new Rectangle(atlasX + width - c, atlasY + height - c, c, c);

        Rectangle topSrc = new Rectangle(atlasX + c, atlasY, width - c * 2, c);
        Rectangle bottomSrc = new Rectangle(atlasX + c, atlasY + height - c, width - c * 2, c);
        Rectangle leftSrc = new Rectangle(atlasX, atlasY + c, c, height - c * 2);
        Rectangle rightSrc = new Rectangle(atlasX + width - c, atlasY + c, c, height - c * 2);
        Rectangle centerSrc = new Rectangle(atlasX + c, atlasY + c, width - c * 2, height - c * 2);

        spriteBatch.Draw(texture, new Rectangle(rect.X + c, rect.Y + c, rect.Width - c * 2, rect.Height - c * 2), centerSrc, Color.White);

        spriteBatch.Draw(texture, new Rectangle(rect.X, rect.Y, c, c), topLeftSrc, Color.White);
        spriteBatch.Draw(texture, new Rectangle(rect.Right - c, rect.Y, c, c), topRightSrc, Color.White);
        spriteBatch.Draw(texture, new Rectangle(rect.X, rect.Bottom - c, c, c), bottomLeftSrc, Color.White);
        spriteBatch.Draw(texture, new Rectangle(rect.Right - c, rect.Bottom - c, c, c), bottomRightSrc, Color.White);

        spriteBatch.Draw(texture, new Rectangle(rect.X + c, rect.Y, rect.Width - c * 2, c), topSrc, Color.White);
        spriteBatch.Draw(texture, new Rectangle(rect.X + c, rect.Bottom - c, rect.Width - c * 2, c), bottomSrc, Color.White);
        spriteBatch.Draw(texture, new Rectangle(rect.X, rect.Y + c, c, rect.Height - c * 2), leftSrc, Color.White);
        spriteBatch.Draw(texture, new Rectangle(rect.Right - c, rect.Y + c, c, rect.Height - c * 2), rightSrc, Color.White);
    }
}