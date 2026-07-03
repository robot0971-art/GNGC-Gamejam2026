using System;

namespace Gamejam2026.Gameplay
{
    [Serializable]
    public class StageConfig
    {
        public string stageName = "Stage";
        public int artRound;
        public int waveCount = 2;
        public int entrantsPerWave = 5;
        public int minAiCount = 1;
        public int maxAiCount = 3;
        public float previewHoldSeconds = 0.35f;
        public float previewPanSeconds = 0.25f;
        public int maxMistakes = 3;
    }
}
