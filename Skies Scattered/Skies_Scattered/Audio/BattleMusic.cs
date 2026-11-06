using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

namespace Skies_Scattered.Audio;
public sealed class BattleMusic : IDisposable
{
    private readonly SoundEffectInstance _instance;
    public static float Volume { get; set; } = .4f;

    private float _fadeRemaining;
    private float _fadeDuration;
    private float _introVolume = 1f;
    private float _introSpeed;
    public bool IsFading => _fadeRemaining > 0;
    public float PlaybackRate { get; private set; } = 1f;
    public bool IsPlaying => _instance.State == SoundState.Playing;

    public BattleMusic(SoundEffect sound)
    {
        _instance = sound.CreateInstance();
        _instance.IsLooped = true;
        _instance.Volume = Volume;
    }

    public static float RateForPopulation(int remaining)
    {
        if (remaining > 5)
            return 1f;
        return .70f + Math.Clamp(remaining, 0, 5) * .05f;
    }

    public void Start(int remaining, float initialVolume = 1f)
    {
        Stop();
        _introVolume = Math.Clamp(initialVolume, 0f, 1f);
        _instance.Volume = Volume * _introVolume;
        PlaybackRate = RateForPopulation(remaining);
        _instance.Pitch = (float)Math.Log2(PlaybackRate);
        _instance.Play();
    }

    public void Update(float seconds, int remaining)
    {
        if (!IsPlaying)
            return;
        if (IsFading)
        {
            _fadeRemaining = Math.Max(0, _fadeRemaining - Math.Max(0, seconds));
            _instance.Volume = Volume * _introVolume * (_fadeRemaining / _fadeDuration);
            if (_fadeRemaining <= 0)
                Stop();
            return;
        }

        _introVolume = Math.Min(1f, _introVolume + _introSpeed * Math.Max(0, seconds));
        _instance.Volume = Volume * _introVolume;
        float target = RateForPopulation(remaining);
        float amount = Math.Max(0, seconds) * .12f;
        PlaybackRate += MathHelper.Clamp(target - PlaybackRate, -amount, amount);
        _instance.Pitch = (float)Math.Log2(PlaybackRate);
    }

    public void FadeIn(float seconds = .5f)
    {
        if (seconds <= 0)
        {
            _introVolume = 1f;
            return;
        }

        _introSpeed = (1f - _introVolume) / seconds;
    }

    public void FadeOut(float seconds = 1.2f)
    {
        if (!IsPlaying || IsFading)
            return;
        if (seconds <= 0)
        {
            Stop();
            return;
        }

        _fadeDuration = _fadeRemaining = seconds;
    }

    public void Stop()
    {
        _fadeRemaining = 0;
        _introSpeed = 0;
        _instance.Stop();
    }

    public void Dispose() => _instance.Dispose();
}
