using Microsoft.Xna.Framework;

namespace Skies_Scattered.Mechanics;
public class Camera2D
{
    public Vector2 Position { get; private set; }
    public float Smoothing { get; set; } = 1f;
    public Matrix Transform => Matrix.CreateTranslation(-(int)Position.X, -(int)Position.Y, 0f);

    public void Follow(Vector2 target, Vector2 viewSize, Vector2 worldSize, bool snap = false)
    {
        Vector2 desired = target - viewSize / 2f;
        desired.X = MathHelper.Clamp(desired.X, 0, System.Math.Max(0, worldSize.X - viewSize.X));
        desired.Y = MathHelper.Clamp(desired.Y, 0, System.Math.Max(0, worldSize.Y - viewSize.Y));
        Position = snap ? desired : Vector2.Lerp(Position, desired, Smoothing);
    }
}
