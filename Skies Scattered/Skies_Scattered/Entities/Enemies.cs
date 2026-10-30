using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Skies_Scattered.Entities;

public class Enemy
{
    public string Name { get; set; }
    public Texture2D Texture { get; set; }
    public int HP { get; set; }
    public int MaxHP { get; set; }
    public int Attack { get; set; }
    public int Defense { get; set; }
    public Vector2 Position { get; set; }
    public bool Floats { get; set; }
}
