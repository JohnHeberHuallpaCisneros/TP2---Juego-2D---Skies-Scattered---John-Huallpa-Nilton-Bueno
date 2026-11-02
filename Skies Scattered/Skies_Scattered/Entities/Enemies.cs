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
    public int Attack { get; set; }
    public int Defense { get; set; }
    public Vector2 Position { get; set; }
    public bool Floats { get; set; }
    private float _impactElapsed = 1f;
    private float _impactStrength;
    private const float ImpactDuration = 0.30f;
    public Vector2 ImpactOffset { get; private set; }
    public Color ImpactTint { get; private set; } = Color.White;
    public float ImpactFlash => _impactElapsed < 0.075f ? 0.55f * (1f - _impactElapsed / 0.075f) : 0f;

    public void TriggerImpact(int damage, Color? tint = null)
    {
        ImpactTint = tint ?? Color.White;
        _impactElapsed = 0f;
        _impactStrength = MathHelper.Clamp(4f + damage * 0.12f, 4f, 8f);
        ImpactOffset = new Vector2((float)Math.Round(_impactStrength), 0);
    }

    public void UpdateImpact(float seconds)
    {
        _impactElapsed += Math.Max(0, seconds);
        if (_impactElapsed >= ImpactDuration) { ImpactOffset = Vector2.Zero; return; }
        float fade = 1f - _impactElapsed / ImpactDuration;
        ImpactOffset = new Vector2((float)Math.Round(Math.Cos(_impactElapsed * 95f) * _impactStrength * fade), 0);
    }
}
