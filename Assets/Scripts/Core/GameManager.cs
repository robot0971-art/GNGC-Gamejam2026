using System.Collections;
using Gamejam2026.Gameplay;
using Gamejam2026.Presentation;
using UnityEngine;

namespace Gamejam2026.Core
{
    public class GameManager : Singleton<GameManager>
    {
        [Header("Config")]
        [SerializeField] private StageConfig[] stages;

        [Header("Systems")]
        [SerializeField] private EntrantDatabase entrantDatabase;
        [SerializeField] private WavePresenter wavePresenter;
        [SerializeField] private CameraDirector cameraDirector;
        [SerializeField] private ShootingController shootingController;
        [SerializeField] private HudView hudView;
        [SerializeField] private Gamejam2026.DebugTools.RightClickZoomDebugInput sniperZoomInput;
        [SerializeField] private Gamejam2026.Presentation.PlayerGunRecoil playerGunRecoil;
        [SerializeField] private PlayerMuzzleFlash playerMuzzleFlash;
        [SerializeField] private EffectSpawner effectSpawner;
        [SerializeField] private GameObject playerObject;

        [Header("Timing")]
        [SerializeField] private float overviewSeconds = 0.4f;
        [SerializeField] private float resolveDelaySeconds = 0.65f;
        [SerializeField] private float previewFadeSeconds = 0.18f;
        [SerializeField] private float judgementTimeSeconds = 15f;

        [Header("Shot Feedback")]
        [SerializeField] private float shotShakeDuration = 0.08f;
        [SerializeField] private float shotShakeStrength = 0.06f;
        [SerializeField] private float hitStopSeconds = 0.03f;

        private readonly PlayerState playerState = new PlayerState();
        private readonly CountdownTimer judgementTimer = new CountdownTimer();
        private readonly WaveResultEvaluator waveResultEvaluator = new WaveResultEvaluator();
        private GameState state;
        private int currentStageIndex;
        private int currentWaveIndex;
        private Coroutine hitStopRoutine;
        private Coroutine timerRoutine;
        private Coroutine resolveRoutine;
        private float storedTimeScale = 1f;
        private bool currentWavePenaltyApplied;
        private StageConfig CurrentStage => stages[currentStageIndex];

        protected override void Awake()
        {
            base.Awake();
            ResolveMissingReferences();
            playerState.MistakesChanged += HandleMistakesChanged;
            playerState.ScoreChanged += HandleScoreChanged;
        }

        private void OnEnable()
        {
            if (shootingController != null)
            {
                shootingController.ShotFired += HandleShotFired;
                shootingController.ShotResolved += HandleShotResolved;
                shootingController.BulletsEmpty += HandleBulletsEmpty;
            }
        }

        private void OnDisable()
        {
            playerState.MistakesChanged -= HandleMistakesChanged;
            playerState.ScoreChanged -= HandleScoreChanged;
            RestoreTimeScale();
            StopTimer();

            if (shootingController != null)
            {
                shootingController.ShotFired -= HandleShotFired;
                shootingController.ShotResolved -= HandleShotResolved;
                shootingController.BulletsEmpty -= HandleBulletsEmpty;
            }
        }

        private void Start()
        {
            StartGame();
        }

        public void StartGame()
        {
            if (!CanStart())
            {
                return;
            }

            playerState.Reset();
            currentStageIndex = 0;
            StartStage();
        }

        private bool CanStart()
        {
            ResolveMissingReferences();

            bool valid = stages.Length > 0 && entrantDatabase != null && entrantDatabase.HasEnoughData &&
                wavePresenter != null && cameraDirector != null && shootingController != null;

            if (!valid && hudView != null)
            {
                hudView.SetStatus("SETUP REQUIRED");
            }

            return valid;
        }

