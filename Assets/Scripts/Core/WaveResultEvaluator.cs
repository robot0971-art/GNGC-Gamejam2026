using Gamejam2026.Gameplay;
using Gamejam2026.Presentation;

namespace Gamejam2026.Core
{
    public sealed class WaveResultEvaluator
    {
        public WaveResult Evaluate(WavePresenter wavePresenter)
        {
            if (wavePresenter == null)
            {
                return new WaveResult(0, 0);
            }

            return new WaveResult(
                wavePresenter.CountRemainingAI(),
                wavePresenter.CountShotHumans());
        }
    }
}
