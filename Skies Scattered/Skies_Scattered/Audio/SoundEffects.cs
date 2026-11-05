using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;

namespace Skies_Scattered.Audio;

public enum SoundCue
{
    Defend, FreezeAlpha, FireBeta, FireGamma, FreezeOmega, Encounter, Damage, Escape,
    Item, StatGain, BasicAttack, DefenseDownOmega, Select, ShieldBeta, Text, Move
}

public static class SoundEffects
{
    private sealed class Clip
    {
        public SoundEffect Sound;
        public SoundEffectInstance Instance;
        public float Remaining;
        public float BaseVolume;
    }
    private static readonly Dictionary<SoundCue, Clip> _clips = new();
    public static float Volume { get; set; } = .6f;

    public static void Load(ContentManager content)
    {
        Unload();
        foreach (SoundCue cue in Enum.GetValues<SoundCue>())
        {
            SoundEffect sound = content.Load<SoundEffect>("Audio/Sfx/" + cue);
            _clips.Add(cue, new Clip { Sound = sound, Instance = sound.CreateInstance() });
        }
    }
    public static void Play(SoundCue cue, float pitch = 0f)
    {
        if (!_clips.TryGetValue(cue, out var clip)) return;
        pitch = MathHelper.Clamp(pitch, -1f, 1f);
        Start(clip, cue, pitch, (float)clip.Sound.Duration.TotalSeconds / (float)Math.Pow(2, pitch));
    }
    public static void PlaySpell(string spell, float seconds)
    {
        SoundCue cue;
        switch (spell)
        {
            case "FireBeta": cue = SoundCue.FireBeta; break;
            case "FireGamma": cue = SoundCue.FireGamma; break;
            case "FreezeAlpha": cue = SoundCue.FreezeAlpha; break;
            case "FreezeOmega": cue = SoundCue.FreezeOmega; break;
            case "DefenseDownOmega": cue = SoundCue.DefenseDownOmega; break;
            case "ShieldBeta": cue = SoundCue.ShieldBeta; break;
            default: return;
        }
        if (!_clips.TryGetValue(cue, out var clip)) return;
        float duration = (float)clip.Sound.Duration.TotalSeconds;
        if (seconds <= 0) seconds = duration;
        float pitch = MathHelper.Clamp((float)Math.Log2(duration / seconds), -1f, 1f);
        Start(clip, cue, pitch, seconds);
    }
    private static void Start(Clip clip, SoundCue cue, float pitch, float duration)
    {
        clip.Instance.Stop();
        clip.Instance.Pitch = pitch;
        clip.BaseVolume = cue == SoundCue.Text ? .3f : cue == SoundCue.Move ? .45f : 1f;
        clip.Instance.Volume = Volume * clip.BaseVolume;
        clip.Remaining = duration;
        clip.Instance.Play();
    }
    public static void Update(float seconds)
    {
        foreach (Clip clip in _clips.Values)
        {
            if (clip.Instance.State != SoundState.Playing) continue;
            clip.Remaining -= Math.Max(0, seconds);
            if (clip.Remaining <= 0) clip.Instance.Stop();
            else clip.Instance.Volume = Volume * clip.BaseVolume * Math.Min(1f, clip.Remaining / .025f);
        }
    }
    public static void Unload()
    {
        foreach (Clip clip in _clips.Values) clip.Instance.Dispose();
        _clips.Clear();
    }
}