        private void ResolveMissingReferences()
        {
            if (stages == null || stages.Length == 0)
            {
                stages = new[]
                {
                    new StageConfig
                    {
                        stageName = "Preview",
                        waveCount = 1,
                        entrantsPerWave = 5,
                        minAiCount = 1,
                        maxAiCount = 3,
                        previewHoldSeconds = 1f,
                        previewPanSeconds = 0.8f,
                        maxMistakes = 3
                    }
                };
            }

            if (entrantDatabase == null)
            {
                entrantDatabase = FindFirstObjectByType<EntrantDatabase>();
            }

            if (wavePresenter == null)
            {
                wavePresenter = FindFirstObjectByType<WavePresenter>();
            }

            if (cameraDirector == null)
            {
                cameraDirector = FindFirstObjectByType<CameraDirector>();
            }

            if (shootingController == null)
            {
                shootingController = FindFirstObjectByType<ShootingController>();
            }

            if (playerGunRecoil == null)
            {
                playerGunRecoil = FindFirstObjectByType<Gamejam2026.Presentation.PlayerGunRecoil>();
            }

            if (playerMuzzleFlash == null)
            {
                playerMuzzleFlash = FindFirstObjectByType<PlayerMuzzleFlash>();
            }

            if (effectSpawner == null)
            {
                effectSpawner = FindFirstObjectByType<EffectSpawner>();
            }

            if (sniperZoomInput == null)
            {
                sniperZoomInput = FindFirstObjectByType<Gamejam2026.DebugTools.RightClickZoomDebugInput>();
            }

            if (playerObject == null)
            {
                GameObject foundPlayer = GameObject.Find("Player");

                if (foundPlayer != null)
                {
                    playerObject = foundPlayer;
                }
                else if (playerGunRecoil != null)
                {
                    playerObject = playerGunRecoil.gameObject;
                }
                else if (playerMuzzleFlash != null)
                {
                    playerObject = playerMuzzleFlash.gameObject;
                }
            }
        }

        private void StartStage()
        {
            state = GameState.StageIntro;
            currentWaveIndex = 0;
            hudView?.SetStage(CurrentStage.stageName);
            hudView?.SetMistakes(playerState.Mistakes, CurrentStage.maxMistakes);
            StartCoroutine(StartWaveRoutine());
        }

        private IEnumerator StartWaveRoutine()
        {
            state = GameState.WavePreview;
            currentWaveIndex++;
            currentWavePenaltyApplied = false;
            hudView?.SetWave(currentWaveIndex, CurrentStage.waveCount);
            hudView?.SetStatus("SCAN TARGETS");
            hudView?.HideClearPanel();
            hudView?.SetGameplayHudVisible(false);
            SetPlayerVisible(false);
            wavePresenter.HideAllEntrants();

            if (sniperZoomInput != null)
            {
                sniperZoomInput.HideZoomVisualsForIntro();
                yield return sniperZoomInput.PlayIntroScenesOnce();
                sniperZoomInput.ShowPersistentZoomVisualsOnly();
            }

            WaveData wave = entrantDatabase.BuildWave(CurrentStage, currentStageIndex + 1);
            wavePresenter.ShowWave(wave);
            Vector3 previewCenter = wavePresenter.GetCenter();
            wavePresenter.HideAllEntrants();

            yield return cameraDirector.ShowFocusedPoint(previewCenter, CurrentStage.previewPanSeconds);

            for (int i = 0; i < wavePresenter.Slots.Count; i++)
            {
                if (wavePresenter.Slots[i].Data == null)
                {
                    continue;
                }

                HandlePreviewFocusedSlotChanged(i);
                yield return wavePresenter.PreviewOneAtCenter(
                    i,
                    previewCenter,
                    previewFadeSeconds,
                    CurrentStage.previewHoldSeconds);
            }

            state = GameState.WaveOverview;
            wavePresenter.ShowAllEntrants();
            wavePresenter.FocusAll();
            SetPlayerVisible(false);

            state = GameState.Judging;
            hudView?.SetStatus("ELIMINATE AI");
            hudView?.SetBullets(wave.AiCount, wave.AiCount);
            hudView?.SetGameplayHudVisible(true);
            StartTimer(judgementTimeSeconds);
            sniperZoomInput?.ActivatePersistentZoomVisuals();
            yield return cameraDirector.ShowFocusedPoint(previewCenter, overviewSeconds);
            shootingController.Begin(wave.AiCount);
        }

        private void SetPlayerVisible(bool visible)
        {
            ResolveMissingReferences();

            if (playerObject != null)
            {
                playerObject.SetActive(visible);
            }
        }

        private void HandlePreviewFocusedSlotChanged(int slotIndex)
        {
            Debug.Log($"Preview focused slot {slotIndex + 1}");
            hudView?.SetStatus($"SCAN {slotIndex + 1}/5");
        }

        private void HandleShotFired(int bulletsLeft)
        {
            hudView?.SetBullets(bulletsLeft);
            StartCoroutine(cameraDirector.Shake(shotShakeDuration, shotShakeStrength));
            playerGunRecoil?.Play();
            playerMuzzleFlash?.Play();
        }

