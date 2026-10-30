using System;
using Microsoft.Xna.Framework;

namespace Skies_Scattered.Graphics;

public sealed class BattleShake
{
    private float _elapsed, _duration, _strength;
    private bool _vertical, _diagonal;
    public Vector2 Offset { get; private set; }

    public void Trigger(string attack, float delay = 0f)
    {
        (_strength, _duration) = attack switch
        {
            "FireGamma" => (6f, .42f),
            "FireBeta" => (4f, .32f),
            "FreezeOmega" => (4f, .30f),
            "FreezeAlpha" => (3f, .24f),
            "ShieldBeta" => (1.5f, .28f),
            "DefenseDownOmega" => (2.5f, .35f),
            "EnemyBeam" => (2f, .20f),
            _ => (1.5f, .16f)
        };
        _vertical = attack.StartsWith("Freeze", StringComparison.Ordinal);
        _diagonal = attack == "FireGamma" || attack == "ShieldBeta";
        _elapsed = -Math.Max(0, delay);
        Offset = Vector2.Zero;
    }

    public void Update(float seconds)
    {
        _elapsed += Math.Max(0, seconds);
        if (_elapsed < 0 || _elapsed >= _duration || _duration <= 0)
        {
            Offset = Vector2.Zero;
            return;
        }
        float fade = 1f - _elapsed / _duration;
        float amount = _strength * fade;
        float primary = (float)Math.Sin(_elapsed * 95f) * amount;
        float secondary = _diagonal ? (float)Math.Sin(_elapsed * 127f) * amount * .65f : 0;
        Offset = _vertical
            ? new Vector2(0, (float)Math.Round(primary))
            : new Vector2((float)Math.Round(primary), (float)Math.Round(secondary));
    }
}
