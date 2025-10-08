using System;
using System.Collections.Generic;
using System.IO;
using System.Media;


namespace DW_T9.Game
{
    public enum SoundId
    {
        MenuTheme,
        GameLoop,
        Key,
        DoorBedroom,
        CardGained,
        Win,
        Jumpscare
    }

    /// <summary>
    /// Simple WAV audio manager using relative paths. 
    /// Put your audio files under ./Assets/Audio and set each to:
    ///   Build Action: None
    ///   Copy to Output Directory: Copy if newer
    /// </summary>
    public sealed class AudioManager : IDisposable
    {
        private readonly string _baseDir;
        private readonly Dictionary<SoundId, string> _map;
        private SoundPlayer? _musicPlayer;
        private SoundId? _currentMusic;

        public AudioManager(string baseDir = "Assets/Audio")
        {
            _baseDir = baseDir;

            // Map sound IDs to filenames (you can rename these to whatever you commit)
            _map = new Dictionary<SoundId, string>
            {
                { SoundId.MenuTheme,   "menu_theme.wav" },
                { SoundId.GameLoop,    "game_loop.wav"  },
                { SoundId.Key,         "Key.wav"        },
                { SoundId.DoorBedroom, "door_bedroom.wav" },
                { SoundId.CardGained,  "card_gained.wav" },
                { SoundId.Win,         "win.wav" },
                { SoundId.Jumpscare,   "jumpscare.wav" }
            };
        }

        public void PlayMusic(SoundId id)
        {
            try
            {
                var path = Resolve(id);
                if (path == null) return;

                // If the requested music is already playing, ignore
                if (_currentMusic.HasValue && _currentMusic.Value.Equals(id)) return;

                StopMusic();
                _musicPlayer = new SoundPlayer(path);
                _musicPlayer.LoadAsync();
                _musicPlayer.PlayLooping();
                _currentMusic = id;
            }
            catch { /* no-op: silent fail if not supported */ }
        }

        public void StopMusic()
        {
            try
            {
                _musicPlayer?.Stop();
                _musicPlayer?.Dispose();
            }
            catch { }
            finally
            {
                _musicPlayer = null;
                _currentMusic = null;
            }
        }

        public void PlaySfx(SoundId id)
        {
            try
            {
                var path = Resolve(id);
                if (path == null) return;
                using var s = new SoundPlayer(path);
                s.Play(); // fire-and-forget
            }
            catch { /* no-op */ }
        }

        private string? Resolve(SoundId id)
        {
            if (!_map.TryGetValue(id, out var file)) return null;
            var full = Path.Combine(_baseDir, file);
            return File.Exists(full) ? full : null;
        }

        public void Dispose()
        {
            StopMusic();
        }
    }
}
