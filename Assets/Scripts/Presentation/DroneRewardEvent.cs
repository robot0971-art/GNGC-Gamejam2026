using System.Collections;
using System.Collections.Generic;
using Gamejam2026.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Gamejam2026.Presentation
{
    public class DroneRewardEvent : MonoBehaviour
    {
        [SerializeField] private GameObject rewardPanel;
        [SerializeField] private RectTransform droneRoot;
        [SerializeField] private RectTransform droneTextRoot;
        [SerializeField] private Button droneButton;
        [SerializeField] private Image droneImage;
        [SerializeField] private Animator droneAnimator;
        [SerializeField] private string droneAnimationStateName;
        [SerializeField] private bool disableDroneAnimatorWhenStopped = true;
        [SerializeField] private Sprite[] droneAnimationSprites;
        [SerializeField] private float droneAnimationFrameSeconds = 0.08f;
        [SerializeField] private GameObject choicePanel;
        [SerializeField] private Button firstChoiceButton;
        [SerializeField] private Button secondChoiceButton;
        [SerializeField] private CanvasGroup panelCanvasGroup;
        [SerializeField] private CanvasGroup choiceCanvasGroup;
        [SerializeField] private float fadeSeconds = 0.25f;
        [SerializeField] private float droneMoveSeconds = 0.55f;
        [SerializeField] private float itemSpitSeconds = 0.25f;
        [SerializeField] private float droneStartY = 771f;
        [SerializeField] private float droneLandingY = 28f;
        [SerializeField] private Sprite clockSprite;
        [SerializeField] private Sprite downgradeSprite;
        [SerializeField] private Sprite extraMagazineSprite;
        [SerializeField] private Sprite autoPassHumanSprite;
        [SerializeField] private Sprite blankBulletSprite;
        [SerializeField] private Sprite bonusChoiceSprite;
        [SerializeField] private Sprite heartSprite;
        [SerializeField] private Sprite theftSprite;
        [SerializeField] private AudioClip droneMusicClip;
        [SerializeField, Range(0f, 1f)] private float droneMusicVolume = 1f;
        [SerializeField] private BgmPlayer bgmPlayer;
        [SerializeField] private AudioClip getItemClip;
        [SerializeField, Range(0f, 1f)] private float getItemVolume = 1f;

        private readonly RewardItemType[] allRewards =
        {
            RewardItemType.Clock,
            RewardItemType.Downgrade,
            RewardItemType.ExtraMagazine,
            RewardItemType.AutoPassHuman,
            RewardItemType.BlankBullet,
            RewardItemType.BonusChoice,
            RewardItemType.Heart,
            RewardItemType.Theft
        };

        private bool droneClicked;
        private bool rewardSelected;
        private RewardItemType selectedReward;
        private Vector2 droneShownPosition;
        private Coroutine droneAnimationRoutine;
        private Sprite defaultDroneSprite;

#if UNITY_EDITOR
        private void OnValidate()
        {
            ResolveDroneAnimationSpritesFromAssets();
        }
#endif

        public IEnumerator Play(System.Action<RewardItemType> onSelected)
        {
            ResolveMissingReferences();

            if (rewardPanel == null || droneRoot == null || droneButton == null || firstChoiceButton == null || secondChoiceButton == null)
            {
                yield break;
            }

            PlayDroneMusic();
            RegisterDroneButton();
            rewardSelected = false;
            droneClicked = false;
            droneShownPosition = new Vector2(droneRoot.anchoredPosition.x, droneLandingY);
            Vector2 droneStartPosition = new Vector2(droneShownPosition.x, droneStartY);
            Vector2 droneTextOffset = droneTextRoot != null ? droneTextRoot.anchoredPosition - droneRoot.anchoredPosition : Vector2.zero;
            RectTransform choiceRect = choicePanel != null ? choicePanel.GetComponent<RectTransform>() : null;
            Vector2 choiceShownPosition = choiceRect != null ? choiceRect.anchoredPosition : Vector2.zero;

            rewardPanel.SetActive(true);
            SetChoicePanelVisible(false, 0f);
            SetPanelAlpha(0f);
            SetDroneVisualPosition(droneStartPosition, droneTextOffset);
            StartDroneAnimation();
            droneButton.interactable = false;

            yield return Fade(panelCanvasGroup, 0f, 1f, fadeSeconds);
            SetPanelAlpha(1f);
            yield return MoveDrone(droneStartPosition, droneShownPosition, droneMoveSeconds, droneTextOffset);

            droneButton.interactable = true;
            yield return new WaitUntil(() => droneClicked);
            droneButton.interactable = false;

            RewardItemType firstReward;
            RewardItemType secondReward;
            PickTwoRewards(out firstReward, out secondReward);
            BindChoice(firstChoiceButton, firstReward);
            BindChoice(secondChoiceButton, secondReward);

            SetChoicePanelVisible(true, 0f);
            yield return ShowChoicesFromDrone(choiceRect, droneShownPosition, choiceShownPosition);
            yield return new WaitUntil(() => rewardSelected);

            onSelected?.Invoke(selectedReward);

            yield return Fade(choiceCanvasGroup, 1f, 0f, fadeSeconds);
            if (choiceRect != null)
            {
                choiceRect.anchoredPosition = choiceShownPosition;
            }

            SetChoicePanelVisible(false, 0f);
            yield return MoveDrone(droneRoot.anchoredPosition, droneStartPosition, droneMoveSeconds, droneTextOffset);
            yield return Fade(panelCanvasGroup, 1f, 0f, fadeSeconds);

            StopDroneAnimation();
            RestoreBgm();
            rewardPanel.SetActive(false);
        }

        public void HideImmediate()
        {
            ResolveMissingReferences();

            if (firstChoiceButton != null)
            {
                firstChoiceButton.onClick.RemoveAllListeners();
            }

            if (secondChoiceButton != null)
            {
                secondChoiceButton.onClick.RemoveAllListeners();
            }

            if (droneButton != null)
            {
                droneButton.onClick.RemoveListener(HandleDroneClicked);
                droneButton.interactable = false;
            }

            SetChoicePanelVisible(false, 0f);
            SetPanelAlpha(0f);
            StopDroneAnimation();
            RestoreBgm();

            if (rewardPanel != null)
            {
                rewardPanel.SetActive(false);
            }
        }

        private void ResolveMissingReferences()
        {
            if (rewardPanel == null)
            {
                rewardPanel = FindFirstSceneObjectByNames("Drone Reward Panel", "DroneRewardPanel", "Drone Panel", "DronePanel");
            }

            if (panelCanvasGroup == null && rewardPanel != null)
            {
                panelCanvasGroup = GetOrAddCanvasGroup(rewardPanel);
            }

            if (droneRoot == null)
            {
                GameObject droneObject = FindFirstSceneObjectByNames("Drone Button", "DroneButton", "Drone");
                droneRoot = droneObject != null ? droneObject.GetComponent<RectTransform>() : null;
            }

            if (droneTextRoot == null)
            {
                GameObject droneTextObject = FindFirstSceneObjectByNames("Drone Text", "Drone text", "DroneText");
                droneTextRoot = droneTextObject != null ? droneTextObject.GetComponent<RectTransform>() : null;
            }

            if (droneButton == null && droneRoot != null)
            {
                droneButton = droneRoot.GetComponent<Button>();
            }

            if (droneImage == null && droneRoot != null)
            {
                droneImage = droneRoot.GetComponent<Image>();
            }

            if (droneAnimator == null && droneRoot != null)
            {
                droneAnimator = droneRoot.GetComponent<Animator>();

                if (droneAnimator == null)
                {
                    droneAnimator = droneRoot.GetComponentInChildren<Animator>(true);
                }
            }

#if UNITY_EDITOR
            ResolveDroneAnimationSpritesFromAssets();
#endif

            if (choicePanel == null)
            {
                choicePanel = FindFirstSceneObjectByNames("Reward Choice Panel", "RewardChoicePanel", "Item Choice Panel", "ItemChoicePanel");
            }

            if (choiceCanvasGroup == null && choicePanel != null)
            {
                choiceCanvasGroup = GetOrAddCanvasGroup(choicePanel);
            }

            if (firstChoiceButton == null)
            {
                GameObject buttonObject = FindFirstSceneObjectByNames("Item Button 1", "Item Button1", "ItemButton1", "Reward Button 1", "RewardButton1");
                firstChoiceButton = buttonObject != null ? buttonObject.GetComponent<Button>() : null;
            }

            if (secondChoiceButton == null)
            {
                GameObject buttonObject = FindFirstSceneObjectByNames("Item Button 2", "Item Button2", "ItemButton2", "Reward Button 2", "RewardButton2");
                secondChoiceButton = buttonObject != null ? buttonObject.GetComponent<Button>() : null;
            }

            if (bgmPlayer == null)
            {
                bgmPlayer = FindFirstObjectByType<BgmPlayer>();
            }
        }

        private void PlayDroneMusic()
        {
            if (bgmPlayer == null || droneMusicClip == null)
            {
                return;
            }

            bgmPlayer.PlayTemporaryLoop(droneMusicClip, droneMusicVolume);
        }

        private void RestoreBgm()
        {
            if (bgmPlayer != null)
            {
                bgmPlayer.RestoreMainLoop();
            }
        }

        private void StartDroneAnimation()
        {
            StopDroneAnimation();

            if (droneAnimator != null && droneAnimator.runtimeAnimatorController != null)
            {
                droneAnimator.enabled = true;
                droneAnimator.Rebind();
                droneAnimator.Update(0f);

                if (!string.IsNullOrWhiteSpace(droneAnimationStateName))
                {
                    droneAnimator.Play(droneAnimationStateName, 0, 0f);
                }

                return;
            }

            if (droneImage == null || droneAnimationSprites == null || droneAnimationSprites.Length == 0)
            {
                return;
            }

            defaultDroneSprite = droneImage.sprite;
            droneAnimationRoutine = StartCoroutine(PlayDroneAnimation());
        }

        private void StopDroneAnimation()
        {
            if (droneAnimationRoutine != null)
            {
                StopCoroutine(droneAnimationRoutine);
                droneAnimationRoutine = null;
            }

            if (droneImage != null && defaultDroneSprite != null)
            {
                droneImage.sprite = defaultDroneSprite;
            }

            if (droneAnimator != null && disableDroneAnimatorWhenStopped)
            {
                droneAnimator.enabled = false;
            }
        }

        private IEnumerator PlayDroneAnimation()
        {
            int index = 0;

            while (true)
            {
                Sprite frame = GetDroneAnimationFrame(index);

                if (frame != null)
                {
                    droneImage.sprite = frame;
                }

                index++;
                yield return new WaitForSecondsRealtime(GetDroneAnimationFrameSeconds());
            }
        }

        private float GetDroneAnimationFrameSeconds()
        {
            return Mathf.Max(0.01f, droneAnimationFrameSeconds);
        }

        private Sprite GetDroneAnimationFrame(int index)
        {
            if (droneAnimationSprites == null || droneAnimationSprites.Length == 0)
            {
                return null;
            }

            int safeIndex = Mathf.Abs(index) % droneAnimationSprites.Length;
            return droneAnimationSprites[safeIndex];
        }

#if UNITY_EDITOR
        private void ResolveDroneAnimationSpritesFromAssets()
        {
            const int frameCount = 5;
            bool changed = false;

            if (droneAnimationSprites == null || droneAnimationSprites.Length != frameCount)
            {
                droneAnimationSprites = new Sprite[frameCount];
                changed = true;
            }

            for (int i = 0; i < frameCount; i++)
            {
                string path = $"Assets/Image/Drone Anim/dron_ani_{i + 1}.png";
                Sprite sprite = LoadSpriteAssetAtPath(path);

                if (droneAnimationSprites[i] != sprite)
                {
                    droneAnimationSprites[i] = sprite;
                    changed = true;
                }
            }

            if (changed && !Application.isPlaying)
            {
                EditorUtility.SetDirty(this);
            }
        }

        private static Sprite LoadSpriteAssetAtPath(string path)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);

            if (sprite != null)
            {
                return sprite;
            }

            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);

            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] is Sprite childSprite)
                {
                    return childSprite;
                }
            }

            return null;
        }
