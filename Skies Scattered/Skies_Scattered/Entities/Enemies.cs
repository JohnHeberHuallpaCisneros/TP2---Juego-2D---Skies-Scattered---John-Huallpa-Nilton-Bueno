using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Skies_Scattered.Entities;
public class Enemy
{
    public string Name { get; set; }
    public Texture2D Texture { get; set; }
    public int HP { get; set; }
    public int MaxHP { get; set; }
    public int Attack { get; set; } = 8;
    public int Defense { get; set; }
    public Vector2 Position { get; set; }
    public bool Floats { get; set; }

    private float _impactElapsed = 1f;
    private float _impactStrength;
    private float _impactDuration = .30f;
    private bool _impactVertical, _impactDiagonal;
    public Vector2 ImpactOffset { get; private set; }
    public Color ImpactTint { get; private set; } = Color.White;
    public float ImpactFlash => _impactElapsed < 0.075f ? 0.55f * (1f - _impactElapsed / 0.075f) : 0f;

    public void TriggerImpact(int damage, Color? tint = null, string attack = "BasicAttack")
    {
        ImpactTint = tint ?? Color.White;
        _impactElapsed = 0f;
        float weight = attack switch
        {
            "FireGamma" => 1.25f,
            "FreezeOmega" => 1.1f,
            "DefenseDownOmega" => .55f,
            _ => 1f
        };
        _impactDuration = attack switch
        {
            "FireGamma" => .42f,
            "FireBeta" => .32f,
            "FreezeOmega" => .30f,
            "FreezeAlpha" => .24f,
            "DefenseDownOmega" => .35f,
            _ => .30f
        };
        _impactVertical = attack.StartsWith("Freeze", StringComparison.Ordinal);
        _impactDiagonal = attack == "FireGamma";
        _impactStrength = MathHelper.Clamp((4f + Math.Max(0, damage) * .12f) * weight, 2f, 10f);
        ImpactOffset = _impactVertical ? new Vector2(0, (float)Math.Round(_impactStrength)) : new Vector2((float)Math.Round(_impactStrength), 0);
    }

    public void UpdateImpact(float seconds)
    {
        _impactElapsed += Math.Max(0, seconds);
        if (_impactElapsed >= _impactDuration)
        {
            ImpactOffset = Vector2.Zero;
            return;
        }

        float fade = 1f - _impactElapsed / _impactDuration;
        float primary = (float)Math.Round(Math.Cos(_impactElapsed * 95f) * _impactStrength * fade);
        float secondary = _impactDiagonal ? (float)Math.Round(Math.Sin(_impactElapsed * 127f) * _impactStrength * fade * .65f) : 0;
        ImpactOffset = _impactVertical ? new Vector2(0, primary) : new Vector2(primary, secondary);
    }
}
