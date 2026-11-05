using Skies_Scattered.Audio;
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Skies_Scattered.UI;

public sealed class OverworldDialogue
{
    private string[] _pages = Array.Empty<string>();
    private int _page;
    public TypewriterText Message { get; } = new TypewriterText();
    public bool IsOpen { get; private set; }
    public bool IsCinematic { get; private set; }
    public void Show(bool cinematic, params string[] pages)
    {
        if (pages.Length == 0) return;
        _pages = pages; _page = 0; IsOpen = true; IsCinematic = cinematic;
        Message.Text = "";
        Message.Text = pages[0];
    }
    public void Update(float dt, bool advance, bool reveal = false)
    {
        if (!IsOpen) return;
        Message.Update(dt);
        if (reveal) { Message.Reveal(); return; }
        if (!advance || !Message.IsComplete) return;
        SoundEffects.Play(SoundCue.Select);
        _page++;
        if (_page >= _pages.Length) { Close(); return; }
        Message.Text = ""; Message.Text = _pages[_page];
    }
    public void Close() { IsOpen = false; IsCinematic = false; }
    public void Draw(SpriteBatch batch, Texture2D hud, Texture2D pixel, SpriteFont font, Vector2 screen)
    {
        if (!IsOpen) return;
        if (IsCinematic)
        {
            batch.Draw(pixel, new Rectangle(0,0,(int)screen.X,36), Color.Black);
            batch.Draw(pixel, new Rectangle(0,(int)screen.Y-36,(int)screen.X,36), Color.Black);
        }
        var box = new Rectangle(20,(int)screen.Y-124,(int)screen.X-40,104);
        EarthboundBox.Draw(batch,hud,pixel,box);
        // La línea se ajusta con el texto completo para no saltar al escribirse.
        float scale = Math.Min(1f, (box.Width-28)/560f);
        var visible = Message.Text.Substring(0,Message.VisibleCharacters);
        float lineHeight = font.LineSpacing * scale;
        Vector2 cursor = new Vector2(box.X+14,box.Y+14);
        int start = 0;
        foreach (string word in Message.Text.Split(' '))
        {
            if (cursor.X + font.MeasureString(word).X*scale > box.Right-14)
            { cursor.X=box.X+14; cursor.Y+=lineHeight; }
            for (int i=0;i<word.Length;i++)
            {
                if (start>=visible.Length) break;
                char letter=visible[start++];
                if (letter=='\n') {cursor.X=box.X+14;cursor.Y+=lineHeight;continue;}
                batch.DrawString(font,letter.ToString(),cursor,Color.White,0,Vector2.Zero,scale,SpriteEffects.None,0);
                cursor.X+=font.MeasureString(letter.ToString()).X*scale;
            }
            if (start<visible.Length && visible[start]==' ') {start++;cursor.X+=font.MeasureString(" ").X*scale;}
        }
        batch.DrawString(font,Message.IsComplete?"Z: Continuar":"X: Mostrar texto",new Vector2(box.X+14,box.Bottom-23),Color.LightGray,0,Vector2.Zero,.8f,SpriteEffects.None,0);
    }
}
