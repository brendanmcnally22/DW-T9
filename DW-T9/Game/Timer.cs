using System;
using System.Diagnostics;

namespace DW_T9.Game
{
    public sealed class Timer
    {
        private readonly int _secondsTotal;
        private readonly Stopwatch _sw;

        public Timer(int secondsTotal)
        {
            _secondsTotal = secondsTotal;
            _sw = new Stopwatch();
        }

        public void Start() => _sw.Start();
        public void Pause() => _sw.Stop();
        public void Resume() => _sw.Start();
        public void Stop() => _sw.Stop();

        public int RemainingSeconds
        {
            get
            {
                int elapsed = (int)_sw.Elapsed.TotalSeconds;
                return Math.Max(0, _secondsTotal - elapsed);
            }
        }

        public string RemainingFormatted
        {
            get
            {
                int rem = RemainingSeconds;
                return $"{rem / 60:00}:{rem % 60:00}";
            }
        }

        public bool Expired => RemainingSeconds <= 0;
    }
}
