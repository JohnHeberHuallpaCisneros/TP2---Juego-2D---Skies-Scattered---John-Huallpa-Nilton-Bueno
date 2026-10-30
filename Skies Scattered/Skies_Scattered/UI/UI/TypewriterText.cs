using System;

namespace Skies_Scattered.UI;

public sealed class TypewriterText
{
    private string _text = "";
    private float _elapsed;
    private float _delay = 0.025f;
    public float CharactersPerSecond { get; set; } = 40f;
    public int VisibleCharacters { get; private set; }
    public bool IsComplete => VisibleCharacters >= _text.Length;
    public string Text
    {
        get => _text;
        set
        {
            value ??= "";
            if (_text == value) return;
            _text = value;
            VisibleCharacters = 0;
            _elapsed = 0;
            _delay = 1f / Math.Max(1f, CharactersPerSecond);
        }
    }

    public void Reveal() { VisibleCharacters = _text.Length; _elapsed = 0; }

    public void Update(float seconds)
    {
        if (IsComplete) return;
        _elapsed += Math.Max(0, seconds);
        while (!IsComplete && _elapsed >= _delay)
        {
            _elapsed -= _delay;
            char c = _text[VisibleCharacters++];
            _delay = 1f / Math.Max(1f, CharactersPerSecond);
            if (c == '.' || c == '!' || c == '?' || c == ',' || c == ':' || c == ';')
                _delay += 0.08f;
        }
    }
}
