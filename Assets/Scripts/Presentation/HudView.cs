using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
        [SerializeField] private TMP_Text clearText;
        [SerializeField] private Button clearButton;
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private TMP_Text gameOverText;
        [SerializeField] private Button restartButton;

        private GameObject[] heartIcons;
        private GameObject[] bulletIcons;
        private int maxBulletCount;

        public event System.Action ClearPanelContinueRequested;
        public event System.Action RestartRequested;

        private void Awake()
        {
            ResolveMissingReferences();
            RegisterClearButton();
            RegisterRestartButton();
            HideClearPanel();
            HideGameOverPanel();
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
            ResolveMissingReferences();
            RegisterClearButton();
            SetText(clearText, message);

            if (clearPanel != null)
            {
                clearPanel.SetActive(true);
            }
        }

        public void HideClearPanel()
        {
            if (clearPanel != null)
            {
                clearPanel.SetActive(false);
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

        public void SetClearPanelInteractable(bool interactable)
        {
            ResolveMissingReferences();
            RegisterClearButton();

            if (clearButton != null)
            {
                clearButton.interactable = interactable;
            }
        }

        private void ResolveMissingReferences()
        {
            if (clearPanel == null)
            {
                Transform found = transform.Find("Clear Panel");

                if (found == null)
                {
                    found = transform.Find("ClearPanel");
                }

                if (found != null)
                {
                    clearPanel = found.gameObject;
                }
            }

            if (clearText == null && clearPanel != null)
            {
                clearText = clearPanel.GetComponentInChildren<TMP_Text>(true);
            }

            if (clearButton == null && clearPanel != null)
            {
                clearButton = clearPanel.GetComponentInChildren<Button>(true);
            }

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
            ClearPanelContinueRequested?.Invoke();
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
