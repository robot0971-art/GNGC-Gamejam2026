using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gamejam2026.Presentation
{
    public class CameraDirector : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;
        [SerializeField] private float focusSize = 2.8f;
        [SerializeField] private float overviewSize = 5.5f;
        [SerializeField] private Vector3 focusOffset = new Vector3(0f, 0.4f, -10f);
        [SerializeField] private Vector3 overviewOffset = new Vector3(0f, 0.25f, -10f);
        [SerializeField] private SpriteRenderer cameraBoundsSource;
        [SerializeField] private bool clampInsideBounds = true;

        public IEnumerator PreviewSlots(
            IReadOnlyList<EntrantSlot> slots,
            float holdSeconds,
            float panSeconds,
            System.Action<int> focusedSlotChanged = null)
        {
            ResolveCamera();

            if (targetCamera == null || slots.Count == 0)
            {
                yield break;
            }

            float clampedFocusSize = ClampSizeToBounds(focusSize);
            targetCamera.orthographicSize = clampedFocusSize;

            for (int i = 0; i < slots.Count; i++)
            {
                if (!slots[i].gameObject.activeSelf)
                {
                    continue;
                }

                focusedSlotChanged?.Invoke(i);
                Vector3 focusPosition = GetFocusPosition(slots[i].transform);

                if (i == 0)
                {
                    targetCamera.transform.position = ClampPositionToBounds(focusPosition, clampedFocusSize);
                }
                else
                {
                    yield return MoveCamera(focusPosition, clampedFocusSize, panSeconds);
                }

                yield return new WaitForSeconds(holdSeconds);
            }
        }

        public IEnumerator ShowOverview(Vector3 center, float duration)
        {
            ResolveCamera();

            Vector3 targetPosition = center + overviewOffset;
            targetPosition.z = overviewOffset.z;
            yield return MoveCamera(targetPosition, ClampSizeToBounds(overviewSize), duration);
        }

        public IEnumerator ShowFocusedPoint(Vector3 center, float duration)
        {
            ResolveCamera();

            if (targetCamera == null)
            {
                yield break;
            }

            yield return MoveCamera(GetFocusPosition(center), ClampSizeToBounds(focusSize), duration);
        }

        public Coroutine FocusTarget(Transform target, float duration)
        {
            ResolveCamera();

            if (targetCamera == null || target == null)
            {
                return null;
            }

            return StartCoroutine(MoveCamera(GetFocusPosition(target), ClampSizeToBounds(focusSize), duration));
        }

        public Coroutine FocusPoint(Vector3 worldPosition, float duration)
        {
            ResolveCamera();

            if (targetCamera == null)
            {
                return null;
            }

            return StartCoroutine(MoveCamera(GetFocusPosition(worldPosition), ClampSizeToBounds(focusSize), duration));
        }

        public void SetFocusPoint(Vector3 worldPosition)
        {
            ResolveCamera();

            if (targetCamera == null)
            {
                return;
            }

            float clampedFocusSize = ClampSizeToBounds(focusSize);
            targetCamera.orthographicSize = clampedFocusSize;
            targetCamera.transform.position = ClampPositionToBounds(GetFocusPosition(worldPosition), clampedFocusSize);
        }

        public bool TryGetCurrentView(out Vector3 position, out float size)
        {
            ResolveCamera();

            if (targetCamera == null)
            {
                position = Vector3.zero;
                size = 0f;
                return false;
            }

            position = targetCamera.transform.position;
            size = targetCamera.orthographicSize;
            return true;
        }

        public Coroutine MoveToView(Vector3 position, float size, float duration)
        {
            ResolveCamera();

            if (targetCamera == null)
            {
                return null;
            }

            return StartCoroutine(MoveCamera(position, size, duration));
        }

        public Vector3 ScreenToWorldPoint(Vector2 screenPosition)
        {
            ResolveCamera();

            if (targetCamera == null)
            {
                return Vector3.zero;
            }

            Vector3 worldPosition = targetCamera.ScreenToWorldPoint(screenPosition);
            worldPosition.z = 0f;
            return worldPosition;
        }

        public Vector3 ClampWorldPointToBounds(Vector3 worldPosition)
        {
            ResolveCamera();

            if (!clampInsideBounds || cameraBoundsSource == null)
            {
                return worldPosition;
            }

            Bounds bounds = cameraBoundsSource.bounds;
            worldPosition.x = Mathf.Clamp(worldPosition.x, bounds.min.x, bounds.max.x);
            worldPosition.y = Mathf.Clamp(worldPosition.y, bounds.min.y, bounds.max.y);
            return worldPosition;
        }

        public IEnumerator Shake(float duration, float strength)
        {
            ResolveCamera();

            if (targetCamera == null)
            {
                yield break;
            }

            Vector3 origin = targetCamera.transform.position;
            float time = 0f;

            while (time < duration)
            {
                time += Time.deltaTime;
                Vector2 offset = Random.insideUnitCircle * strength;
                targetCamera.transform.position = origin + new Vector3(offset.x, offset.y, 0f);
                yield return null;
            }

            targetCamera.transform.position = origin;
        }

        private void ResolveCamera()
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            if (cameraBoundsSource == null)
            {
                GameObject background = GameObject.Find("BackGround");
                if (background != null)
                {
                    cameraBoundsSource = background.GetComponent<SpriteRenderer>();
                }
            }
        }

        private Vector3 GetFocusPosition(Transform target)
        {
            return GetFocusPosition(target.position);
        }

        private Vector3 GetFocusPosition(Vector3 worldPosition)
        {
            Vector3 position = worldPosition + focusOffset;
            position.z = focusOffset.z;
            return position;
        }

        private IEnumerator MoveCamera(Vector3 targetPosition, float targetSize, float duration)
        {
            Vector3 startPosition = targetCamera.transform.position;
            float startSize = targetCamera.orthographicSize;
            targetSize = ClampSizeToBounds(targetSize);
            float time = 0f;

            while (time < duration)
            {
                time += Time.deltaTime;
                float t = Mathf.Clamp01(time / duration);
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                float currentSize = ClampSizeToBounds(Mathf.Lerp(startSize, targetSize, eased));
                Vector3 currentPosition = Vector3.Lerp(startPosition, targetPosition, eased);

                targetCamera.orthographicSize = currentSize;
                targetCamera.transform.position = ClampPositionToBounds(currentPosition, currentSize);
                yield return null;
            }

            targetCamera.orthographicSize = targetSize;
            targetCamera.transform.position = ClampPositionToBounds(targetPosition, targetSize);
        }

        private float ClampSizeToBounds(float cameraSize)
        {
            if (!clampInsideBounds || cameraBoundsSource == null || targetCamera == null)
            {
                return cameraSize;
            }

            Bounds bounds = cameraBoundsSource.bounds;
            float maxSizeByHeight = bounds.size.y * 0.5f;
            float maxSizeByWidth = bounds.size.x / (targetCamera.aspect * 2f);
            float maxSize = Mathf.Min(maxSizeByHeight, maxSizeByWidth);

            return Mathf.Min(cameraSize, maxSize);
        }

        private Vector3 ClampPositionToBounds(Vector3 position, float cameraSize)
        {
            if (!clampInsideBounds || cameraBoundsSource == null || targetCamera == null)
            {
                return position;
            }

            Bounds bounds = cameraBoundsSource.bounds;
            float halfHeight = cameraSize;
            float halfWidth = cameraSize * targetCamera.aspect;
            float minX = bounds.min.x + halfWidth;
            float maxX = bounds.max.x - halfWidth;
            float minY = bounds.min.y + halfHeight;
            float maxY = bounds.max.y - halfHeight;

            if (minX > maxX)
            {
                position.x = bounds.center.x;
            }
            else
            {
                position.x = Mathf.Clamp(position.x, minX, maxX);
            }

            if (minY > maxY)
            {
                position.y = bounds.center.y;
            }
            else
            {
                position.y = Mathf.Clamp(position.y, minY, maxY);
            }

            return position;
        }
    }
}
