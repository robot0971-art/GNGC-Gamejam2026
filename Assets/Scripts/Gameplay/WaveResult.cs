namespace Gamejam2026.Gameplay
{
    public sealed class WaveResult
    {
        public WaveResult(int remainingAi, int shotHumans)
        {
            RemainingAi = remainingAi;
            ShotHumans = shotHumans;
        }

        public int RemainingAi { get; }
        public int ShotHumans { get; }
        public bool IsSuccess => RemainingAi == 0 && ShotHumans == 0;
    }
}
