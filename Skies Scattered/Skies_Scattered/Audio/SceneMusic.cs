using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework.Media;

namespace Skies_Scattered.Audio;
public sealed class SceneMusic : IDisposable
{
    private readonly Dictionary<string, Song> _songs = new();
    private string _currentTrack;
    private string _requestedTrack;
    private float _gain;
    public SceneMusic()
    {
        foreach (string name in new[]
        {
            "Menu",
            "Cave",
            "Final",
            "GameOver"
        }

        )
        {
            string path = Path.Combine(AppContext.BaseDirectory, "Content", "Audio", "Scenes", name + ".ogg");
            _songs.Add(name, Song.FromUri(name, new Uri(path)));
        }
    }

    public void Select(string track)
    {
        _requestedTrack = track;
    }

    public void Update(float seconds)
    {
        float dt = Math.Max(0, seconds);
        if (_currentTrack != _requestedTrack)
        {
            _gain = Math.Max(0, _gain - dt / .25f);
            if (_gain <= 0)
            {
                MediaPlayer.Stop();
                _currentTrack = _requestedTrack;
                if (_currentTrack != null)
                {
                    MediaPlayer.Volume = 0;
                    MediaPlayer.IsRepeating = true;
                    MediaPlayer.Play(_songs[_currentTrack]);
                }
            }
        }
        else if (_currentTrack != null)
        {
            _gain = Math.Min(1f, _gain + dt / .5f);
        }

        MediaPlayer.Volume = Math.Clamp(BattleMusic.Volume * _gain, 0f, 1f);
    }

    public void Dispose()
    {
        MediaPlayer.Stop();
        foreach (Song song in _songs.Values)
            song.Dispose();
        _songs.Clear();
    }
}
