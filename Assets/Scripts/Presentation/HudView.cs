using TMPro;
using UnityEngine;

namespace Gamejam2026.Presentation
{
    public class HudView : MonoBehaviour
    {
        [SerializeField] private TMP_Text stageText;
        [SerializeField] private TMP_Text waveText;
        [SerializeField] private TMP_Text bulletText;
        [SerializeField] private TMP_Text mistakeText;
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private GameObject timerObject;
        [SerializeField] private GameObject heartObject;
        [SerializeField] private GameObject bulletObject;
        [SerializeField] private TMP_Text timerText;
        [SerializeField] private GameObject clearPanel;
        [SerializeField] private TMP_Text clearText;

        private GameObject[] heartIcons;
        private int maxBulletCount;

        private void Awake()
        {
            ResolveMissingReferences();
            HideClearPanel();
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
            maxBulletCount = Mathf.Max(0, maxBullets);
            int currentBullets = Mathf.Clamp(bullets, 0, maxBulletCount);
            SetText(bulletText, $"{currentBullets}/{maxBulletCount}");
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
            int minutes = totalSeconds / 60;
            int remainderSeconds = totalSeconds % 60;
            SetText(timerText, $"{minutes:00}:{remainderSeconds:00}");
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
                bulletText = bulletObject.GetComponentInChildren<TMP_Text>(true);
            }

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
        }

        private static void SetActiveIconCount(ref GameObject[] icons, GameObject root, int activeCount)
        {
            if (root == null)
            {
                return;
            }

            if (icons == null || icons.Length == 0)
            {
                icons = GetChildObjects(root);
            }

            for (int i = 0; i < icons.Length; i++)
            {
                if (icons[i] != null)
                {
                    icons[i].SetActive(i < activeCount);
                }
            }
        }

        private static GameObject[] GetChildObjects(GameObject root)
        {
            int childCount = root.transform.childCount;
            GameObject[] children = new GameObject[childCount];

            for (int i = 0; i < childCount; i++)
            {
                children[i] = root.transform.GetChild(i).gameObject;
            }

            return children;
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
            }
        }
    }
}
