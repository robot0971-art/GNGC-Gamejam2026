using System.Collections;
using System.Collections.Generic;
using Gamejam2026.Gameplay;
using UnityEngine;

namespace Gamejam2026.Presentation
{
    public class WavePresenter : MonoBehaviour
    {
        [SerializeField] private EntrantSlot[] slots;

        private Vector3[] originalLocalPositions;

        public IReadOnlyList<EntrantSlot> Slots => slots;

        public void ShowWave(WaveData wave)
        {
            ResolveSlots();

            for (int i = 0; i < slots.Length; i++)
            {
                if (i < wave.Entrants.Count)
                {
                    slots[i].Setup(wave.Entrants[i]);
                }
                else
                {
                    slots[i].Clear();
                    slots[i].gameObject.SetActive(false);
                }
            }

            CaptureOriginalPositions();
        }

        public void FocusOnly(int index)
        {
            ResolveSlots();

            for (int i = 0; i < slots.Length; i++)
            {
                slots[i].SetFocused(i == index);
            }
        }

        public void ShowOnly(int index)
        {
            ResolveSlots();

            for (int i = 0; i < slots.Length; i++)
            {
                bool shouldShow = i == index && slots[i].Data != null;
                slots[i].gameObject.SetActive(shouldShow);

                if (shouldShow)
                {
                    slots[i].SetFocused(true);
                }
            }
        }

        public void HideAllEntrants()
        {
            ResolveSlots();
            RestoreOriginalPositions();

            for (int i = 0; i < slots.Length; i++)
            {
                SetSlotAlpha(slots[i], 0f);
                slots[i].gameObject.SetActive(false);
            }
        }

        public IEnumerator PreviewOneAtCenter(int index, Vector3 center, float fadeSeconds, float holdSeconds)
        {
            ResolveSlots();

            if (index < 0 || index >= slots.Length || slots[index].Data == null)
            {
                yield break;
            }

            HideAllEntrants();

            EntrantSlot slot = slots[index];
            Vector3 targetPosition = center;
            targetPosition.z = slot.transform.position.z;

            slot.gameObject.SetActive(true);
            slot.transform.position = targetPosition;
            slot.SetFocused(true);
            SetSlotAlpha(slot, 0f);

            yield return FadeSlot(slot, 0f, 1f, fadeSeconds);
            yield return new WaitForSeconds(holdSeconds);
            yield return FadeSlot(slot, 1f, 0f, fadeSeconds);

            slot.gameObject.SetActive(false);
            RestoreSlotPosition(index);
        }

        public void ShowAllEntrants()
        {
            ResolveSlots();
            RestoreOriginalPositions();

            for (int i = 0; i < slots.Length; i++)
            {
                bool shouldShow = slots[i].Data != null;
                slots[i].gameObject.SetActive(shouldShow);

                if (shouldShow)
                {
                    SetSlotAlpha(slots[i], 1f);
                    slots[i].SetFocused(true);
                }
            }
        }

        public void FocusAll()
        {
            ResolveSlots();

            for (int i = 0; i < slots.Length; i++)
            {
                slots[i].SetFocused(true);
            }
        }

        public int CountRemainingAI()
        {
            ResolveSlots();

            int count = 0;

            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i].Data != null && slots[i].IsTargetAI && !slots[i].IsShot)
                {
                    count++;
                }
            }

            return count;
        }

        public int CountShotHumans()
        {
            ResolveSlots();

            int count = 0;

            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i].Data != null && slots[i].Data.type == EntrantType.Human && slots[i].IsShot)
                {
                    count++;
                }
            }

            return count;
        }

        public bool TryGetFirstUnshotHuman(out EntrantSlot humanSlot)
        {
            ResolveSlots();

            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i].Data != null && slots[i].Data.type == EntrantType.Human && !slots[i].IsShot)
                {
                    humanSlot = slots[i];
                    return true;
                }
            }

            humanSlot = null;
            return false;
        }

        public Vector3 GetCenter()
        {
            ResolveSlots();

            if (slots.Length == 0)
            {
                return transform.position;
            }

            Vector3 total = Vector3.zero;
            int activeCount = 0;

            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i].gameObject.activeSelf)
                {
                    total += slots[i].transform.position;
                    activeCount++;
                }
            }

            return activeCount > 0 ? total / activeCount : transform.position;
        }

        public bool TryGetVisibleSlotByHorizontalRatio(float ratio, out EntrantSlot slot)
        {
            ResolveSlots();

            ratio = Mathf.Clamp01(ratio);
            int visibleCount = 0;

            for (int i = 0; i < slots.Length; i++)
            {
                if (IsVisibleEntrant(slots[i]))
                {
                    visibleCount++;
                }
            }

            if (visibleCount == 0)
            {
                slot = null;
                return false;
            }

            int targetVisibleIndex = Mathf.Clamp(Mathf.FloorToInt(ratio * visibleCount), 0, visibleCount - 1);
            int currentVisibleIndex = 0;

            for (int i = 0; i < slots.Length; i++)
            {
                if (!IsVisibleEntrant(slots[i]))
                {
                    continue;
                }

                if (currentVisibleIndex == targetVisibleIndex)
                {
                    slot = slots[i];
                    return true;
                }

                currentVisibleIndex++;
            }

            slot = null;
            return false;
        }

        private void ResolveSlots()
        {
            if (slots != null && slots.Length > 0)
            {
                return;
            }

            slots = FindObjectsByType<EntrantSlot>(FindObjectsSortMode.None);
            System.Array.Sort(slots, (left, right) => left.transform.position.x.CompareTo(right.transform.position.x));
        }

        private static bool IsVisibleEntrant(EntrantSlot slot)
        {
            return slot != null && slot.Data != null && slot.gameObject.activeInHierarchy;
        }

        private void CaptureOriginalPositions()
        {
            originalLocalPositions = new Vector3[slots.Length];

            for (int i = 0; i < slots.Length; i++)
            {
                originalLocalPositions[i] = slots[i].transform.localPosition;
            }
        }

        private void RestoreOriginalPositions()
        {
            if (originalLocalPositions == null || originalLocalPositions.Length != slots.Length)
            {
                CaptureOriginalPositions();
            }

            for (int i = 0; i < slots.Length; i++)
            {
                RestoreSlotPosition(i);
            }
        }

        private void RestoreSlotPosition(int index)
        {
            if (originalLocalPositions == null || index < 0 || index >= originalLocalPositions.Length)
            {
                return;
            }

            slots[index].transform.localPosition = originalLocalPositions[index];
        }

        private static IEnumerator FadeSlot(EntrantSlot slot, float from, float to, float duration)
        {
            if (slot == null)
            {
                yield break;
            }

            float time = 0f;

            while (time < duration)
            {
                time += Time.deltaTime;
                float t = duration <= 0f ? 1f : Mathf.Clamp01(time / duration);
                SetSlotAlpha(slot, Mathf.Lerp(from, to, t));
                yield return null;
            }

            SetSlotAlpha(slot, to);
        }

        private static void SetSlotAlpha(EntrantSlot slot, float alpha)
        {
            SpriteRenderer spriteRenderer = slot.GetComponent<SpriteRenderer>();

            if (spriteRenderer == null)
            {
                return;
            }

            Color color = spriteRenderer.color;
            color.a = alpha;
            spriteRenderer.color = color;
        }
    }
}
