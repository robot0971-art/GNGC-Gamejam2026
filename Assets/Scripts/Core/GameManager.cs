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
        [SerializeField] private FailStoryCutscenePlayer failStoryCutscenePlayer;
        [SerializeField] private GameObject playerObject;

        [Header("Timing")]
        [SerializeField] private float overviewSeconds = 0.4f;
        [SerializeField] private float resolveDelaySeconds = 0.65f;
        [SerializeField] private float previewFadeSeconds = 0.18f;
        [SerializeField] private float stageTransitionFadeSeconds = 0.45f;
        [SerializeField] private float stagePanelFadeSeconds = 0.35f;
        [SerializeField] private float stagePanelHoldSeconds = 1.2f;
        [SerializeField] private float judgementTimeSeconds = 15f;

        [Header("Stage Transition")]
        [SerializeField] private CanvasGroup stageTransitionFadeGroup;

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
        private Coroutine stageTransitionRoutine;
        private float storedTimeScale = 1f;
        private bool currentWavePenaltyApplied;
        private bool waitingForStageContinue;
        private bool missionFailed;
        private int stageKilledAiCount;
        private int stageKilledHumanCount;
        private int stageHumanCount;
        private int totalClearPanelScore;
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

            if (hudView != null)
            {
                hudView.ClearPanelContinueRequested += HandleClearPanelContinueRequested;
                hudView.RestartRequested += HandleRestartRequested;
            }

            if (failStoryCutscenePlayer != null)
            {
                failStoryCutscenePlayer.RestartRequested += HandleFailStoryRestartRequested;
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

            if (hudView != null)
            {
                hudView.ClearPanelContinueRequested -= HandleClearPanelContinueRequested;
                hudView.RestartRequested -= HandleRestartRequested;
            }

            if (failStoryCutscenePlayer != null)
            {
                failStoryCutscenePlayer.RestartRequested -= HandleFailStoryRestartRequested;
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
            totalClearPanelScore = 0;
            currentStageIndex = 0;
            waitingForStageContinue = false;
            missionFailed = false;
            hudView?.HideGameOverPanel();
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
                    CreateDefaultStage(1),
                    CreateDefaultStage(2),
                    CreateDefaultStage(3),
                    CreateDefaultStage(4),
                    CreateDefaultStage(5)
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

            if (failStoryCutscenePlayer == null)
            {
                failStoryCutscenePlayer = FindFirstObjectByType<FailStoryCutscenePlayer>(FindObjectsInactive.Include);
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

            if (stageTransitionFadeGroup == null)
            {
                GameObject fadeObject = FindSceneObjectByName("Stage Transition Fade");

                if (fadeObject != null)
                {
                    stageTransitionFadeGroup = fadeObject.GetComponent<CanvasGroup>();
                }
            }
        }

        private static StageConfig CreateDefaultStage(int stageNumber)
        {
            return new StageConfig
            {
                stageName = $"Stage {stageNumber}",
                artRound = stageNumber,
                waveCount = 1,
                entrantsPerWave = 5,
                minAiCount = 1,
                maxAiCount = 3,
                previewHoldSeconds = 1f,
                previewPanSeconds = 0.8f,
                maxMistakes = 3
            };
        }

        private void StartStage()
        {
            state = GameState.StageIntro;
            currentWaveIndex = 0;
            stageKilledAiCount = 0;
            stageKilledHumanCount = 0;
            stageHumanCount = 0;
            waitingForStageContinue = false;
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
            hudView?.HideGameOverPanel();
            hudView?.HideStagePanel();
            hudView?.SetGameplayHudVisible(false);
            SetPlayerVisible(false);
            wavePresenter.HideAllEntrants();

            if (sniperZoomInput != null)
            {
                sniperZoomInput.HideZoomVisualsForIntro();
                if (currentStageIndex == 0 && currentWaveIndex == 1)
                {
                    yield return sniperZoomInput.PlayIntroScenesOnce();
                }
                sniperZoomInput.ShowPersistentZoomVisualsOnly();
                sniperZoomInput.ResetSniperVisualsToInitialPosition();
            }

            if (currentWaveIndex == 1 && hudView != null)
            {
                yield return hudView.PlayStagePanel(
                    currentStageIndex + 1,
                    stagePanelFadeSeconds,
                    stagePanelHoldSeconds);
            }

            WaveData wave = entrantDatabase.BuildWave(CurrentStage, currentStageIndex + 1);
            stageHumanCount += CountHumans(wave);
            wavePresenter.ShowWave(wave);
            Vector3 previewCenter = wavePresenter.GetCenter();
            wavePresenter.HideAllEntrants();
            cameraDirector.ResetToInitialView();

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

            if (sniperZoomInput != null && wavePresenter.TryGetFirstSelectableSlot(out EntrantSlot firstTargetSlot))
            {
                sniperZoomInput.ActivatePersistentZoomVisualsAt(firstTargetSlot);
            }
            else
            {
                sniperZoomInput?.ActivatePersistentZoomVisualsAt(previewCenter);
            }

            yield return new WaitForSeconds(overviewSeconds);
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

        private static GameObject FindSceneObjectByName(string objectName)
        {
            GameObject activeObject = GameObject.Find(objectName);

            if (activeObject != null)
            {
                return activeObject;
            }

            Transform[] transforms = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            for (int i = 0; i < transforms.Length; i++)
            {
                if (transforms[i].name == objectName)
                {
                    return transforms[i].gameObject;
                }
            }

            return null;
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

            if (bulletsLeft > 0 && slot != null)
            {
                sniperZoomInput?.SelectNearestTargetFrom(slot.transform.position);
            }

            if (correct)
            {
                stageKilledAiCount++;
                playerState.AddScore(100);
                hudView?.SetStatus("AI DOWN");
                effectSpawner?.PlayElectricHit(slot.transform.position);

                if (wavePresenter.CountRemainingAI() <= 0)
                {
                    StartResolveRoutine(false);
                }
            }
            else
            {
                stageKilledHumanCount++;
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
            bool success = result.RemainingAi == 0 && playerState.Mistakes < CurrentStage.maxMistakes;

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
                ShowFailedResultPanel();
                resolveRoutine = null;
                yield break;
            }

            if (!success)
            {
                ShowFailedResultPanel();
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
            int clearPanelScore = AddCurrentStageScoreToTotal();
            string rank = CalculateRank(false);
            currentStageIndex++;

            if (currentStageIndex >= stages.Length)
            {
                state = GameState.GameClear;
                hudView?.SetStatus("MISSION COMPLETE");
                hudView?.ShowClearPanel("MISSION COMPLETE", stageKilledAiCount, stageKilledHumanCount, clearPanelScore, rank, "Next");
                hudView?.SetClearPanelInteractable(false);
                return;
            }

            state = GameState.StageClear;
            waitingForStageContinue = true;
            wavePresenter?.HideAllEntrants();
            shootingController?.Stop();
            StopTimer();
            hudView?.SetGameplayHudVisible(false);
            hudView?.SetStatus("STAGE CLEAR");
            hudView?.ShowClearPanel("CLEAR", stageKilledAiCount, stageKilledHumanCount, clearPanelScore, rank, "Next");
            hudView?.SetClearPanelInteractable(true);
        }

        private void ShowFailedResultPanel()
        {
            state = GameState.GameOver;
            waitingForStageContinue = false;
            missionFailed = playerState.Mistakes >= CurrentStage.maxMistakes;
            shootingController?.Stop();
            StopTimer();
            wavePresenter?.HideAllEntrants();
            hudView?.SetGameplayHudVisible(false);
            hudView?.SetStatus(missionFailed ? "MISSION FAILED" : "FAILED");

            if (missionFailed && failStoryCutscenePlayer != null)
            {
                hudView?.HideClearPanel();
                StartCoroutine(PlayMissionFailedCutsceneRoutine());
                return;
            }

            hudView?.ShowClearPanel(
                missionFailed ? "MISSION FAILED" : "FAILED",
                stageKilledAiCount,
                stageKilledHumanCount,
                totalClearPanelScore,
                CalculateRank(true),
                missionFailed ? "End" : "Retry");
            hudView?.SetClearPanelInteractable(true);
        }

        private IEnumerator PlayMissionFailedCutsceneRoutine()
        {
            SetPlayerVisible(false);
            sniperZoomInput?.HideZoomVisualsForIntro();
            failStoryCutscenePlayer?.HideImmediate();

            if (failStoryCutscenePlayer != null)
            {
                yield return failStoryCutscenePlayer.Play();
            }
        }

        private int AddCurrentStageScoreToTotal()
        {
            totalClearPanelScore += CalculateStageClearPanelScore();
            return Mathf.Max(0, totalClearPanelScore);
        }

        private string CalculateRank(bool failed)
        {
            if (failed || stageKilledHumanCount >= 3)
            {
                return "C";
            }

            if (stageKilledHumanCount == 0)
            {
                return "S";
            }

            if (stageKilledHumanCount == 1)
            {
                return "A";
            }

            return "B";
        }

        private int CalculateStageClearPanelScore()
        {
            int currentRound = currentStageIndex + 1;
            int survivingHumans = Mathf.Max(0, stageHumanCount - stageKilledHumanCount);
            int score = currentRound * stageKilledAiCount * survivingHumans;
            return Mathf.Max(0, score);
        }

        private static int CountHumans(WaveData wave)
        {
            if (wave == null)
            {
                return 0;
            }

            int count = 0;

            for (int i = 0; i < wave.Entrants.Count; i++)
            {
                if (wave.Entrants[i] != null && wave.Entrants[i].type == EntrantType.Human)
                {
                    count++;
                }
            }

            return count;
        }

        private void HandleClearPanelContinueRequested()
        {
            if (state == GameState.GameOver && stageTransitionRoutine == null)
            {
                if (missionFailed)
                {
                    return;
                }

                stageTransitionRoutine = StartCoroutine(RestartStageRoutine());
                return;
            }

            if (!waitingForStageContinue || stageTransitionRoutine != null)
            {
                return;
            }

            stageTransitionRoutine = StartCoroutine(StageTransitionRoutine());
        }

        private void HandleRestartRequested()
        {
            if (state != GameState.GameOver || stageTransitionRoutine != null)
            {
                return;
            }

            if (missionFailed)
            {
                return;
            }

            stageTransitionRoutine = StartCoroutine(RestartStageRoutine());
        }

        private void HandleFailStoryRestartRequested()
        {
            if (stageTransitionRoutine != null)
            {
                return;
            }

            stageTransitionRoutine = StartCoroutine(RestartFromBeginningRoutine());
        }

        private IEnumerator StageTransitionRoutine()
        {
            hudView?.SetClearPanelInteractable(false);
            yield return FadeStageTransition(0f, 1f);
            hudView?.HideClearPanel();
            StartStage();
            yield return FadeStageTransition(1f, 0f);
            stageTransitionRoutine = null;
        }

        private IEnumerator RestartStageRoutine()
        {
            yield return FadeStageTransition(0f, 1f);
            hudView?.HideGameOverPanel();
            wavePresenter?.HideAllEntrants();
            shootingController?.Stop();
            StopTimer();
            RestoreTimeScale();
            hudView?.SetMistakes(playerState.Mistakes, CurrentStage.maxMistakes);
            StartStage();
            yield return FadeStageTransition(1f, 0f);
            stageTransitionRoutine = null;
        }

        private IEnumerator RestartFromBeginningRoutine()
        {
            yield return FadeStageTransition(0f, 1f);
            failStoryCutscenePlayer?.HideImmediate();
            hudView?.HideClearPanel();
            hudView?.HideGameOverPanel();
            wavePresenter?.HideAllEntrants();
            shootingController?.Stop();
            StopTimer();
            RestoreTimeScale();
            sniperZoomInput?.ResetIntroPlayback();
            StartGame();
            yield return FadeStageTransition(1f, 0f);
            stageTransitionRoutine = null;
        }

        private IEnumerator FadeStageTransition(float from, float to)
        {
            if (stageTransitionFadeGroup == null || stageTransitionFadeSeconds <= 0f)
            {
                yield break;
            }

            stageTransitionFadeGroup.gameObject.SetActive(true);
            stageTransitionFadeGroup.blocksRaycasts = true;
            stageTransitionFadeGroup.interactable = true;
            float time = 0f;

            while (time < stageTransitionFadeSeconds)
            {
                time += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(time / stageTransitionFadeSeconds);
                stageTransitionFadeGroup.alpha = Mathf.Lerp(from, to, t);
                yield return null;
            }

            stageTransitionFadeGroup.alpha = to;

            if (Mathf.Approximately(to, 0f))
            {
                stageTransitionFadeGroup.blocksRaycasts = false;
                stageTransitionFadeGroup.interactable = false;
                stageTransitionFadeGroup.gameObject.SetActive(false);
            }
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
                stageKilledHumanCount++;
            }

            playerState.AddMistake();
            currentWavePenaltyApplied = true;
            hudView?.SetStatus("TIME OUT");
            shootingController?.ClearOverrideTarget();
        }

        private void EnterGameOver()
        {
            state = GameState.GameOver;
            missionFailed = playerState.Mistakes >= CurrentStage.maxMistakes;
            shootingController?.Stop();
            StopTimer();
            wavePresenter?.HideAllEntrants();
            hudView?.SetGameplayHudVisible(false);
            hudView?.SetStatus(missionFailed ? "MISSION FAILED" : "FAILED");
            hudView?.ShowClearPanel(
                missionFailed ? "MISSION FAILED" : "FAILED",
                stageKilledAiCount,
                stageKilledHumanCount,
                totalClearPanelScore,
                CalculateRank(true),
                missionFailed ? "End" : "Retry");
            hudView?.SetClearPanelInteractable(true);
        }
    }
}
