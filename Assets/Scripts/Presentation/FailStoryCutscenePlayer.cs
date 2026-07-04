using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Gamejam2026.Presentation
{
    public class FailStoryCutscenePlayer : MonoBehaviour
    {
        [SerializeField] private GameObject failStoryPanel;
        [SerializeField] private GameObject[] storyScenes;
        [SerializeField] private Button failedStoryButton;
        [SerializeField] private CanvasGroup failedStoryButtonGroup;
        [SerializeField] private float fadeSeconds = 0.525f;
        [SerializeField] private float holdSeconds = 3.75f;
        [SerializeField] private float buttonFadeSeconds = 0.28f;

        private Coroutine playRoutine;

        public event System.Action RestartRequested;

        private void Awake()
        {
            ResolveMissingReferences();
            RegisterButton();
            HideImmediate();
        }

        private void OnDestroy()
        {
            if (failedStoryButton != null)
            {
                failedStoryButton.onClick.RemoveListener(HandleButtonClicked);
            }
        }

        public IEnumerator Play()
        {
            ResolveMissingReferences();
            RegisterButton();

            if (failStoryPanel != null)
            {
                failStoryPanel.SetActive(true);
            }
            else
            {
                gameObject.SetActive(true);
            }

            playRoutine = null;
            yield return PlayRoutine();
            playRoutine = null;
        }

        public void HideImmediate()
        {
            if (playRoutine != null)
            {
                StopCoroutine(playRoutine);
                playRoutine = null;
            }

            ResolveMissingReferences();

            if (storyScenes != null)
            {
                for (int i = 0; i < storyScenes.Length; i++)
                {
                    if (storyScenes[i] != null)
                    {
                        CanvasGroup sceneGroup = GetOrAddCanvasGroup(storyScenes[i]);
                        sceneGroup.alpha = 0f;
                        sceneGroup.blocksRaycasts = false;
                        sceneGroup.interactable = false;
                        storyScenes[i].SetActive(false);
                    }
                }
            }

            if (failedStoryButtonGroup != null)
            {
                failedStoryButtonGroup.alpha = 0f;
                failedStoryButtonGroup.blocksRaycasts = false;
                failedStoryButtonGroup.interactable = false;
            }

            if (failedStoryButton != null)
            {
                failedStoryButton.interactable = false;
                failedStoryButton.gameObject.SetActive(false);
            }

            if (failStoryPanel != null)
            {
                failStoryPanel.SetActive(false);
            }
        }

        private IEnumerator PlayRoutine()
        {
            if (failStoryPanel == null || storyScenes == null || storyScenes.Length == 0)
            {
                yield break;
            }

            failStoryPanel.SetActive(true);

            if (failedStoryButton != null)
            {
                failedStoryButton.gameObject.SetActive(false);
                failedStoryButton.interactable = false;
            }

            for (int i = 0; i < storyScenes.Length; i++)
            {
                if (storyScenes[i] == null)
                {
                    continue;
                }

                storyScenes[i].SetActive(true);
                CanvasGroup sceneGroup = GetOrAddCanvasGroup(storyScenes[i]);
                sceneGroup.alpha = 0f;
                sceneGroup.blocksRaycasts = false;
                sceneGroup.interactable = false;

                yield return Fade(sceneGroup, 0f, 1f, fadeSeconds);
                yield return new WaitForSeconds(holdSeconds);

                bool isLastScene = i == storyScenes.Length - 1;

                if (!isLastScene)
                {
                    yield return Fade(sceneGroup, 1f, 0f, fadeSeconds);
                    storyScenes[i].SetActive(false);
                }
                else
                {
                    sceneGroup.blocksRaycasts = true;
                    sceneGroup.interactable = true;
                }
            }

            yield return ShowFailedStoryButton();
        }

        private IEnumerator ShowFailedStoryButton()
        {
            if (failedStoryButton == null)
            {
                yield break;
            }

            failedStoryButton.gameObject.SetActive(true);

            if (failedStoryButtonGroup == null)
            {
                failedStoryButtonGroup = GetOrAddCanvasGroup(failedStoryButton.gameObject);
            }

            failedStoryButtonGroup.alpha = 0f;
            failedStoryButtonGroup.blocksRaycasts = false;
            failedStoryButtonGroup.interactable = false;
            failedStoryButton.interactable = false;

            yield return Fade(failedStoryButtonGroup, 0f, 1f, buttonFadeSeconds);

            failedStoryButtonGroup.blocksRaycasts = true;
            failedStoryButtonGroup.interactable = true;
            failedStoryButton.interactable = true;
        }

        private void HandleButtonClicked()
        {
            if (playRoutine != null)
            {
                return;
            }

            playRoutine = StartCoroutine(FadeOutAndRestartRoutine());
        }

        private IEnumerator FadeOutAndRestartRoutine()
        {
            if (failedStoryButton != null)
            {
                failedStoryButton.interactable = false;
            }

            if (failedStoryButtonGroup != null)
            {
                failedStoryButtonGroup.blocksRaycasts = false;
                failedStoryButtonGroup.interactable = false;
            }

            GameObject lastScene = GetLastActiveStoryScene();
            CanvasGroup lastSceneGroup = lastScene != null ? GetOrAddCanvasGroup(lastScene) : null;

            float time = 0f;
            float duration = Mathf.Max(0.01f, fadeSeconds);
            float buttonStart = failedStoryButtonGroup != null ? failedStoryButtonGroup.alpha : 0f;
            float sceneStart = lastSceneGroup != null ? lastSceneGroup.alpha : 0f;

            while (time < duration)
            {
                time += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(time / duration);

                if (failedStoryButtonGroup != null)
                {
                    failedStoryButtonGroup.alpha = Mathf.Lerp(buttonStart, 0f, t);
                }

                if (lastSceneGroup != null)
                {
                    lastSceneGroup.alpha = Mathf.Lerp(sceneStart, 0f, t);
                }

                yield return null;
            }

            playRoutine = null;
            HideImmediate();
            RestartRequested?.Invoke();
        }

        private GameObject GetLastActiveStoryScene()
        {
            if (storyScenes == null)
            {
                return null;
            }

            for (int i = storyScenes.Length - 1; i >= 0; i--)
            {
                if (storyScenes[i] != null && storyScenes[i].activeSelf)
                {
                    return storyScenes[i];
                }
            }

            return null;
        }

        private void ResolveMissingReferences()
        {
            if (failStoryPanel == null)
            {
                GameObject foundPanel = FindSceneObjectByName("Fail Story Panel");

                if (foundPanel != null)
                {
                    failStoryPanel = foundPanel;
                }
            }

            if ((storyScenes == null || storyScenes.Length == 0) && failStoryPanel != null)
            {
                int childCount = failStoryPanel.transform.childCount;
                storyScenes = new GameObject[childCount];

                for (int i = 0; i < childCount; i++)
                {
                    storyScenes[i] = failStoryPanel.transform.GetChild(i).gameObject;
                }
            }

            if (failedStoryButton == null)
            {
                GameObject foundButton = FindSceneObjectByName("Failed Story Button");

                if (foundButton == null)
                {
                    foundButton = FindSceneObjectByName("Fail Story Button");
                }

                if (foundButton != null)
                {
                    failedStoryButton = foundButton.GetComponent<Button>();
                }
            }

            if (failedStoryButtonGroup == null && failedStoryButton != null)
            {
                failedStoryButtonGroup = GetOrAddCanvasGroup(failedStoryButton.gameObject);
            }
        }

        private void RegisterButton()
        {
            if (failedStoryButton == null)
            {
                return;
            }

            failedStoryButton.onClick.RemoveListener(HandleButtonClicked);
            failedStoryButton.onClick.AddListener(HandleButtonClicked);
        }

        private static CanvasGroup GetOrAddCanvasGroup(GameObject target)
        {
            CanvasGroup group = target.GetComponent<CanvasGroup>();

            if (group == null)
            {
                group = target.AddComponent<CanvasGroup>();
            }

            return group;
        }

        private static IEnumerator Fade(CanvasGroup group, float from, float to, float duration)
        {
            if (group == null)
            {
                yield break;
            }

            if (duration <= 0f)
            {
                group.alpha = to;
                yield break;
            }

            float time = 0f;

            while (time < duration)
            {
                time += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(time / duration);
                group.alpha = Mathf.Lerp(from, to, t);
                yield return null;
            }

            group.alpha = to;
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
    }
}
