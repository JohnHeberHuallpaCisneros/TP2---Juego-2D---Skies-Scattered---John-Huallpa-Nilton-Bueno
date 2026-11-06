using System.Collections.Generic;

namespace MonoGameLibrary.Graphics;
public class Animation
{
    public List<TextureRegion> Frames { get; set; }



    public Animation(List<TextureRegion> frames)
    {
        Frames = frames;
    }
}
