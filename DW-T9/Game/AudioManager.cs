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
    /// WAV audio manager using relative paths under ./Assets/Audio.
    /// Set each .wav to: Build Action=None, Copy to Output Directory=Copy if newer.
    /// Windows-only playback is guarded; on other OSes this safely no-ops.
    /// </summary>
    public sealed class AudioManager : IDisposable
    {
        private readonly string _baseDir;
        private readonly Dictionary<SoundId, string> _map;
        private SoundPlayer? _musicPlayer;
        private SoundId? _currentMusic;

        // Flip to false later if you don't want SFX to block while playing
        public bool BlockSfxWhileDebugging { get; set; } = true;

        public AudioManager(string baseDir = "Assets/Audio")
        {
            _baseDir = baseDir;

            // TIP: keep names lowercase to avoid case-sensitive misses
            _map = new Dictionary<SoundId, string>
            {
                { SoundId.MenuTheme,   "menu_theme.wav" },
                { SoundId.GameLoop,    "game_loop.wav"  },
                { SoundId.Key,         "key.wav"        },          // <- was "Key.wav"
                { SoundId.DoorBedroom, "door_bedroom.wav" },
                { SoundId.CardGained,  "card_gained.wav" },
                { SoundId.Win,         "win.wav" },
                { SoundId.Jumpscare,   "jumpscare.wav" }
            };
        }

        public void PlayMusic(SoundId id)
        {
            if (!OperatingSystem.IsWindows()) return;
            var path = Resolve(id);
            if (path == null) return;

            if (_currentMusic.HasValue && _currentMusic.Value == id) return;

#pragma warning disable CA1416 // SoundPlayer is Windows-only
            try
            {
                StopMusic();
                _musicPlayer = new SoundPlayer(path);
                _musicPlayer.Load();       // sync load is safer here
                _musicPlayer.PlayLooping();
                _currentMusic = id;
            }
            catch { /* swallow */ }
#pragma warning restore CA1416
        }

        public void StopMusic()
        {
            if (!OperatingSystem.IsWindows()) return;
#pragma warning disable CA1416
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
#pragma warning restore CA1416
        }

        public void PlaySfx(SoundId id)
        {
            if (!OperatingSystem.IsWindows()) return;
            var path = Resolve(id);
            if (path == null) return;

#pragma warning disable CA1416
            try
            {
                using var s = new SoundPlayer(path);
                s.Load();
                if (BlockSfxWhileDebugging)
                    s.PlaySync();  // block briefly so you KNOW it played
                else
                    s.Play();      // async fire-and-forget
            }
            catch { /* swallow */ }
#pragma warning restore CA1416
        }

        private string? Resolve(SoundId id)
        {
            if (!_map.TryGetValue(id, out var file)) return null;
            var full = Path.Combine(_baseDir, file);
#if DEBUG
            // Helpful trace while wiring things up
            Console.WriteLine($"[Audio] {id} -> {full} (exists={File.Exists(full)})");
#endif
            return File.Exists(full) ? full : null;
        }

        public void Dispose() => StopMusic();
    }
}
