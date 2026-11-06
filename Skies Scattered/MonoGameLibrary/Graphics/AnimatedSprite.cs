
namespace MonoGameLibrary.Graphics;
public class AnimatedSprite : Sprite
{
    private Animation _animation;
    public Animation Animation
    {
        get => _animation;
        set
        {
            _animation = value;
            Region = _animation.Frames[0];
        }
    }



    public AnimatedSprite(Animation animation)
    {
        Animation = animation;
    }



    public void Reset()
    {
        Region = _animation.Frames[0];
    }
}
