using TMPro;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Gamejam2026.Presentation
{
    public class HudView : MonoBehaviour
    {
        [SerializeField] private TMP_Text stageText;
        [SerializeField] private TMP_Text waveText;
        [SerializeField] private TMP_Text bulletText;
        [SerializeField] private TMP_Text currentBulletText;
        [SerializeField] private TMP_Text mistakeText;
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private GameObject timerObject;
        [SerializeField] private GameObject heartObject;
        [SerializeField] private GameObject bulletObject;
        [SerializeField] private TMP_Text timerText;
        [SerializeField] private GameObject clearPanel;
        [SerializeField] private CanvasGroup clearPanelCanvasGroup;
        [SerializeField] private GameObject clearImageObject;
        [SerializeField] private CanvasGroup clearImageCanvasGroup;
        [SerializeField] private float clearPanelFadeSeconds = 0.28f;
        [SerializeField] private TMP_Text clearText;
        [SerializeField] private Button clearButton;
        [SerializeField] private TMP_Text clearButtonText;
        [SerializeField] private TMP_Text clearKilledAiText;
        [SerializeField] private TMP_Text clearKilledHumanText;
        [SerializeField] private TMP_Text clearScoreText;
        [SerializeField] private Image clearRankImage;
        [SerializeField] private Sprite rankSSprite;
        [SerializeField] private Sprite rankASprite;
        [SerializeField] private Sprite rankBSprite;
        [SerializeField] private Sprite rankCSprite;
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private TMP_Text gameOverText;
        [SerializeField] private Button restartButton;
        [SerializeField] private GameObject stagePanel;
        [SerializeField] private CanvasGroup stagePanelCanvasGroup;
        [SerializeField] private Image stageTitleImage;
        [SerializeField] private Sprite[] stageTitleSprites;
        [SerializeField] private GameObject progressRedObject;
        [SerializeField] private CanvasGroup progressRedCanvasGroup;
        [SerializeField] private Image[] progressPanelImages;
        [SerializeField] private Sprite[] progressPanelDefaultSprites;
        [SerializeField] private Sprite[] progressPanelSelectedSprites;

        private GameObject[] heartIcons;
        private GameObject[] bulletIcons;
        private int maxBulletCount;
        private Coroutine clearPanelFadeRoutine;
        private bool clearPanelInteractableRequested = true;

        public event System.Action ClearPanelContinueRequested;
        public event System.Action RestartRequested;

        private void Awake()
        {
            ResolveMissingReferences();
            RegisterClearButton();
            RegisterRestartButton();
            HideClearPanel();
            HideGameOverPanel();
            HideStagePanel();
        }

        private void OnDestroy()
        {
            if (clearButton != null)
            {
                clearButton.onClick.RemoveListener(HandleClearButtonClicked);
            }

            if (restartButton != null)
            {
                restartButton.onClick.RemoveListener(HandleRestartButtonClicked);
            }
        }

        public void SetStage(string stageName)
        {
            SetText(stageText, stageName);
        }

        public void SetWave(int currentWave, int totalWaves)
        {
            SetText(waveText, $"WAVE {currentWave}/{totalWaves}");
        }

        public void SetBullets(int bullets)
        {
            SetBullets(bullets, maxBulletCount);
        }

        public void SetBullets(int bullets, int maxBullets)
        {
            ResolveMissingReferences();
            maxBulletCount = Mathf.Max(0, maxBullets);
            int currentBullets = Mathf.Clamp(bullets, 0, maxBulletCount);
            SetText(currentBulletText, currentBullets.ToString());
            SetText(bulletText, $"/{maxBulletCount}");
            SetActiveIconCount(ref bulletIcons, bulletObject, currentBullets, IsBulletIcon);
        }

        public void SetMistakes(int mistakes, int maxMistakes)
        {
            SetText(mistakeText, $"ERROR {mistakes}/{maxMistakes}");
            SetHearts(maxMistakes - mistakes, maxMistakes);
        }

        public void SetHearts(int hearts, int maxHearts)
        {
            int visibleHearts = Mathf.Clamp(hearts, 0, maxHearts);
            SetActiveIconCount(ref heartIcons, heartObject, visibleHearts);
        }

        public void SetScore(int score)
        {
            SetText(scoreText, $"SCORE {score}");
        }

        public void SetStatus(string message)
        {
            SetText(statusText, message);
        }

        public void SetTimer(float seconds)
        {
            int totalSeconds = Mathf.Max(0, Mathf.CeilToInt(seconds));
            SetText(timerText, totalSeconds.ToString());
        }

        public void SetTimerVisible(bool visible)
        {
            ResolveMissingReferences();

            if (timerObject != null)
            {
                timerObject.SetActive(visible);
            }
        }

        public void SetGameplayHudVisible(bool visible)
        {
            ResolveMissingReferences();
            SetTimerVisible(visible);

            if (heartObject != null)
            {
                heartObject.SetActive(visible);
            }

            if (bulletObject != null)
            {
                bulletObject.SetActive(visible);
            }
        }

        public void ShowClearPanel(string message = "CLEAR")
        {
            ShowClearPanel(message, 0, 0, 0, "C", "Next");
        }

        public void ShowClearPanel(string message, int killedAi, int killedHumans, int score)
        {
            ShowClearPanel(message, killedAi, killedHumans, score, "C", "Next");
        }

        public void ShowClearPanel(string message, int killedAi, int killedHumans, int score, string rank)
        {
            ShowClearPanel(message, killedAi, killedHumans, score, rank, "Next");
        }

        public void ShowClearPanel(string message, int killedAi, int killedHumans, int score, string rank, string buttonLabel)
        {
            ResolveMissingReferences();
            RegisterClearButton();
            SetText(clearText, message);
            SetText(clearKilledAiText, killedAi.ToString());
            SetText(clearKilledHumanText, killedHumans.ToString());
            SetText(clearScoreText, Mathf.Max(0, score).ToString());
            SetText(clearButtonText, buttonLabel);
            SetRank(rank);

            if (clearPanel != null)
            {
                if (clearPanelFadeRoutine != null)
                {
                    StopCoroutine(clearPanelFadeRoutine);
                }

                clearPanelFadeRoutine = StartCoroutine(FadeClearPanelIn());
            }
        }

        public void HideClearPanel()
        {
            if (clearPanelFadeRoutine != null)
            {
                StopCoroutine(clearPanelFadeRoutine);
                clearPanelFadeRoutine = null;
            }

            if (clearPanelCanvasGroup != null)
            {
                clearPanelCanvasGroup.alpha = 0f;
                clearPanelCanvasGroup.blocksRaycasts = false;
                clearPanelCanvasGroup.interactable = false;
            }

            if (clearImageCanvasGroup != null)
            {
                clearImageCanvasGroup.alpha = 0f;
                clearImageCanvasGroup.blocksRaycasts = false;
                clearImageCanvasGroup.interactable = false;
            }

            if (clearPanel != null)
            {
                clearPanel.SetActive(false);
            }

            if (clearImageObject != null)
            {
                clearImageObject.SetActive(false);
            }
        }

        public void ShowGameOverPanel(string message = "GAME OVER")
        {
            ResolveMissingReferences();
            RegisterRestartButton();
            SetText(gameOverText, message);

            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(true);
            }
        }

        public void HideGameOverPanel()
        {
            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(false);
            }
        }

        public IEnumerator PlayStagePanel(int stageNumber, float fadeSeconds, float holdSeconds)
        {
            ResolveMissingReferences();
            SetStageTitle(stageNumber);
            SetStageProgress(stageNumber);

            if (stagePanel == null)
            {
                yield break;
            }

            if (stagePanelCanvasGroup == null)
            {
                stagePanelCanvasGroup = stagePanel.GetComponent<CanvasGroup>();
            }

            if (stagePanelCanvasGroup == null)
            {
                stagePanelCanvasGroup = stagePanel.AddComponent<CanvasGroup>();
            }

            stagePanel.SetActive(true);
            SetProgressRedVisible(true);
            stagePanelCanvasGroup.blocksRaycasts = true;
            stagePanelCanvasGroup.interactable = false;

            yield return FadeStagePanel(0f, 1f, fadeSeconds);

            if (holdSeconds > 0f)
            {
                yield return new WaitForSeconds(holdSeconds);
            }

            yield return FadeStagePanel(1f, 0f, fadeSeconds);
            HideStagePanel();
        }

        public void HideStagePanel()
        {
            if (stagePanelCanvasGroup != null)
            {
                if (Application.isPlaying)
                {
                    stagePanelCanvasGroup.alpha = 0f;
                }

                stagePanelCanvasGroup.blocksRaycasts = false;
                stagePanelCanvasGroup.interactable = false;
            }

            if (stagePanel != null && Application.isPlaying)
            {
                stagePanel.SetActive(false);
            }

            SetProgressRedVisible(false);
        }

        public void SetClearPanelInteractable(bool interactable)
        {
            ResolveMissingReferences();
            RegisterClearButton();
            clearPanelInteractableRequested = interactable;

            if (clearButton != null)
            {
                clearButton.interactable = interactable;
            }

            if (clearPanelCanvasGroup != null)
            {
                clearPanelCanvasGroup.interactable = interactable;
                clearPanelCanvasGroup.blocksRaycasts = interactable;
            }
        }

        private void ResolveMissingReferences()
        {
            if (clearPanel == null)
            {
                clearPanel = FindClearPanelObject();
            }

            if (clearPanelCanvasGroup == null && clearPanel != null)
            {
                clearPanelCanvasGroup = clearPanel.GetComponent<CanvasGroup>();

                if (clearPanelCanvasGroup == null)
                {
                    clearPanelCanvasGroup = clearPanel.AddComponent<CanvasGroup>();
                }
            }

            if (clearImageObject == null)
            {
                clearImageObject = FindFirstSceneObjectByNames("Clear Image", "ClearImage");
            }

            if (clearImageCanvasGroup == null && clearImageObject != null)
            {
                clearImageCanvasGroup = clearImageObject.GetComponent<CanvasGroup>();

                if (clearImageCanvasGroup == null)
                {
                    clearImageCanvasGroup = clearImageObject.AddComponent<CanvasGroup>();
                }
            }

            if (clearText == null && clearPanel != null)
            {
                clearText = FindTextChildByName(clearPanel.transform, "Clear Text", "ClearText", "Clear Title", "ClearTitle", "Title");
            }

            if (clearButton == null && clearPanel != null)
            {
                clearButton = FindButtonChildByName(clearPanel.transform, "Next Button", "NextButton", "Clear Button", "ClearButton", "Button");
            }

            if (clearButtonText == null && clearButton != null)
            {
                clearButtonText = clearButton.GetComponentInChildren<TMP_Text>(true);
            }

            if (clearKilledAiText == null && clearPanel != null)
            {
                clearKilledAiText = FindTextChildByName(clearPanel.transform, "KillAIText  (1)", "KillAIText (1)", "KillAIText(1)", "KillAIText Value", "KillAITextValue");
            }

            if (clearKilledHumanText == null && clearPanel != null)
            {
                clearKilledHumanText = FindTextChildByName(clearPanel.transform, "KillHumanText  (1)", "KillHumanText (1)", "KillHumanText(1)", "KillHumanText Value", "KillHumanTextValue");
            }

            if (clearScoreText == null && clearPanel != null)
            {
                clearScoreText = FindTextChildByName(clearPanel.transform, "ScroreText (1)", "ScroreText  (1)", "ScoreText (1)", "ScoreText  (1)", "ScroreText Value", "ScoreText Value");
            }

            if (clearRankImage == null && clearPanel != null)
            {
                clearRankImage = FindImageChildByName(clearPanel.transform, "Rank");
            }

            ResolveRankSprites();

            if (gameOverPanel == null)
            {
                Transform found = transform.Find("Game Over Panel");

                if (found == null)
                {
                    found = transform.Find("GameOver Panel");
                }

                if (found == null)
                {
                    found = transform.Find("GameOverPanel");
                }

                if (found != null)
                {
                    gameOverPanel = found.gameObject;
                }
            }

            if (gameOverPanel == null)
            {
                gameOverPanel = FindFirstSceneObjectByNames("Game Over Panel", "GameOver Panel", "GameOverPanel", "Game Over");
            }

            if (gameOverText == null && gameOverPanel != null)
            {
                gameOverText = FindTextChildByName(gameOverPanel.transform, "Game Over Text", "GameOver Text", "GameOverText", "Title", "Text");

                if (gameOverText == null)
                {
                    TMP_Text[] texts = gameOverPanel.GetComponentsInChildren<TMP_Text>(true);

                    for (int i = 0; i < texts.Length; i++)
                    {
                        if (texts[i] != null && texts[i].GetComponentInParent<Button>(true) == null)
                        {
                            gameOverText = texts[i];
                            break;
                        }
                    }
                }
            }

            if (restartButton == null && gameOverPanel != null)
            {
                restartButton = FindButtonChildByName(gameOverPanel.transform, "Restart Button", "RestartButton", "Restart", "Button");
            }

            if (stagePanel == null)
            {
                Transform found = transform.Find("Stage Panel");

                if (found == null)
                {
                    found = transform.Find("StagePanel");
                }

                if (found != null)
                {
                    stagePanel = found.gameObject;
                }
            }

            if (stagePanel == null)
            {
                stagePanel = FindFirstSceneObjectByNames("Stage Panel", "StagePanel", "Progress Panel", "ProgressPanel");
            }

            if (stagePanelCanvasGroup == null && stagePanel != null)
            {
                stagePanelCanvasGroup = stagePanel.GetComponent<CanvasGroup>();
            }

            if (stageTitleImage == null && stagePanel != null)
            {
                stageTitleImage = FindImageChildByName(stagePanel.transform, "Stage Title", "StageTitle", "Progress Title", "ProgressTitle");
            }

            if (progressRedObject == null && stagePanel != null)
            {
                Transform found = FindChildByNames(stagePanel.transform, "Progress Red", "ProgressRed", "Progress Red Ring", "ProgressRedRing");

                if (found != null)
                {
                    progressRedObject = found.gameObject;
                }
            }

            if (progressRedObject == null)
            {
                progressRedObject = FindFirstSceneObjectByNames("Progress Red", "ProgressRed", "Progress Red Ring", "ProgressRedRing");
            }

            if (progressRedCanvasGroup == null && progressRedObject != null)
            {
                progressRedCanvasGroup = progressRedObject.GetComponent<CanvasGroup>();
            }

            ResolveStageTitleSprites();
            ResolveProgressPanels();

            if (timerObject == null)
            {
                Transform timerRoot = transform.Find("Timerr UI");

                if (timerRoot == null)
                {
                    timerRoot = transform.Find("Timer");
                }

                if (timerRoot != null)
                {
                    timerObject = timerRoot.gameObject;
                }
            }

            if (timerObject == null)
            {
                timerObject = FindSceneObjectByName("Timer UI");
            }

            if (timerObject == null)
            {
                timerObject = FindSceneObjectByName("Timer");
            }

            if (heartObject == null)
            {
                heartObject = FindFirstSceneObjectByNames("Heart UI", "Heart", "Hearts UI", "Hearts", "Life UI", "Life");
            }

            if (bulletObject == null)
            {
                bulletObject = FindFirstSceneObjectByNames("Bullet UI", "Bullet", "Bullets UI", "Bullets", "Ammo UI", "Ammo");
            }

            if (bulletText == null && bulletObject != null)
            {
                bulletText = FindTextChildByName(bulletObject.transform, "Bullet Text", "BulletText", "Total Bullet", "TotalBullet");
            }

            if (currentBulletText == null && bulletObject != null)
            {
                currentBulletText = FindTextChildByName(bulletObject.transform, "Current Bullet", "CurrentBullet", "Current Bullets", "CurrentBullets");
            }

            if (bulletText == null && bulletObject != null)
            {
                TMP_Text[] texts = bulletObject.GetComponentsInChildren<TMP_Text>(true);

                for (int i = 0; i < texts.Length; i++)
                {
                    if (texts[i] != null && texts[i] != currentBulletText)
                    {
                        bulletText = texts[i];
                        break;
                    }
                }
            }

            BringRootTextToFront(bulletText);
            BringRootTextToFront(currentBulletText);

            if (timerText == null && timerObject != null)
            {
                Transform timerRoot = timerObject.transform;
                Transform timeText = timerRoot.Find("TIme");

                if (timeText == null)
                {
                    timeText = timerRoot.Find("Time");
                }

                if (timeText != null)
                {
                    timerText = timeText.GetComponent<TMP_Text>();
                }
                else
                {
                    timerText = timerRoot.GetComponentInChildren<TMP_Text>(true);
                }
            }

            BringRootTextToFront(timerText);
        }

        private void SetStageTitle(int stageNumber)
        {
            ResolveStageTitleSprites();

            if (stageTitleImage == null || stageTitleSprites == null || stageTitleSprites.Length == 0)
            {
                return;
            }

            int index = Mathf.Clamp(stageNumber - 1, 0, stageTitleSprites.Length - 1);
            Sprite sprite = stageTitleSprites[index];

            if (sprite != null)
            {
                stageTitleImage.sprite = sprite;
                stageTitleImage.enabled = true;
                stageTitleImage.preserveAspect = true;
            }
        }

        private void SetStageProgress(int stageNumber)
        {
            ResolveProgressPanels();

            if (progressPanelImages == null || progressPanelImages.Length == 0)
            {
                return;
            }

            for (int i = 0; i < progressPanelImages.Length; i++)
            {
                Image image = progressPanelImages[i];

                if (image == null)
                {
                    continue;
                }

                bool isCurrentStage = i == stageNumber - 1;
                Sprite[] sourceSprites = isCurrentStage ? progressPanelSelectedSprites : progressPanelDefaultSprites;
                Sprite sprite = GetSpriteAt(sourceSprites, i);

                if (sprite != null)
                {
                    image.sprite = sprite;
                    image.enabled = true;
                    image.preserveAspect = true;
                }
            }
        }

        private void SetRank(string rank)
        {
            ResolveRankSprites();

            if (clearRankImage == null)
            {
                return;
            }

            Sprite sprite = GetRankSprite(rank);

            if (sprite != null)
            {
                clearRankImage.sprite = sprite;
                clearRankImage.enabled = true;
                clearRankImage.preserveAspect = true;
            }
        }

        private Sprite GetRankSprite(string rank)
        {
            switch ((rank ?? string.Empty).Trim().ToUpperInvariant())
            {
                case "S":
                    return rankSSprite;
                case "A":
                    return rankASprite;
                case "B":
                    return rankBSprite;
                default:
                    return rankCSprite;
            }
        }

        private IEnumerator FadeStagePanel(float from, float to, float duration)
        {
            if (stagePanelCanvasGroup == null)
            {
                yield break;
            }

            if (duration <= 0f)
            {
                stagePanelCanvasGroup.alpha = to;
                SetProgressRedAlpha(to);
                yield break;
            }

            float time = 0f;

            while (time < duration)
            {
                time += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(time / duration);
                float alpha = Mathf.Lerp(from, to, t);
                stagePanelCanvasGroup.alpha = alpha;
                SetProgressRedAlpha(alpha);
                yield return null;
            }

            stagePanelCanvasGroup.alpha = to;
            SetProgressRedAlpha(to);
        }

        private void SetProgressRedVisible(bool visible)
        {
            if (progressRedObject == null)
            {
                return;
            }

            progressRedObject.SetActive(visible);

            if (progressRedCanvasGroup == null)
            {
                progressRedCanvasGroup = progressRedObject.GetComponent<CanvasGroup>();
            }

            if (progressRedCanvasGroup != null)
            {
                progressRedCanvasGroup.blocksRaycasts = false;
                progressRedCanvasGroup.interactable = false;
            }
        }

        private void SetProgressRedAlpha(float alpha)
        {
            if (progressRedObject == null)
            {
                return;
            }

            if (progressRedCanvasGroup == null)
            {
                progressRedCanvasGroup = progressRedObject.GetComponent<CanvasGroup>();
            }

            if (progressRedCanvasGroup == null)
            {
                progressRedCanvasGroup = progressRedObject.AddComponent<CanvasGroup>();
                progressRedCanvasGroup.blocksRaycasts = false;
                progressRedCanvasGroup.interactable = false;
            }

            progressRedCanvasGroup.alpha = alpha;
        }

        private void RegisterClearButton()
        {
            if (clearButton == null)
            {
                return;
            }

            clearButton.onClick.RemoveListener(HandleClearButtonClicked);
            clearButton.onClick.AddListener(HandleClearButtonClicked);
        }

        private void HandleClearButtonClicked()
        {
            if (clearPanelFadeRoutine != null)
            {
                return;
            }

            clearPanelFadeRoutine = StartCoroutine(FadeClearPanelOutThenContinue());
        }

        private IEnumerator FadeClearPanelIn()
        {
            ResolveMissingReferences();

            if (clearPanel == null)
            {
                clearPanelFadeRoutine = null;
                yield break;
            }

            if (clearPanelCanvasGroup == null)
            {
                clearPanelCanvasGroup = clearPanel.GetComponent<CanvasGroup>();
            }

            if (clearPanelCanvasGroup == null)
            {
                clearPanelCanvasGroup = clearPanel.AddComponent<CanvasGroup>();
            }

            clearPanel.SetActive(true);
            if (clearImageObject != null)
            {
                clearImageObject.transform.SetAsLastSibling();
            }
            clearPanel.transform.SetAsLastSibling();
            clearPanelCanvasGroup.alpha = 0f;
            clearPanelCanvasGroup.blocksRaycasts = false;
            clearPanelCanvasGroup.interactable = false;

            if (clearImageObject != null)
            {
                clearImageObject.SetActive(true);
            }

            if (clearImageCanvasGroup != null)
            {
                clearImageCanvasGroup.alpha = 0f;
                clearImageCanvasGroup.blocksRaycasts = false;
                clearImageCanvasGroup.interactable = false;
            }

            if (clearButton != null)
            {
                clearButton.interactable = false;
            }

            yield return FadeClearPanel(0f, 1f);

            clearPanelCanvasGroup.blocksRaycasts = clearPanelInteractableRequested;
            clearPanelCanvasGroup.interactable = clearPanelInteractableRequested;

            if (clearButton != null)
            {
                clearButton.interactable = clearPanelInteractableRequested;
            }

            clearPanelFadeRoutine = null;
        }

        private IEnumerator FadeClearPanelOutThenContinue()
        {
            ResolveMissingReferences();

            if (clearButton != null)
            {
                clearButton.interactable = false;
            }

            if (clearPanelCanvasGroup != null)
            {
                clearPanelCanvasGroup.blocksRaycasts = false;
                clearPanelCanvasGroup.interactable = false;
            }

            yield return FadeClearPanel(1f, 0f);

            if (clearPanel != null)
            {
                clearPanel.SetActive(false);
            }

            if (clearImageObject != null)
            {
                clearImageObject.SetActive(false);
            }

            clearPanelFadeRoutine = null;
            ClearPanelContinueRequested?.Invoke();
        }

        private IEnumerator FadeClearPanel(float from, float to)
        {
            if (clearPanelCanvasGroup == null || clearPanelFadeSeconds <= 0f)
            {
                if (clearPanelCanvasGroup != null)
                {
                    clearPanelCanvasGroup.alpha = to;
                }

                if (clearImageCanvasGroup != null)
                {
                    clearImageCanvasGroup.alpha = to;
                }

                yield break;
            }

            float time = 0f;

            while (time < clearPanelFadeSeconds)
            {
                time += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(time / clearPanelFadeSeconds);
                clearPanelCanvasGroup.alpha = Mathf.Lerp(from, to, t);

                if (clearImageCanvasGroup != null)
                {
                    clearImageCanvasGroup.alpha = Mathf.Lerp(from, to, t);
                }

                yield return null;
            }

            clearPanelCanvasGroup.alpha = to;

            if (clearImageCanvasGroup != null)
            {
                clearImageCanvasGroup.alpha = to;
            }
        }

        private void RegisterRestartButton()
        {
            if (restartButton == null)
            {
                return;
            }

            restartButton.onClick.RemoveListener(HandleRestartButtonClicked);
            restartButton.onClick.AddListener(HandleRestartButtonClicked);
        }

        private void HandleRestartButtonClicked()
        {
            RestartRequested?.Invoke();
        }

        private static void SetActiveIconCount(ref GameObject[] icons, GameObject root, int activeCount)
        {
            SetActiveIconCount(ref icons, root, activeCount, null);
        }

        private static void SetActiveIconCount(ref GameObject[] icons, GameObject root, int activeCount, System.Predicate<GameObject> filter)
        {
            if (root == null)
            {
                return;
            }

            if (icons == null || icons.Length == 0)
            {
                icons = GetChildObjects(root, filter);
            }

            for (int i = 0; i < icons.Length; i++)
            {
                if (icons[i] != null)
                {
                    icons[i].SetActive(i < activeCount);
                }
            }
        }

        private static GameObject[] GetChildObjects(GameObject root, System.Predicate<GameObject> filter = null)
        {
            int childCount = root.transform.childCount;
            System.Collections.Generic.List<GameObject> children = new System.Collections.Generic.List<GameObject>(childCount);

            for (int i = 0; i < childCount; i++)
            {
                GameObject child = root.transform.GetChild(i).gameObject;

                if (filter == null || filter(child))
                {
                    children.Add(child);
                }
            }

            return children.ToArray();
        }

        private static bool IsBulletIcon(GameObject target)
        {
            if (target == null || target.GetComponent<TMP_Text>() != null)
            {
                return false;
            }

            string normalizedName = target.name.Trim().Replace(" ", string.Empty);
            return normalizedName.StartsWith("Bullet", System.StringComparison.OrdinalIgnoreCase);
        }

        private static TMP_Text FindTextChildByName(Transform root, params string[] names)
        {
            if (root == null)
            {
                return null;
            }

            TMP_Text[] texts = root.GetComponentsInChildren<TMP_Text>(true);

            for (int i = 0; i < texts.Length; i++)
            {
                if (texts[i] == null)
                {
                    continue;
                }

                string textName = texts[i].name.Trim();

                for (int j = 0; j < names.Length; j++)
                {
                    if (textName == names[j])
                    {
                        return texts[i];
                    }
                }
            }

            return null;
        }

        private static Button FindButtonChildByName(Transform root, params string[] names)
        {
            if (root == null)
            {
                return null;
            }

            Button[] buttons = root.GetComponentsInChildren<Button>(true);

            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] == null)
                {
                    continue;
                }

                string buttonName = buttons[i].name.Trim();

                for (int j = 0; j < names.Length; j++)
                {
                    if (buttonName == names[j])
                    {
                        return buttons[i];
                    }
                }
            }

            return buttons.Length > 0 ? buttons[0] : null;
        }

        private static Image FindImageChildByName(Transform root, params string[] names)
        {
            if (root == null)
            {
                return null;
            }

            Image[] images = root.GetComponentsInChildren<Image>(true);

            for (int i = 0; i < images.Length; i++)
            {
                if (images[i] == null)
                {
                    continue;
                }

                string imageName = images[i].name.Trim();

                for (int j = 0; j < names.Length; j++)
                {
                    if (imageName == names[j])
                    {
                        return images[i];
                    }
                }
            }

            return null;
        }

        private static Transform FindChildByNames(Transform root, params string[] names)
        {
            if (root == null)
            {
                return null;
            }

            Transform[] children = root.GetComponentsInChildren<Transform>(true);

            for (int i = 0; i < children.Length; i++)
            {
                Transform child = children[i];

                if (child == null)
                {
                    continue;
                }

                string childName = child.name.Trim();

                for (int j = 0; j < names.Length; j++)
                {
                    if (childName == names[j])
                    {
                        return child;
                    }
                }
            }

            return null;
        }

        private void ResolveStageTitleSprites()
        {
#if UNITY_EDITOR
            stageTitleSprites = new Sprite[5];

            for (int i = 0; i < stageTitleSprites.Length; i++)
            {
                string path = $"Assets/Image/Mission UI/progress_title_{i + 1}.png";
                stageTitleSprites[i] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }
#endif
        }

        private void ResolveProgressPanels()
        {
            if (progressPanelImages == null || progressPanelImages.Length == 0)
            {
                progressPanelImages = new Image[5];

                for (int i = 0; i < progressPanelImages.Length; i++)
                {
                    GameObject found = FindFirstSceneObjectByNames(
                        $"Progress Default{i + 1}",
                        $"ProgressDefault{i + 1}",
                        $"Progress Panel {i + 1}",
                        $"ProgressPanel{i + 1}");

                    if (found != null)
                    {
                        progressPanelImages[i] = found.GetComponent<Image>();
                    }
                }
            }

#if UNITY_EDITOR
            ResolveProgressPanelSpritesFromMissionUi();
#endif
        }