#endif

        private void RegisterDroneButton()
        {
            droneButton.onClick.RemoveListener(HandleDroneClicked);
            droneButton.onClick.AddListener(HandleDroneClicked);
        }

        private void HandleDroneClicked()
        {
            droneClicked = true;
        }

        private void PickTwoRewards(out RewardItemType firstReward, out RewardItemType secondReward)
        {
            List<RewardItemType> pool = new List<RewardItemType>(allRewards);
            int firstIndex = Random.Range(0, pool.Count);
            firstReward = pool[firstIndex];
            pool.RemoveAt(firstIndex);

            int secondIndex = Random.Range(0, pool.Count);
            secondReward = pool[secondIndex];
        }

        private void BindChoice(Button button, RewardItemType rewardType)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => SelectReward(rewardType));
            button.interactable = true;

            Image icon = FindRewardIcon(button);

            if (icon != null)
            {
                Sprite rewardSprite = GetRewardSprite(rewardType);
                icon.sprite = rewardSprite;
                icon.enabled = rewardSprite != null;
                icon.preserveAspect = true;
            }

            TMP_Text title = FindRewardText(button, "Title", "Item Title", "Name", "Item Name");
            TMP_Text description = FindRewardText(button, "Description", "Desc", "Item Description", "Explain");

            if (title == null || description == null)
            {
                TMP_Text[] labels = button.GetComponentsInChildren<TMP_Text>(true);

                if (title == null && labels.Length > 0)
                {
                    title = labels[0];
                }

                if (description == null && labels.Length > 1)
                {
                    description = labels[1];
                }
            }

            if (title != null)
            {
                title.text = GetRewardTitle(rewardType);
            }

            if (description != null)
            {
                description.text = GetRewardDescription(rewardType);
            }
        }

        private static string GetRewardTitle(RewardItemType rewardType)
        {
            switch (rewardType)
            {
                case RewardItemType.Clock:
                    return "\uc2dc\uacc4";
                case RewardItemType.Downgrade:
                    return "\ub2e4\uc6b4 \uadf8\ub808\uc774\ub4dc";
                case RewardItemType.ExtraMagazine:
                    return "\uc5ec\ubd84 \ud0c4\ucc3d";
                case RewardItemType.AutoPassHuman:
                    return "\uc2ec\uc0ac\uae30";
                case RewardItemType.BlankBullet:
                    return "\uacf5\ud3ec\ud0c4";
                case RewardItemType.BonusChoice:
                    return "\ubf40\ub098\uc2a4~~~!";
                case RewardItemType.Heart:
                    return "\uc2ec\uc7a5";
                case RewardItemType.Theft:
                    return "\ub3c4\ub09c";
                default:
                    return rewardType.ToString();
            }
        }

        private static string GetRewardDescription(RewardItemType rewardType)
        {
            switch (rewardType)
            {
                case RewardItemType.Clock:
                    return "\ucd08 +2";
                case RewardItemType.Downgrade:
                    return "\uc774\uc804 \uc2a4\ud14c\uc774\uc9c0 AI 1\ub9c8\ub9ac \ub4f1\uc7a5";
                case RewardItemType.ExtraMagazine:
                    return "AI \uc22b\uc790\ubcf4\ub2e4 \ucd1d\uc54c +1";
                case RewardItemType.AutoPassHuman:
                    return "\uba40\uc9f1\ud55c \uc778\uac04 1\uba85 \uc790\ub3d9 \ud1b5\uacfc";
                case RewardItemType.BlankBullet:
                    return "\uc0ac\ub78c\uc744 \uc3dc\ub3c4 1\ud68c \ubd88\ubc8c";
                case RewardItemType.BonusChoice:
                    return "\ubcf4\uae09 \uc120\ud0dd\uc9c0 \ud6a8\uacfc";
                case RewardItemType.Heart:
                    return "\uc0dd\uba85 +1";
                case RewardItemType.Theft:
                    return "AI 1\uba85\uacfc \ucd1d\uc54c 1\ubc1c \uc81c\uac70";
                default:
                    return string.Empty;
            }
        }

        private Sprite GetRewardSprite(RewardItemType rewardType)
        {
            switch (rewardType)
            {
                case RewardItemType.Clock:
                    return GetFallbackSprite(clockSprite, 0);
                case RewardItemType.Downgrade:
                    return GetFallbackSprite(downgradeSprite, 1);
                case RewardItemType.ExtraMagazine:
                    return GetFallbackSprite(extraMagazineSprite, 2);
                case RewardItemType.AutoPassHuman:
                    return GetFallbackSprite(autoPassHumanSprite, 3);
                case RewardItemType.BlankBullet:
                    return GetFallbackSprite(blankBulletSprite, 4);
                case RewardItemType.BonusChoice:
                    return GetFallbackSprite(bonusChoiceSprite, 5);
                case RewardItemType.Heart:
                    return GetFallbackSprite(heartSprite, 6);
                case RewardItemType.Theft:
                    return GetFallbackSprite(theftSprite, 7);
                default:
                    return null;
            }
        }

        private Sprite GetFallbackSprite(Sprite preferred, int rewardIndex)
        {
            if (preferred != null)
            {
                return preferred;
            }

            Sprite[] fallbackSprites = { clockSprite, downgradeSprite, extraMagazineSprite };
            List<Sprite> availableSprites = new List<Sprite>();

            for (int i = 0; i < fallbackSprites.Length; i++)
            {
                if (fallbackSprites[i] != null)
                {
                    availableSprites.Add(fallbackSprites[i]);
                }
            }

            return availableSprites.Count > 0 ? availableSprites[rewardIndex % availableSprites.Count] : null;
        }

        private static Image FindRewardIcon(Button button)
        {
            if (button == null)
            {
                return null;
            }

            Image[] images = button.GetComponentsInChildren<Image>(true);

            for (int i = 0; i < images.Length; i++)
            {
                if (images[i] != null && images[i] != button.targetGraphic)
                {
                    string name = images[i].name.ToLowerInvariant();

                    if (name.Contains("icon") || name.Contains("sprite") || name.Contains("item"))
                    {
                        return images[i];
                    }
                }
            }

            for (int i = 0; i < images.Length; i++)
            {
                if (images[i] != null && images[i] != button.targetGraphic)
                {
                    return images[i];
                }
            }

            return null;
        }

        private static TMP_Text FindRewardText(Button button, params string[] names)
        {
            if (button == null)
            {
                return null;
            }

            TMP_Text[] labels = button.GetComponentsInChildren<TMP_Text>(true);

            for (int i = 0; i < labels.Length; i++)
            {
                for (int j = 0; j < names.Length; j++)
                {
                    if (labels[i] != null && labels[i].name.Trim() == names[j])
                    {
                        return labels[i];
                    }
                }
            }

            return null;
        }

        private void SelectReward(RewardItemType rewardType)
        {
            PlayGetItemSound();
            selectedReward = rewardType;
            rewardSelected = true;

            if (firstChoiceButton != null)
            {
                firstChoiceButton.interactable = false;
            }

            if (secondChoiceButton != null)
            {
                secondChoiceButton.interactable = false;
            }
        }

        private void PlayGetItemSound()
        {
            if (getItemClip == null)
            {
                return;
            }

            AudioSource.PlayClipAtPoint(getItemClip, Vector3.zero, getItemVolume);
        }

        private void SetChoicePanelVisible(bool visible, float alpha)
        {
            if (choicePanel != null)
            {
                choicePanel.SetActive(visible);
            }

            if (choiceCanvasGroup != null)
            {
                choiceCanvasGroup.alpha = alpha;
                choiceCanvasGroup.blocksRaycasts = visible;
                choiceCanvasGroup.interactable = visible;
            }
        }

        private void SetPanelAlpha(float alpha)
        {
            if (panelCanvasGroup != null)
            {
                panelCanvasGroup.alpha = alpha;
                panelCanvasGroup.blocksRaycasts = alpha > 0f;
                panelCanvasGroup.interactable = alpha > 0f;
            }
        }

        private IEnumerator MoveDrone(Vector2 from, Vector2 to, float duration, Vector2 droneTextOffset)
        {
            if (droneRoot == null)
            {
                yield break;
            }

            if (duration <= 0f)
            {
                SetDroneVisualPosition(to, droneTextOffset);
                yield break;
            }

            float time = 0f;

            while (time < duration)
            {
                time += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(time / duration));
                SetDroneVisualPosition(Vector2.LerpUnclamped(from, to, t), droneTextOffset);
                yield return null;
            }

            SetDroneVisualPosition(to, droneTextOffset);
        }

        private void SetDroneVisualPosition(Vector2 position, Vector2 droneTextOffset)
        {
            if (droneRoot != null)
            {
                droneRoot.anchoredPosition = position;
            }

            if (droneTextRoot != null)
            {
                droneTextRoot.anchoredPosition = position + droneTextOffset;
            }
        }

        private IEnumerator ShowChoicesFromDrone(RectTransform choiceRect, Vector2 dronePosition, Vector2 shownPosition)
        {
            if (choiceRect == null)
            {
                yield return Fade(choiceCanvasGroup, 0f, 1f, fadeSeconds);
                yield break;
            }

            choiceRect.anchoredPosition = dronePosition;

            if (choiceCanvasGroup != null)
            {
                choiceCanvasGroup.alpha = 0f;
            }

            float duration = Mathf.Max(0.01f, itemSpitSeconds);
            float time = 0f;

            while (time < duration)
            {
                time += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(time / duration));
                choiceRect.anchoredPosition = Vector2.LerpUnclamped(dronePosition, shownPosition, t);

                if (choiceCanvasGroup != null)
                {
                    choiceCanvasGroup.alpha = t;
                }

                yield return null;
            }

            choiceRect.anchoredPosition = shownPosition;

            if (choiceCanvasGroup != null)
            {
                choiceCanvasGroup.alpha = 1f;
            }
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

        private static CanvasGroup GetOrAddCanvasGroup(GameObject target)
        {
            CanvasGroup group = target.GetComponent<CanvasGroup>();

            if (group == null)
            {
                group = target.AddComponent<CanvasGroup>();
            }

            return group;
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
    }
}
