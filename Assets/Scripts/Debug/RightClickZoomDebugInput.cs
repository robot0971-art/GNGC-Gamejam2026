using System.Collections;
using Gamejam2026.Gameplay;
using Gamejam2026.Presentation;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Gamejam2026.DebugTools
{
    public class RightClickZoomDebugInput : MonoBehaviour
    {
        [SerializeField] private CameraDirector cameraDirector;
        [SerializeField] private WavePresenter wavePresenter;
        [SerializeField] private ShootingController shootingController;
        [SerializeField] private Transform aimPoint;
        [SerializeField] private Transform sniperAimPoint;
        [SerializeField] private GameObject playerObject;
        [SerializeField] private GameObject playerSniperObject;
        [SerializeField] private GameObject sniperMaskObject;
        [SerializeField] private GameObject sniperSceneObject;
        [SerializeField] private GameObject[] sniperSceneObjects;
        [SerializeField] private GameObject cutsceneBackdropObject;
        [SerializeField] private float zoomSeconds = 0.15f;
        [SerializeField] private float transitionFadeSeconds = 0.35f;
        [SerializeField] private float cutsceneBlackHoldSeconds = 0.15f;
        [SerializeField] private float sniperSceneSeconds = 2.5f;
        [SerializeField] private bool followMouseWhileZoomed = true;
        [SerializeField] private bool selectTargetWithADKeys = true;
        [SerializeField] private bool movePlayerSniperWithMouse;
        [SerializeField] private float playerSniperFollowSpeed = 18f;
        [SerializeField] private Vector3 sniperSlotAimOffset = new Vector3(0f, 0.4f, 0f);
        [SerializeField] private bool disableRightClickToggle = true;

        private bool isZoomed;
        private bool isTransitioning;
        private bool persistentZoom;
        private Vector3 followTarget;
        private Vector3 savedPosition;
        private float savedSize;
        private Vector3 playerSniperBasePosition;
        private Vector3 initialPlayerSniperPosition;
        private Vector3 initialSniperMaskPosition;
        private bool hasInitialPlayerSniperPosition;
        private bool hasInitialSniperMaskPosition;
        private bool hasPlayedSniperScene;
        private Coroutine transitionRoutine;
        private EntrantSlot currentSniperTarget;

        private void Awake()
        {
            ResolveMissingReferences();
            CacheInitialSniperVisualPositions();
            SetSniperMaskVisible(true);
        }

        private void Update()
        {
            if (!disableRightClickToggle && WasRightClickPressed())
            {
                ToggleZoomMode();
            }

            if (isZoomed && followMouseWhileZoomed)
            {
                FollowTargetInZoomMode();
            }
        }

        private void ToggleZoomMode()
        {
            if (isTransitioning && IsSniperVisualActive())
            {
                ExitZoomMode();
                return;
            }

            if (isZoomed || IsSniperVisualActive())
            {
                ExitZoomMode();
                return;
            }

            if (isTransitioning)
            {
                return;
            }

            EnterZoomMode();
        }

        public void EnterPersistentZoom()
        {
            persistentZoom = true;

            if (isZoomed || isTransitioning)
            {
                SetZoomVisuals(true);
                SetShootingAimPoint(true);
                SetShootingTargetOverride(true);
                SetSniperMaskVisible(true);
                return;
            }

            EnterZoomMode();
        }

        public void ActivatePersistentZoomVisuals()
        {
            ActivatePersistentZoomVisualsAt((Vector3?)null);
        }

        public void ActivatePersistentZoomVisualsAt(Vector3 previewTargetPoint)
        {
            ActivatePersistentZoomVisualsAt((Vector3?)previewTargetPoint);
        }

        public void ActivatePersistentZoomVisualsAt(EntrantSlot targetSlot)
        {
            ActivatePersistentZoomVisualsAt(targetSlot, true);
        }

        private void ActivatePersistentZoomVisualsAt(EntrantSlot targetSlot, bool snapToTarget)
        {
            persistentZoom = true;
            isZoomed = true;
            isTransitioning = false;
            ResolveMissingReferences();
            SetZoomVisuals(true);
            SetShootingAimPoint(true);
            SetSniperMaskVisible(true);
            CachePlayerSniperBasePosition();

            if (targetSlot != null && !targetSlot.IsShot)
            {
                SetCurrentSniperTarget(targetSlot);
                cameraDirector?.SetFocusPoint(followTarget, false);
                MovePlayerSniperToWorldPoint(followTarget, snapToTarget);
            }
            else
            {
                RefreshSniperTargetFromCurrentAim(true);
            }

            SetShootingTargetOverride(true);
        }

        private void ActivatePersistentZoomVisualsAt(Vector3? initialTargetPoint)
        {
            persistentZoom = true;
            isZoomed = true;
            isTransitioning = false;
            ResolveMissingReferences();
            SetZoomVisuals(true);
            SetShootingAimPoint(true);
            SetSniperMaskVisible(true);
            CachePlayerSniperBasePosition();

            if (initialTargetPoint.HasValue)
            {
                currentSniperTarget = null;
                followTarget = GetSniperTargetPoint(initialTargetPoint.Value);
                cameraDirector?.SetFocusPoint(followTarget, false);
                MovePlayerSniperToWorldPoint(followTarget, true);
            }
            else
            {
                RefreshSniperTargetFromCurrentAim(true);
            }

            SetShootingTargetOverride(true);
        }

        public void ShowPersistentZoomVisualsOnly()
        {
            persistentZoom = true;
            isZoomed = false;
            isTransitioning = false;
            ResolveMissingReferences();
            SetZoomVisuals(true);
            SetShootingAimPoint(false);
            SetShootingTargetOverride(false);
            SetSniperMaskVisible(true);
            CachePlayerSniperBasePosition();
        }

        public void ResetSniperVisualsToInitialPosition()
        {
            ResolveMissingReferences();
            CacheInitialSniperVisualPositions();

            if (hasInitialPlayerSniperPosition && playerSniperObject != null)
            {
                playerSniperObject.transform.position = initialPlayerSniperPosition;
            }

            if (hasInitialSniperMaskPosition && sniperMaskObject != null)
            {
                sniperMaskObject.transform.position = initialSniperMaskPosition;
            }

            CachePlayerSniperBasePosition();
        }

        public void HideZoomVisualsForIntro()
        {
            isZoomed = false;
            isTransitioning = false;
            ResolveMissingReferences();
            SetZoomVisuals(false);
            SetShootingAimPoint(false);
            SetShootingTargetOverride(false);
        }

        public IEnumerator PlayIntroScenesOnce()
        {
            if (hasPlayedSniperScene)
            {
                yield break;
            }

            yield return PlaySniperScenesRoutine();
            hasPlayedSniperScene = true;
        }

        private void EnterZoomMode()
        {
            ResolveMissingReferences();

            if (cameraDirector == null || aimPoint == null)
            {
                return;
            }

            if (!cameraDirector.TryGetCurrentView(out savedPosition, out savedSize))
            {
                return;
            }

            RaycastHit2D hit = Physics2D.Raycast(aimPoint.position, Vector2.zero);
            Vector3 focusPosition = aimPoint.position;

            if (hit.collider != null)
            {
                EntrantSlot slot = hit.collider.GetComponentInParent<EntrantSlot>();

                if (slot != null)
                {
                    focusPosition = slot.transform.position;
                }
            }

            followTarget = focusPosition;

            if (transitionRoutine != null)
            {
                StopCoroutine(transitionRoutine);
                transitionRoutine = null;
                isTransitioning = false;
            }

            transitionRoutine = StartCoroutine(EnterZoomRoutine(focusPosition));
        }

        private void ExitZoomMode()
        {
            if (persistentZoom)
            {
                SetZoomVisuals(true);
                SetShootingAimPoint(true);
                SetShootingTargetOverride(true);
                SetSniperMaskVisible(true);
                return;
            }

            if (cameraDirector == null)
            {
                isZoomed = false;
                return;
            }

            isZoomed = false;
            isTransitioning = false;

            if (transitionRoutine != null)
            {
                StopCoroutine(transitionRoutine);
                transitionRoutine = null;
            }

            if (sniperSceneObject != null)
            {
                sniperSceneObject.SetActive(false);
            }

            SetSniperScenesActive(false);

            SetZoomVisuals(false);
            SetShootingAimPoint(false);
            SetShootingTargetOverride(false);
            SetSniperMaskVisible(true);
            cameraDirector.MoveToView(savedPosition, savedSize, zoomSeconds);
        }

        private IEnumerator EnterZoomRoutine(Vector3 focusPosition)
        {
            isTransitioning = true;

            if (!hasPlayedSniperScene)
            {
                yield return PlaySniperScenesRoutine();
                hasPlayedSniperScene = true;
            }

            isZoomed = true;
            SetZoomVisuals(true);
            SetShootingAimPoint(true);
            SetShootingTargetOverride(true);
            SetSniperMaskVisible(true);
            CachePlayerSniperBasePosition();
            RefreshSniperTargetFromCurrentAim(true);
            ApplyCurrentSniperTargetView(false);
            isTransitioning = false;
            transitionRoutine = null;
        }

        private void FollowTargetInZoomMode()
        {
            if (!RefreshSniperTarget() || cameraDirector == null)
            {
                return;
            }

            cameraDirector.SetFocusPoint(followTarget, false);
            MovePlayerSniperToWorldPoint(followTarget, false);
            SetShootingTargetOverride(true);
        }

        private bool RefreshSniperTarget()
        {
            return selectTargetWithADKeys
                ? RefreshSniperTargetFromKeyboard()
                : RefreshSniperTargetFromCurrentMouse();
        }

        private bool RefreshSniperTargetFromKeyboard()
        {
            int direction = GetKeyboardTargetDirection();

            if (direction != 0 && TryGetSniperTargetByDirection(direction, out EntrantSlot directionalTarget))
            {
                SetCurrentSniperTarget(directionalTarget);
                return true;
            }

            if (currentSniperTarget == null)
            {
                return true;
            }

            if (currentSniperTarget.IsShot || !currentSniperTarget.gameObject.activeInHierarchy)
            {
                Vector3 previousTargetPosition = currentSniperTarget.transform.position;

                if (TryGetNearestSniperTarget(previousTargetPosition, out EntrantSlot nearestTarget))
                {
                    SetCurrentSniperTarget(nearestTarget);
                }
                else
                {
                    currentSniperTarget = null;
                    shootingController?.ClearOverrideTarget();
                    return false;
                }
            }
            else
            {
                followTarget = GetSniperTargetPoint(currentSniperTarget);
            }

            return currentSniperTarget != null;
        }

        public void SelectNearestTargetFrom(Vector3 fromPosition)
        {
            if (TryGetNearestSniperTarget(fromPosition, out EntrantSlot nearestTarget))
            {
                SetCurrentSniperTarget(nearestTarget);
                SetShootingTargetOverride(true);
            }
            else
            {
                currentSniperTarget = null;
                shootingController?.ClearOverrideTarget();
            }
        }

        private bool RefreshSniperTargetFromCurrentAim(bool allowFallback)
        {
            EntrantSlot targetSlot = null;

            if (sniperAimPoint != null)
            {
                RaycastHit2D hit = Physics2D.Raycast(sniperAimPoint.position, Vector2.zero);

                if (hit.collider != null)
                {
                    targetSlot = hit.collider.GetComponentInParent<EntrantSlot>();
                }
            }

            if ((targetSlot == null || targetSlot.IsShot) && aimPoint != null)
            {
                RaycastHit2D hit = Physics2D.Raycast(aimPoint.position, Vector2.zero);

                if (hit.collider != null)
                {
                    targetSlot = hit.collider.GetComponentInParent<EntrantSlot>();
                }
            }

            if (targetSlot != null && !targetSlot.IsShot)
            {
                SetCurrentSniperTarget(targetSlot);
                return true;
            }

            if (allowFallback && TryGetFirstSniperTarget(out EntrantSlot firstTarget))
            {
                SetCurrentSniperTarget(firstTarget);
                return true;
            }

            return false;
        }

        private void ApplyCurrentSniperTargetView(bool instant)
        {
            if (currentSniperTarget == null || cameraDirector == null)
            {
                return;
            }

            if (instant)
            {
                cameraDirector.SetFocusPoint(followTarget);
            }
            else
            {
                cameraDirector.FocusPoint(followTarget, zoomSeconds);
            }

            SetShootingTargetOverride(true);
        }

        private bool RefreshSniperTargetFromCurrentMouse()
        {
            if (!TryGetMousePosition(out Vector2 screenPosition))
            {
                return false;
            }

            if (TryGetSniperTarget(screenPosition, out EntrantSlot targetSlot))
            {
                SetCurrentSniperTarget(targetSlot);
            }
            else if (currentSniperTarget != null)
            {
                followTarget = GetSniperTargetPoint(currentSniperTarget);
            }

            return currentSniperTarget != null;
        }

        private void SetCurrentSniperTarget(EntrantSlot targetSlot)
        {
            currentSniperTarget = targetSlot;
            followTarget = GetSniperTargetPoint(targetSlot);
        }

        private Vector3 GetSniperTargetPoint(EntrantSlot targetSlot)
        {
            return targetSlot != null
                ? GetSniperTargetPoint(targetSlot.transform.position)
                : followTarget;
        }

        private Vector3 GetSniperTargetPoint(Vector3 basePoint)
        {
            return basePoint + sniperSlotAimOffset;
        }

        private void ResolveMissingReferences()
        {
            if (cameraDirector == null)
            {
                cameraDirector = FindFirstObjectByType<CameraDirector>();
            }

            if (wavePresenter == null)
            {
                wavePresenter = FindFirstObjectByType<WavePresenter>();
            }

            if (shootingController == null)
            {
                shootingController = FindFirstObjectByType<ShootingController>();
            }

            if (aimPoint == null)
            {
                GameObject foundAimPoint = GameObject.Find("Aim Point");

                if (foundAimPoint != null)
                {
                    aimPoint = foundAimPoint.transform;
                }
            }

            if (sniperAimPoint == null)
            {
                GameObject foundSniperAimPoint = FindSceneObjectByName("Sniper Aim (1)");

                if (foundSniperAimPoint != null)
                {
                    sniperAimPoint = foundSniperAimPoint.transform;
                }
            }

            if (playerObject == null)
            {
                playerObject = GameObject.Find("Player");
            }

            if (playerSniperObject == null)
            {
                playerSniperObject = FindSceneObjectByName("Player Sniper");
            }

            if (sniperMaskObject == null)
            {
                sniperMaskObject = FindSceneObjectByName("Sniper Mask");
            }

            if (sniperSceneObject == null)
            {
                sniperSceneObject = FindSceneObjectByName("Sniper Scene");
            }

            if (sniperSceneObjects == null || sniperSceneObjects.Length == 0)
            {
                GameObject sniperScene1 = FindSceneObjectByName("Sniper Scene1");
                GameObject sniperScene2 = FindSceneObjectByName("Sniper Scene2");

                if (sniperScene1 != null || sniperScene2 != null)
                {
                    sniperSceneObjects = new[] { sniperScene1, sniperScene2 };
                }
                else if (sniperSceneObject != null)
                {
                    sniperSceneObjects = new[] { sniperSceneObject };
                }
            }

            if (cutsceneBackdropObject == null)
            {
                cutsceneBackdropObject = FindSceneObjectByName("Cutscene Backdrop");
            }
        }

        private void SetZoomVisuals(bool zoomed)
        {
            if (playerObject != null)
            {
                playerObject.SetActive(false);
            }

            if (playerSniperObject != null)
            {
                playerSniperObject.SetActive(zoomed);
            }

            SetSniperMaskVisible(zoomed);
        }

        private bool IsSniperVisualActive()
        {
            return playerSniperObject != null && playerSniperObject.activeInHierarchy;
        }

        private void SetSniperMaskVisible(bool visible)
        {
            if (sniperMaskObject != null)
            {
                sniperMaskObject.SetActive(visible);
            }
        }

        private void SetShootingAimPoint(bool zoomed)
        {
            if (shootingController == null)
            {
                return;
            }

            Transform targetAimPoint = zoomed && sniperAimPoint != null ? sniperAimPoint : aimPoint;
            shootingController.SetAimPoint(targetAimPoint);
        }

        private void SetShootingTargetOverride(bool zoomed)
        {
            if (shootingController == null)
            {
                return;
            }

            if (zoomed && currentSniperTarget != null && !currentSniperTarget.IsShot)
            {
                shootingController.SetOverrideTarget(currentSniperTarget);
            }
            else
            {
                shootingController.ClearOverrideTarget();
            }
        }

        private void CachePlayerSniperBasePosition()
        {
            if (playerSniperObject != null)
            {
                playerSniperBasePosition = playerSniperObject.transform.position;
            }
        }

        private void CacheInitialSniperVisualPositions()
        {
            if (!hasInitialPlayerSniperPosition && playerSniperObject != null)
            {
                initialPlayerSniperPosition = playerSniperObject.transform.position;
                hasInitialPlayerSniperPosition = true;
            }

            if (!hasInitialSniperMaskPosition && sniperMaskObject != null)
            {
                initialSniperMaskPosition = sniperMaskObject.transform.position;
                hasInitialSniperMaskPosition = true;
            }
        }

        private bool TryGetSniperTarget(Vector2 screenPosition, out EntrantSlot targetSlot)
        {
            ResolveMissingReferences();

            if (wavePresenter == null)
            {
                targetSlot = null;
                return false;
            }

            float ratio = Screen.width <= 0 ? 0.5f : screenPosition.x / Screen.width;
            return wavePresenter.TryGetVisibleSlotByHorizontalRatio(ratio, out targetSlot);
        }

        private bool TryGetFirstSniperTarget(out EntrantSlot targetSlot)
        {
            ResolveMissingReferences();

            if (wavePresenter == null)
            {
                targetSlot = null;
                return false;
            }

            return wavePresenter.TryGetFirstSelectableSlot(out targetSlot);
        }

        private bool TryGetNearestSniperTarget(Vector3 fromPosition, out EntrantSlot targetSlot)
        {
            ResolveMissingReferences();

            if (wavePresenter == null)
            {
                targetSlot = null;
                return false;
            }

            return wavePresenter.TryGetNearestSelectableSlot(fromPosition, out targetSlot);
        }

        private bool TryGetSniperTargetByDirection(int direction, out EntrantSlot targetSlot)
        {
            ResolveMissingReferences();

            if (wavePresenter == null)
            {
                targetSlot = null;
                return false;
            }

            return wavePresenter.TryGetSelectableSlotByDirection(currentSniperTarget, direction, out targetSlot);
        }

        private void MovePlayerSniperToWorldPoint(Vector3 worldPoint, bool instant)
        {
            bool shouldMoveSniper = selectTargetWithADKeys || movePlayerSniperWithMouse;

            if (!shouldMoveSniper || playerSniperObject == null || sniperAimPoint == null)
            {
                return;
            }

            Vector3 targetWorldPoint = cameraDirector != null
                ? cameraDirector.ClampWorldPointToBounds(worldPoint)
                : worldPoint;
            Vector3 aimOffset = sniperAimPoint.position - playerSniperObject.transform.position;
            Vector3 targetPosition = targetWorldPoint - aimOffset;
            targetPosition.z = playerSniperBasePosition.z;
            Vector3 previousSniperPosition = playerSniperObject.transform.position;

            playerSniperObject.transform.position = instant
                ? targetPosition
                : Vector3.Lerp(
                    playerSniperObject.transform.position,
                    targetPosition,
                    1f - Mathf.Exp(-playerSniperFollowSpeed * Time.deltaTime));

            if (selectTargetWithADKeys && sniperMaskObject != null)
            {
                Vector3 sniperDelta = playerSniperObject.transform.position - previousSniperPosition;
                sniperDelta.z = 0f;
                sniperMaskObject.transform.position += sniperDelta;
            }
        }

        private CanvasGroup GetOrAddCanvasGroup(GameObject target)
        {
            if (target == null)
            {
                return null;
            }

            CanvasGroup canvasGroup = target.GetComponent<CanvasGroup>();

            if (canvasGroup == null)
            {
                canvasGroup = target.AddComponent<CanvasGroup>();
            }

            return canvasGroup;
        }

        private IEnumerator PlaySniperScenesRoutine()
        {
            ResolveMissingReferences();
            HideZoomVisualsForIntro();
            SetSniperScenesActive(false);

            if (sniperSceneObjects == null || sniperSceneObjects.Length == 0)
            {
                yield break;
            }

            GameObject backdrop = GetOrCreateCutsceneBackdrop();
            CanvasGroup backdropCanvasGroup = GetOrAddCanvasGroup(backdrop);

            if (backdrop != null)
            {
                backdrop.SetActive(true);
                backdrop.transform.SetAsFirstSibling();
            }

            if (backdropCanvasGroup != null)
            {
                backdropCanvasGroup.alpha = 1f;
            }

            if (cutsceneBlackHoldSeconds > 0f)
            {
                yield return new WaitForSeconds(cutsceneBlackHoldSeconds);
            }

            GameObject currentScene = GetNextSniperScene(0);

            if (currentScene == null)
            {
                yield break;
            }

            yield return FadeCanvasGroup(currentScene, 0f, 1f, transitionFadeSeconds, true);
            yield return new WaitForSeconds(sniperSceneSeconds);

            for (int i = 1; i < sniperSceneObjects.Length; i++)
            {
                GameObject nextScene = GetNextSniperScene(i);

                if (nextScene == null)
                {
                    continue;
                }

                yield return CrossFadeCanvasGroups(currentScene, nextScene, transitionFadeSeconds);
                yield return new WaitForSeconds(sniperSceneSeconds);
                currentScene = nextScene;
            }

            yield return FadeSceneAndBackdropOut(currentScene, backdrop, transitionFadeSeconds);
        }

        private GameObject GetNextSniperScene(int startIndex)
        {
            if (sniperSceneObjects == null)
            {
                return null;
            }

            for (int i = startIndex; i < sniperSceneObjects.Length; i++)
            {
                if (sniperSceneObjects[i] != null)
                {
                    return sniperSceneObjects[i];
                }
            }

            return null;
        }

        private void SetSniperScenesActive(bool active)
        {
            if (sniperSceneObjects == null)
            {
                return;
            }

            for (int i = 0; i < sniperSceneObjects.Length; i++)
            {
                if (sniperSceneObjects[i] != null)
                {
                    sniperSceneObjects[i].SetActive(active);
                }
            }
        }

        private IEnumerator FadeCanvasGroup(GameObject target, float from, float to, float duration, bool activeAfterStart)
        {
            if (target == null)
            {
                yield break;
            }

            target.SetActive(true);
            CanvasGroup canvasGroup = GetOrAddCanvasGroup(target);

            if (canvasGroup == null)
            {
                yield break;
            }

            canvasGroup.alpha = from;
            float time = 0f;

            while (time < duration)
            {
                time += Time.deltaTime;
                float t = Mathf.Clamp01(time / duration);
                canvasGroup.alpha = Mathf.Lerp(from, to, t);
                yield return null;
            }

            canvasGroup.alpha = to;
            target.SetActive(activeAfterStart);
        }

        private GameObject GetOrCreateCutsceneBackdrop()
        {
            if (cutsceneBackdropObject != null)
            {
                return cutsceneBackdropObject;
            }

            Transform parent = null;

            if (sniperSceneObjects != null)
            {
                for (int i = 0; i < sniperSceneObjects.Length; i++)
                {
                    if (sniperSceneObjects[i] != null)
                    {
                        parent = sniperSceneObjects[i].transform.parent;
                        break;
                    }
                }
            }

            Canvas canvas = parent != null ? parent.GetComponentInParent<Canvas>() : FindFirstObjectByType<Canvas>();

            if (canvas == null)
            {
                return null;
            }

            GameObject backdrop = new GameObject("Cutscene Backdrop", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
            RectTransform rectTransform = backdrop.GetComponent<RectTransform>();
            rectTransform.SetParent(canvas.transform, false);
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            rectTransform.localScale = Vector3.one;

            Image image = backdrop.GetComponent<Image>();
            image.color = Color.black;
            image.raycastTarget = false;

            cutsceneBackdropObject = backdrop;
            return cutsceneBackdropObject;
        }

        private IEnumerator FadeSceneAndBackdropOut(GameObject scene, GameObject backdrop, float duration)
        {
            CanvasGroup sceneCanvasGroup = GetOrAddCanvasGroup(scene);
            CanvasGroup backdropCanvasGroup = GetOrAddCanvasGroup(backdrop);

            if (sceneCanvasGroup == null)
            {
                yield break;
            }

            float time = 0f;

            while (time < duration)
            {
                time += Time.deltaTime;
                float t = duration <= 0f ? 1f : Mathf.Clamp01(time / duration);
                float alpha = Mathf.Lerp(1f, 0f, t);
                sceneCanvasGroup.alpha = alpha;

                if (backdropCanvasGroup != null)
                {
                    backdropCanvasGroup.alpha = alpha;
                }

                yield return null;
            }

            sceneCanvasGroup.alpha = 0f;
            scene.SetActive(false);

            if (backdropCanvasGroup != null)
            {
                backdropCanvasGroup.alpha = 0f;
            }

            if (backdrop != null)
            {
                backdrop.SetActive(false);
            }
        }

        private IEnumerator CrossFadeCanvasGroups(GameObject fromTarget, GameObject toTarget, float duration)
        {
            if (fromTarget == null || toTarget == null)
            {
                yield break;
            }

            fromTarget.SetActive(true);
            toTarget.SetActive(true);
            CanvasGroup fromCanvasGroup = GetOrAddCanvasGroup(fromTarget);
            CanvasGroup toCanvasGroup = GetOrAddCanvasGroup(toTarget);

            if (fromCanvasGroup == null || toCanvasGroup == null)
            {
                yield break;
            }

            fromCanvasGroup.alpha = 1f;
            toCanvasGroup.alpha = 0f;
            float time = 0f;

            while (time < duration)
            {
                time += Time.deltaTime;
                float t = duration <= 0f ? 1f : Mathf.Clamp01(time / duration);
                fromCanvasGroup.alpha = Mathf.Lerp(1f, 0f, t);
                toCanvasGroup.alpha = Mathf.Lerp(0f, 1f, t);
                yield return null;
            }

            fromCanvasGroup.alpha = 0f;
            toCanvasGroup.alpha = 1f;
            fromTarget.SetActive(false);
            toTarget.SetActive(true);
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

        private static bool WasRightClickPressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetMouseButtonDown(1);
#else
            return false;
#endif
        }

        private static int GetKeyboardTargetDirection()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current == null)
            {
                return 0;
            }

            if (Keyboard.current.aKey.wasPressedThisFrame
                || Keyboard.current.wKey.wasPressedThisFrame
                || Keyboard.current.leftArrowKey.wasPressedThisFrame)
            {
                return -1;
            }

            if (Keyboard.current.dKey.wasPressedThisFrame
                || Keyboard.current.rightArrowKey.wasPressedThisFrame)
            {
                return 1;
            }

            return 0;
#elif ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.A)
                || Input.GetKeyDown(KeyCode.W)
                || Input.GetKeyDown(KeyCode.LeftArrow))
            {
                return -1;
            }

            if (Input.GetKeyDown(KeyCode.D)
                || Input.GetKeyDown(KeyCode.RightArrow))
            {
                return 1;
            }

            return 0;
#else
            return 0;
#endif
        }

        private static bool TryGetMousePosition(out Vector2 screenPosition)
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current == null)
            {
                screenPosition = Vector2.zero;
                return false;
            }

            screenPosition = Mouse.current.position.ReadValue();
            return true;
#elif ENABLE_LEGACY_INPUT_MANAGER
            screenPosition = Input.mousePosition;
            return true;
#else
            screenPosition = Vector2.zero;
            return false;
#endif
        }
    }
}