#if UNITY_EDITOR
        private void ResolveProgressPanelSpritesFromMissionUi()
        {
            const int progressPanelCount = 5;
            const string spriteFolder = "Assets/Image/Mission UI";

            progressPanelDefaultSprites = new Sprite[progressPanelCount];
            progressPanelSelectedSprites = new Sprite[progressPanelCount];

            for (int i = 0; i < progressPanelCount; i++)
            {
                int stageNumber = i + 1;

                string defaultPath = $"{spriteFolder}/progress_panel_{stageNumber}_default.png";
                progressPanelDefaultSprites[i] = AssetDatabase.LoadAssetAtPath<Sprite>(defaultPath);

                string selectedPath = $"{spriteFolder}/progress_panel_{stageNumber}_select_.png";
                progressPanelSelectedSprites[i] = AssetDatabase.LoadAssetAtPath<Sprite>(selectedPath);
            }
        }
#endif

        private void ResolveRankSprites()
        {
#if UNITY_EDITOR
            if (rankSSprite == null)
            {
                rankSSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Image/Complete/result_S.png");
            }

            if (rankASprite == null)
            {
                rankASprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Image/Complete/result_A.png");
            }

            if (rankBSprite == null)
            {
                rankBSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Image/Complete/result_B.png");
            }

            if (rankCSprite == null)
            {
                rankCSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Image/Complete/result_C.png");
            }
#endif
        }

        private static Sprite GetSpriteAt(Sprite[] sprites, int index)
        {
            if (sprites == null || index < 0 || index >= sprites.Length)
            {
                return null;
            }

            return sprites[index];
        }

        private static GameObject FindFirstSceneObjectByNames(params string[] names)
        {
            for (int i = 0; i < names.Length; i++)
            {
                GameObject found = FindSceneObjectByName(names[i]);

                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static GameObject FindClearPanelObject()
        {
            Transform[] transforms = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            GameObject fallback = null;

            for (int i = 0; i < transforms.Length; i++)
            {
                Transform candidate = transforms[i];

                if (candidate == null)
                {
                    continue;
                }

                string candidateName = candidate.name.Trim();

                if (candidateName != "Clear Panel" && candidateName != "ClearPanel")
                {
                    continue;
                }

                if (fallback == null)
                {
                    fallback = candidate.gameObject;
                }

                if (HasClearResultChildren(candidate))
                {
                    return candidate.gameObject;
                }
            }

            return fallback;
        }

        private static bool HasClearResultChildren(Transform root)
        {
            if (root == null)
            {
                return false;
            }

            bool hasKilledAi = FindTextChildByName(root, "KillAIText  (1)", "KillAIText (1)", "KillAIText(1)") != null;
            bool hasKilledHuman = FindTextChildByName(root, "KillHumanText  (1)", "KillHumanText (1)", "KillHumanText(1)") != null;
            bool hasScore = FindTextChildByName(root, "ScroreText (1)", "ScroreText  (1)", "ScoreText (1)", "ScoreText  (1)") != null;
            bool hasNext = FindButtonChildByName(root, "Next Button", "NextButton") != null;
            return hasKilledAi && hasKilledHuman && hasScore && hasNext;
        }

        private static GameObject FindSceneObjectByName(string targetName)
        {
            GameObject activeObject = GameObject.Find(targetName);

            if (activeObject != null)
            {
                return activeObject;
            }

            Transform[] transforms = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            for (int i = 0; i < transforms.Length; i++)
            {
                if (transforms[i].name.Trim() == targetName)
                {
                    return transforms[i].gameObject;
                }
            }

            return null;
        }

        private static void SetText(TMP_Text text, string value)
        {
            if (text != null)
            {
                text.text = value;
                text.enabled = true;
                text.ForceMeshUpdate();
            }
        }

        private static void BringRootTextToFront(TMP_Text text)
        {
            if (text != null && text.transform.parent != null && text.transform.parent.GetComponent<TMP_Text>() == null)
            {
                text.transform.SetAsLastSibling();
            }
        }
    }
}
