using System;
using Microsoft.Xna.Framework;

namespace Skies_Scattered.Graphics;
public sealed class BattleShake
{
    private float _elapsed, _duration, _strength;
    private bool _vertical, _diagonal;
    public Vector2 Offset { get; private set; }

    public void Trigger(string attack, float delay = 0f, float intensity = 1f)
    {
        switch (attack)
        {
            case "FireGamma":
                _strength = 6f;
                _duration = .42f;
                break;
            case "FireBeta":
                _strength = 4f;
                _duration = .32f;
                break;
            case "FreezeOmega":
                _strength = 4f;
                _duration = .30f;
                break;
            case "FreezeAlpha":
                _strength = 3f;
                _duration = .24f;
                break;
            case "ShieldBeta":
                _strength = 1.5f;
                _duration = .28f;
                break;
            case "DefenseDownOmega":
                _strength = 2.5f;
                _duration = .35f;
                break;
            case "EnemyBeam":
                _strength = 2f;
                _duration = .20f;
                break;
            default:
                _strength = 1.5f;
                _duration = .16f;
                break;
        }

        _strength *= MathHelper.Clamp(intensity, 0f, 2f);
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
        Offset = _vertical ? new Vector2(0, (float)Math.Round(primary)) : new Vector2((float)Math.Round(primary), (float)Math.Round(secondary));
    }
}
