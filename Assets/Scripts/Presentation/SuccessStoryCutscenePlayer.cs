using System.Collections;
using UnityEngine;

namespace Gamejam2026.Presentation
{
    public class SuccessStoryCutscenePlayer : MonoBehaviour
    {
        [SerializeField] private GameObject successStoryPanel;
        [SerializeField] private GameObject[] storyScenes;
        [SerializeField] private float fadeSeconds = 0.525f;
        [SerializeField] private float holdSeconds = 3.75f;
        [SerializeField] private bool keepLastSceneVisible;
        [SerializeField] private AudioClip endingMusicClip;
        [SerializeField, Range(0f, 1f)] private float endingMusicVolume = 1f;
        [SerializeField] private BgmPlayer bgmPlayer;

        private Coroutine playRoutine;

        private void Awake()
        {
            ResolveMissingReferences();
            HideImmediate();
        }

        public IEnumerator Play()
        {
            ResolveMissingReferences();
            PlayEndingMusic();

            if (successStoryPanel != null)
            {
                successStoryPanel.SetActive(true);
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
                    if (storyScenes[i] == null)
                    {
                        continue;
                    }

                    CanvasGroup sceneGroup = GetOrAddCanvasGroup(storyScenes[i]);
                    sceneGroup.alpha = 0f;
                    storyScenes[i].SetActive(false);
                }
            }

            if (successStoryPanel != null)
            {
                successStoryPanel.SetActive(false);
            }

            RestoreBgm();
        }

        private IEnumerator PlayRoutine()
        {
            if (successStoryPanel == null || storyScenes == null || storyScenes.Length == 0)
            {
                yield break;
            }

            successStoryPanel.SetActive(true);

            for (int i = 0; i < storyScenes.Length; i++)
            {
                if (storyScenes[i] == null)
                {
                    continue;
                }

                StretchSceneToPanel(storyScenes[i]);
                storyScenes[i].SetActive(true);
                CanvasGroup sceneGroup = GetOrAddCanvasGroup(storyScenes[i]);
                sceneGroup.alpha = 0f;

                yield return Fade(sceneGroup, 0f, 1f, fadeSeconds);
                yield return new WaitForSeconds(holdSeconds);

                bool isLastScene = i == storyScenes.Length - 1;

                if (!isLastScene || !keepLastSceneVisible)
                {
                    yield return Fade(sceneGroup, 1f, 0f, fadeSeconds);
                    storyScenes[i].SetActive(false);
                }
            }

            if (!keepLastSceneVisible)
            {
                successStoryPanel.SetActive(false);
            }
        }

        private void ResolveMissingReferences()
        {
            if (successStoryPanel == null)
            {
                GameObject foundPanel = FindSceneObjectByName("Success Story Panel");

                if (foundPanel == null)
                {
                    foundPanel = FindSceneObjectByName("Sucess Story Panel");
                }

                if (foundPanel != null)
                {
                    successStoryPanel = foundPanel;
                }
            }

            if ((storyScenes == null || storyScenes.Length == 0) && successStoryPanel != null)
            {
                int childCount = successStoryPanel.transform.childCount;
                storyScenes = new GameObject[childCount];

                for (int i = 0; i < childCount; i++)
                {
                    storyScenes[i] = successStoryPanel.transform.GetChild(i).gameObject;
                }
            }

            if (bgmPlayer == null)
            {
                bgmPlayer = FindFirstObjectByType<BgmPlayer>();
            }
        }

        private void PlayEndingMusic()
        {
            if (bgmPlayer == null || endingMusicClip == null)
            {
                return;
            }

            bgmPlayer.PlayTemporaryLoop(endingMusicClip, endingMusicVolume);
        }

        private void RestoreBgm()
        {
            if (bgmPlayer != null)
            {
                bgmPlayer.RestoreMainLoop();
            }
        }

        private static void StretchSceneToPanel(GameObject scene)
        {
            if (scene == null)
            {
                return;
            }

            RectTransform rect = scene.GetComponent<RectTransform>();

            if (rect == null)
            {
                return;
            }

            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
            rect.localScale = Vector3.one;
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
