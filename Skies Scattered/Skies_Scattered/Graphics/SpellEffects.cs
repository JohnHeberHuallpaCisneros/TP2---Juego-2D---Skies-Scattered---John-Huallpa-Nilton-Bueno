using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Skies_Scattered.Graphics;

// Assets normalizados: los originales de Resources permanecen intactos.
public sealed class SpellEffectLibrary : IDisposable
{
    public sealed class Frame
    {
        public string Texture { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public float Seconds { get; set; }
    }

    private readonly Dictionary<string, List<Frame>> _animations;
    private readonly Dictionary<string, Texture2D> _textures = new();

    public SpellEffectLibrary(GraphicsDevice graphics)
    {
        string folder = Path.Combine(AppContext.BaseDirectory, "Content", "SpellEffects");
        _animations = JsonSerializer.Deserialize<Dictionary<string, List<Frame>>>(
            File.ReadAllText(Path.Combine(folder, "animations.json")),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidDataException("No hay animaciones de hechizos.");
        try
        {
            foreach (var animation in _animations.Values)
            {
                if (animation.Count == 0) throw new InvalidDataException("Animación vacía.");
                foreach (var frame in animation)
                {
                    if (!_textures.ContainsKey(frame.Texture))
                    {
                        using var stream = File.OpenRead(Path.Combine(folder, frame.Texture));
                        _textures.Add(frame.Texture, Texture2D.FromStream(graphics, stream));
                    }
                    var texture = _textures[frame.Texture];
                    if (!float.IsFinite(frame.Seconds) || frame.Seconds <= 0 ||
                        frame.Width <= 0 || frame.Height <= 0 || frame.X < 0 || frame.Y < 0 ||
                        frame.X + frame.Width > texture.Width || frame.Y + frame.Height > texture.Height)
                        throw new InvalidDataException("Frame de hechizo inválido.");
                }
            }
        }
        catch { Dispose(); throw; }
    }

    internal List<Frame> GetFrames(string name) => _animations[name];
    internal Texture2D GetTexture(string name) => _textures[name];

    public void Dispose()
    {
        foreach (var texture in _textures.Values) texture.Dispose();
        _textures.Clear();
    }
}

public sealed class SpellEffectPlayer
{
    private readonly SpellEffectLibrary _library;
    private List<SpellEffectLibrary.Frame> _frames;
    private int _index;
    private float _elapsed;
    public bool IsPlaying => _frames != null && _index < _frames.Count;
    public float Opacity { get; set; } = 0.82f;

    public SpellEffectPlayer(SpellEffectLibrary library) { _library = library; }

    public void Play(string name)
    {
        _frames = _library.GetFrames(name);
        _index = 0;
        _elapsed = 0;
    }

    public void Stop()
    {
        _frames = null;
        _index = 0;
        _elapsed = 0;
    }

    public void Update(float seconds)
    {
        if (!IsPlaying) return;
        _elapsed += seconds;
        while (IsPlaying && _elapsed >= _frames[_index].Seconds)
        {
            _elapsed -= _frames[_index].Seconds;
            _index++;
        }
    }

    public void Draw(SpriteBatch batch, Rectangle area, Rectangle? clip = null)
    {
        if (!IsPlaying) return;
        var frame = _frames[_index];
        Rectangle visible = clip.HasValue ? Rectangle.Intersect(area, clip.Value) : area;
        if (visible.Width <= 0 || visible.Height <= 0) return;
        float sx = (float)frame.Width / area.Width;
        float sy = (float)frame.Height / area.Height;
        var source = new Rectangle(frame.X + (int)((visible.X - area.X) * sx),
            frame.Y + (int)((visible.Y - area.Y) * sy),
            Math.Max(1, (int)(visible.Width * sx)), Math.Max(1, (int)(visible.Height * sy)));
        batch.Draw(_library.GetTexture(frame.Texture), visible, source,
            Color.White * MathHelper.Clamp(Opacity, 0f, 1f));
    }
}
