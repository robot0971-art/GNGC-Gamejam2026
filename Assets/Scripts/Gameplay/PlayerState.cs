using System;

namespace Gamejam2026.Gameplay
{
    public class PlayerState
    {
        public event Action<int> MistakesChanged;
        public event Action<int> ScoreChanged;

        public int Mistakes { get; private set; }
        public int Score { get; private set; }

        public void Reset()
        {
            Mistakes = 0;
            Score = 0;
            MistakesChanged?.Invoke(Mistakes);
            ScoreChanged?.Invoke(Score);
        }

        public void AddMistake()
        {
            Mistakes++;
            MistakesChanged?.Invoke(Mistakes);
        }

        public void AddScore(int amount)
        {
            Score += amount;
            ScoreChanged?.Invoke(Score);
        }
    }
}
