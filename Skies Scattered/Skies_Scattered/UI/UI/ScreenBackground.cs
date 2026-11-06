using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Skies_Scattered.UI;
public static class ScreenBackground
{
    public static void Draw(SpriteBatch batch, Texture2D pixel, Texture2D texture, Rectangle screen, Color tint)
    {
        batch.Draw(pixel, screen, new Color(10, 10, 40));
        if (texture == null)
            return;
        float aspect = screen.Width / (float)screen.Height;
        int width = texture.Width;
        int height = texture.Height;
        if (width / (float)height > aspect)
            width = (int)(height * aspect);
        else
            height = (int)(width / aspect);
        var source = new Rectangle((texture.Width - width) / 2, (texture.Height - height) / 2, width, height);
        batch.Draw(texture, screen, source, tint);
        batch.Draw(pixel, screen, new Color(12, 12, 28) * .3f);
    }
}
