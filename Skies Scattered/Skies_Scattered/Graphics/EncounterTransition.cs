using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Skies_Scattered.Graphics;
public sealed class EncounterTransition : IDisposable
{
    private readonly Texture2D _sheet;
    private float _elapsed;
    private float _alertDuration;
    public const int FrameCount = 22;
    public const float FrameSeconds = .05f;
    public const float RevealSeconds = .5f;
    public bool IsActive { get; private set; }
    public bool HasStartedSwirl => IsActive && _elapsed >= _alertDuration;
    public bool HasCoveredScreen => _elapsed >= _alertDuration + FrameCount * FrameSeconds;
    public int FrameIndex => Math.Clamp((int)((_elapsed - _alertDuration) / FrameSeconds), 0, FrameCount - 1);
    public float BlackOpacity => Math.Clamp(1f - (_elapsed - _alertDuration - FrameCount * FrameSeconds) / RevealSeconds, 0f, 1f);

    public EncounterTransition(GraphicsDevice graphics)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Content", "Transitions", "EncounterSwirls.png");
        using var stream = File.OpenRead(path);
        _sheet = Texture2D.FromStream(graphics, stream);
        var pixels = new Color[_sheet.Width * _sheet.Height];
        _sheet.GetData(pixels);
        for (int i = 0; i < pixels.Length; i++)
            if (pixels[i].R == 248 && pixels[i].G == 248 && pixels[i].B == 248)
                pixels[i] = Color.Transparent;
        _sheet.SetData(pixels);
    }

    public void Start(float alertDuration)
    {
        _elapsed = 0;
        _alertDuration = Math.Max(0, alertDuration);
        IsActive = true;
    }

    public void Update(float seconds, bool battleReady = true)
    {
        if (!IsActive)
            return;
        _elapsed += Math.Max(0, seconds);
        if (!battleReady)
            _elapsed = Math.Min(_elapsed, _alertDuration + FrameCount * FrameSeconds);
        if (_elapsed >= _alertDuration + FrameCount * FrameSeconds + RevealSeconds)
            IsActive = false;
    }

    public void Draw(SpriteBatch batch, Texture2D pixel, Rectangle screen)
    {
        if (!IsActive || _elapsed < _alertDuration)
            return;
        if (HasCoveredScreen)
        {
            batch.Draw(pixel, screen, Color.Black * BlackOpacity);
            return;
        }

        int frame = FrameIndex;
        var source = new Rectangle((frame % 5) * 264, 53 + (frame / 5) * 232, 256, 224);
        batch.Draw(_sheet, screen, source, Color.White * .7f);
        float closingOpacity = Math.Clamp((frame - (FrameCount - 5)) / 4f, 0f, 1f);
        batch.Draw(pixel, screen, Color.Black * closingOpacity);
    }

    public void Reset()
    {
        _elapsed = 0;
        _alertDuration = 0;
        IsActive = false;
    }

    public void Dispose() => _sheet.Dispose();
}