        private void HandleShotResolved(EntrantSlot slot, bool correct, int bulletsLeft)
        {
            PlayHitStop();

            if (correct)
            {
                playerState.AddScore(100);
                hudView?.SetStatus("AI DOWN");
                effectSpawner?.PlayElectricHit(slot.transform.position);

                if (wavePresenter.CountRemainingAI() <= 0)
                {
                    state = GameState.Resolving;
                    shootingController.Stop();
                    StopTimer();
                    hudView?.SetStatus("WAVE CLEAR");
                    hudView?.ShowClearPanel("CLEAR");
                }
            }
            else
            {
                playerState.AddMistake();
                currentWavePenaltyApplied = true;
                hudView?.SetStatus("HUMAN HIT");
            }
        }

        private void PlayHitStop()
        {
            if (hitStopSeconds <= 0f)
            {
                return;
            }

            if (hitStopRoutine != null)
            {
                StopCoroutine(hitStopRoutine);
                RestoreTimeScale();
            }

            hitStopRoutine = StartCoroutine(HitStopRoutine());
        }

        private IEnumerator HitStopRoutine()
        {
            storedTimeScale = Time.timeScale > 0f ? Time.timeScale : storedTimeScale;
            Time.timeScale = 0f;
            yield return new WaitForSecondsRealtime(hitStopSeconds);
            RestoreTimeScale();
            hitStopRoutine = null;
        }

        private void RestoreTimeScale()
        {
            Time.timeScale = storedTimeScale > 0f ? storedTimeScale : 1f;
        }

        private void HandleBulletsEmpty()
        {
            if (state != GameState.Judging)
            {
                return;
            }

            StartResolveRoutine(true);
        }

        private void StartResolveRoutine(bool countFailureMistake)
        {
            if (resolveRoutine != null)
            {
                return;
            }

            resolveRoutine = StartCoroutine(ResolveWaveRoutine(countFailureMistake));
        }

        private IEnumerator ResolveWaveRoutine(bool countFailureMistake)
        {
            state = GameState.Resolving;
            shootingController.Stop();
            StopTimer();

            yield return new WaitForSeconds(resolveDelaySeconds);

            WaveResult result = waveResultEvaluator.Evaluate(wavePresenter);
            bool success = result.IsSuccess;

            if (success)
            {
                playerState.AddScore(250);
                hudView?.SetStatus("WAVE CLEAR");
            }
            else
            {
                if (countFailureMistake && !currentWavePenaltyApplied)
                {
                    playerState.AddMistake();
                    currentWavePenaltyApplied = true;
                }

                hudView?.SetStatus("SECURITY BREACH");
            }

            if (playerState.Mistakes >= CurrentStage.maxMistakes)
            {
                state = GameState.GameOver;
                hudView?.SetStatus("GAME OVER");
                resolveRoutine = null;
                yield break;
            }

            yield return new WaitForSeconds(resolveDelaySeconds);

            if (currentWaveIndex < CurrentStage.waveCount)
            {
                resolveRoutine = null;
                StartCoroutine(StartWaveRoutine());
                yield break;
            }

            resolveRoutine = null;
            AdvanceStage();
        }

        private void AdvanceStage()
        {
            currentStageIndex++;

            if (currentStageIndex >= stages.Length)
            {
                state = GameState.GameClear;
                hudView?.SetStatus("MISSION COMPLETE");
                return;
            }

            state = GameState.StageClear;
            StartStage();
        }

        private void HandleMistakesChanged(int mistakes)
        {
            if (stages.Length > 0 && currentStageIndex < stages.Length)
            {
                hudView?.SetMistakes(mistakes, CurrentStage.maxMistakes);
            }
        }

        private void HandleScoreChanged(int score)
        {
            hudView?.SetScore(score);
        }

        private void StartTimer(float seconds)
        {
            StopTimer();
            hudView?.SetTimer(seconds);
            timerRoutine = StartCoroutine(judgementTimer.Run(
                seconds,
                IsJudging,
                remaining => hudView?.SetTimer(remaining),
                HandleJudgementTimerCompleted));
        }

        private void StopTimer()
        {
            if (timerRoutine != null)
            {
                StopCoroutine(timerRoutine);
                timerRoutine = null;
            }
        }

        private bool IsJudging()
        {
            return state == GameState.Judging;
        }

        private void HandleJudgementTimerCompleted()
        {
            timerRoutine = null;

            if (shootingController != null && shootingController.Bullets > 0)
            {
                HandleJudgementTimeExpired();
            }
        }

        private void HandleJudgementTimeExpired()
        {
            if (state != GameState.Judging)
            {
                return;
            }

            if (wavePresenter != null && wavePresenter.TryGetFirstUnshotHuman(out EntrantSlot humanSlot))
            {
                humanSlot.MarkShot();
            }

            playerState.AddMistake();
            currentWavePenaltyApplied = true;
            hudView?.SetStatus("TIME OUT");
            shootingController?.ClearOverrideTarget();
        }
    }
}
