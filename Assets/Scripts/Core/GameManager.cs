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
        [SerializeField] private DroneRewardEvent droneRewardEvent;
        [SerializeField] private Gamejam2026.DebugTools.RightClickZoomDebugInput sniperZoomInput;
        [SerializeField] private Gamejam2026.Presentation.PlayerGunRecoil playerGunRecoil;
        [SerializeField] private PlayerMuzzleFlash playerMuzzleFlash;
        [SerializeField] private EffectSpawner effectSpawner;
        [SerializeField] private FailStoryCutscenePlayer failStoryCutscenePlayer;
        [SerializeField] private SuccessStoryCutscenePlayer successStoryCutscenePlayer;
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
        private int failedStageIndex = -1;
        private int stageKilledAiCount;
        private int stageKilledHumanCount;
        private int stageHumanCount;
        private int totalClearPanelScore;
        private int nextStageBonusSeconds;
        private int nextStageBonusBullets;
        private bool nextStageDowngradeOneAI;
        private bool nextStageAutoPassHuman;
        private bool nextStageBlankBullet;
        private bool nextStageTheft;
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
            ClearPersistentRewardEffects();
            currentStageIndex = 0;
            failedStageIndex = -1;
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

            if (droneRewardEvent == null)
            {
                droneRewardEvent = FindFirstObjectByType<DroneRewardEvent>(FindObjectsInactive.Include);
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

            if (successStoryCutscenePlayer == null)
            {
                successStoryCutscenePlayer = FindFirstObjectByType<SuccessStoryCutscenePlayer>(FindObjectsInactive.Include);
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
                sniperZoomInput.ResetSniperVisualsToInitialPosition();
                if (currentStageIndex == 0 && currentWaveIndex == 1)
                {
                    yield return sniperZoomInput.PlayIntroScenesOnce();
                }
            }

            if (currentWaveIndex == 1 && hudView != null)
            {
                yield return hudView.PlayStagePanel(
                    currentStageIndex + 1,
                    stagePanelFadeSeconds,
                    stagePanelHoldSeconds);
            }

            if (sniperZoomInput != null)
            {
                sniperZoomInput.ShowPersistentZoomVisualsOnly();
                sniperZoomInput.ResetSniperVisualsToInitialPosition();
            }

            int currentStageNumber = currentStageIndex + 1;
            int bonusSeconds = nextStageBonusSeconds;
            int bonusBullets = nextStageBonusBullets;
            bool downgradeOneAI = nextStageDowngradeOneAI;
            bool autoPassHuman = nextStageAutoPassHuman;
            bool theft = nextStageTheft;
            int autoPassedHumanCount = 0;

            WaveData wave = entrantDatabase.BuildWave(CurrentStage, currentStageNumber);

            if (downgradeOneAI)
            {
                entrantDatabase.TryDowngradeOneAI(wave, currentStageNumber);
            }

            if (autoPassHuman)
            {
                autoPassedHumanCount = RemoveRandomEntrant(wave, EntrantType.Human) ? 1 : 0;
            }

            if (theft && wave.AiCount >= 2)
            {
                RemoveRandomEntrant(wave, EntrantType.AI);
                Debug.Log($"Theft reward applied: AI count {wave.AiCount + 1} -> {wave.AiCount}, bullets will follow current AI count plus bonuses.");
            }
            else if (theft)
            {
                Debug.Log($"Theft reward skipped: AI count is {wave.AiCount}, needs at least 2.");
            }

            stageHumanCount += CountHumans(wave) + autoPassedHumanCount;
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
            int bulletCount = Mathf.Max(0, wave.AiCount + bonusBullets);
            LogFinalWaveAfterRewards(wave, bulletCount);
            hudView?.SetBullets(bulletCount, bulletCount);
            hudView?.SetGameplayHudVisible(true);
            StartTimer(judgementTimeSeconds + bonusSeconds);

            if (nextStageBlankBullet)
            {
                shootingController.EnableBlankNextHumanShot();
            }

            if (sniperZoomInput != null && wavePresenter.TryGetFirstSelectableSlot(out EntrantSlot firstTargetSlot))
            {
                sniperZoomInput.ActivatePersistentZoomVisualsAt(firstTargetSlot);
            }
            else
            {
                sniperZoomInput?.ActivatePersistentZoomVisualsAt(previewCenter);
            }

            yield return new WaitForSeconds(overviewSeconds);
            shootingController.Begin(bulletCount);
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

                if (IsOutOfHearts())
                {
                    ShowFailedResultPanel();
                }
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
                ShowSuccessStoryCutscene(clearPanelScore, rank);
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
            failedStageIndex = currentStageIndex;
            shootingController?.Stop();
            StopTimer();
            wavePresenter?.HideAllEntrants();
            sniperZoomInput?.HideZoomVisualsForIntro();
            sniperZoomInput?.ResetSniperVisualsToInitialPosition();
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

        private void ShowSuccessStoryCutscene(int clearPanelScore, string rank)
        {
            waitingForStageContinue = false;
            wavePresenter?.HideAllEntrants();
            shootingController?.Stop();
            StopTimer();
            hudView?.HideClearPanel();
            hudView?.SetGameplayHudVisible(false);
            hudView?.SetStatus("MISSION COMPLETE");
            SetPlayerVisible(false);
            sniperZoomInput?.HideZoomVisualsForIntro();

            if (successStoryCutscenePlayer != null)
            {
                StartCoroutine(PlaySuccessStoryCutsceneRoutine());
                return;
            }

            hudView?.ShowClearPanel("MISSION COMPLETE", stageKilledAiCount, stageKilledHumanCount, clearPanelScore, rank, "Next");
            hudView?.SetClearPanelInteractable(false);
        }

        private IEnumerator PlaySuccessStoryCutsceneRoutine()
        {
            successStoryCutscenePlayer?.HideImmediate();

            if (successStoryCutscenePlayer != null)
            {
                yield return successStoryCutscenePlayer.Play();
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

        private static bool RemoveRandomEntrant(WaveData wave, EntrantType type)
        {
            if (wave == null)
            {
                return false;
            }

            int matchingCount = 0;

            for (int i = 0; i < wave.Entrants.Count; i++)
            {
                if (wave.Entrants[i] != null && wave.Entrants[i].type == type)
                {
                    matchingCount++;
                }
            }

            if (matchingCount == 0)
            {
                return false;
            }

            int selectedMatch = Random.Range(0, matchingCount);

            for (int i = 0; i < wave.Entrants.Count; i++)
            {
                if (wave.Entrants[i] == null || wave.Entrants[i].type != type)
                {
                    continue;
                }

                if (selectedMatch == 0)
                {
                    wave.Entrants.RemoveAt(i);
                    return true;
                }

                selectedMatch--;
            }

            return false;
        }

        private static void LogFinalWaveAfterRewards(WaveData wave, int bulletCount)
        {
            if (wave == null)
            {
                return;
            }

            int humanCount = CountHumans(wave);
            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            builder.Append($"Final wave after rewards: humans={humanCount}, ai={wave.AiCount}, bullets={bulletCount}: ");

            for (int i = 0; i < wave.Entrants.Count; i++)
            {
                EntrantData entrant = wave.Entrants[i];
                builder.Append('[');
                builder.Append(i + 1);
                builder.Append(' ');
                builder.Append(entrant != null ? entrant.type.ToString() : "null");
                builder.Append(' ');
                builder.Append(entrant != null ? entrant.displayName : "<null>");
                builder.Append(']');

                if (i < wave.Entrants.Count - 1)
                {
                    builder.Append(' ');
                }
            }

            Debug.Log(builder.ToString());
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

            if (ShouldPlayDroneRewardEvent())
            {
                yield return FadeStageTransition(1f, 0f);
                SetPlayerVisible(false);
                sniperZoomInput?.HideZoomVisualsForIntro();

                if (droneRewardEvent != null)
                {
                    yield return droneRewardEvent.Play(ApplyRewardItem);
                }

                yield return FadeStageTransition(0f, 1f);
            }

            StartStage();
            yield return FadeStageTransition(1f, 0f);
            stageTransitionRoutine = null;
        }

        private bool ShouldPlayDroneRewardEvent()
        {
            return droneRewardEvent != null && (currentStageIndex == 2 || currentStageIndex == 4);
        }

        private void ApplyRewardItem(RewardItemType rewardItemType)
        {
            switch (rewardItemType)
            {
                case RewardItemType.Clock:
                    nextStageBonusSeconds += 2;
                    break;
                case RewardItemType.Downgrade:
                    nextStageDowngradeOneAI = true;
                    break;
                case RewardItemType.ExtraMagazine:
                    nextStageBonusBullets += 1;
                    break;
                case RewardItemType.AutoPassHuman:
                    nextStageAutoPassHuman = true;
                    break;
                case RewardItemType.BlankBullet:
                    nextStageBlankBullet = true;
                    break;
                case RewardItemType.BonusChoice:
                    break;
                case RewardItemType.Heart:
                    playerState.RestoreOneMistake();
                    break;
                case RewardItemType.Theft:
                    nextStageTheft = true;
                    Debug.Log("Reward selected: Theft. Next stages will remove one AI when at least two AI are present.");
                    break;
            }
        }

        private void ClearPersistentRewardEffects()
        {
            nextStageBonusSeconds = 0;
            nextStageBonusBullets = 0;
            nextStageDowngradeOneAI = false;
            nextStageAutoPassHuman = false;
            nextStageBlankBullet = false;
            nextStageTheft = false;
            shootingController?.DisableBlankNextHumanShot();
        }

        private IEnumerator RestartStageRoutine()
        {
            yield return FadeStageTransition(0f, 1f);
            hudView?.HideGameOverPanel();
            wavePresenter?.HideAllEntrants();
            shootingController?.Stop();
            StopTimer();
            RestoreTimeScale();
            sniperZoomInput?.HideZoomVisualsForIntro();
            sniperZoomInput?.ResetSniperVisualsToInitialPosition();
            if (failedStageIndex >= 0 && failedStageIndex < stages.Length)
            {
                currentStageIndex = failedStageIndex;
            }

            failedStageIndex = -1;
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

            if (IsOutOfHearts())
            {
                ShowFailedResultPanel();
            }
        }

        private void EnterGameOver()
        {
            ShowFailedResultPanel();
        }

        private bool IsOutOfHearts()
        {
            return playerState.Mistakes >= CurrentStage.maxMistakes;
        }
    }
}
