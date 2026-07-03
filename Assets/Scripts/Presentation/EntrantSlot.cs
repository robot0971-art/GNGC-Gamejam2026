using System.Collections;
using Gamejam2026.Gameplay;
using UnityEngine;

namespace Gamejam2026.Presentation
{
    public class EntrantSlot : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Collider2D hitCollider;
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color dimColor = new Color(0.55f, 0.55f, 0.55f, 1f);
        [SerializeField] private Color shotFlashColor = Color.white;
        [SerializeField] private Color aiFlashColor = new Color(0.25f, 1f, 1f, 1f);
        [SerializeField] private Color humanFlashColor = new Color(1f, 0.9f, 0.15f, 1f);
        [SerializeField] private Color aiDownColor = new Color(0.05f, 0.08f, 0.12f, 1f);
        [SerializeField] private Color humanHitColor = new Color(1f, 0.25f, 0.25f, 1f);
        [SerializeField] private Vector3 focusedScale = new Vector3(1.06f, 1.06f, 1f);
        [SerializeField] private float shotFlashSeconds = 0.04f;
        [SerializeField] private float impactSeconds = 0.1f;
        [SerializeField] private float impactPushX = 0.06f;
        [SerializeField] private float impactRotation = 3f;
        [SerializeField] private Vector3 impactSquashScale = new Vector3(1.03f, 0.97f, 1f);
        [SerializeField] private float aiHoldSeconds = 0.2f;
        [SerializeField] private float aiGlitchSeconds = 0.28f;
        [SerializeField] private float aiFadeSeconds = 0.18f;
        [SerializeField] private float glitchOffsetX = 0.045f;
        [SerializeField] private float glitchFlickerRate = 38f;
        [SerializeField] private bool normalizeSpriteHeight = true;
        [SerializeField] private bool useFixedAiSlotsForTesting;

        private Vector3 originalSlotScale;
        private float targetWorldHeight;
        private Vector3 baseScale;
        private Vector3 basePosition;
        private Quaternion baseRotation;
        private Coroutine shotRoutine;

        public EntrantData Data { get; private set; }
        public bool IsShot { get; private set; }
        public bool IsTargetAI => IsAITarget();

        private void Awake()
        {
            baseScale = transform.localScale;
            originalSlotScale = transform.localScale;
            basePosition = transform.localPosition;
            baseRotation = transform.localRotation;

            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            if (hitCollider == null)
            {
                hitCollider = GetComponent<Collider2D>();
            }

            targetWorldHeight = GetCurrentSpriteWorldHeight();
        }

        public void Setup(EntrantData data)
        {
            Data = data;
            IsShot = false;

            basePosition = transform.localPosition;
            baseRotation = transform.localRotation;

            if (shotRoutine != null)
            {
                StopCoroutine(shotRoutine);
                shotRoutine = null;
            }

            if (spriteRenderer != null && data.sprite != null)
            {
                spriteRenderer.enabled = true;
                spriteRenderer.sprite = data.sprite;
                spriteRenderer.color = normalColor;
                baseScale = CalculateNormalizedScale(data.sprite);
            }
            else if (spriteRenderer != null)
            {
                spriteRenderer.enabled = true;
                spriteRenderer.color = normalColor;
                baseScale = originalSlotScale;
            }

            if (hitCollider != null)
            {
                hitCollider.enabled = true;
            }

            transform.localScale = baseScale;
            transform.localPosition = basePosition;
            transform.localRotation = baseRotation;
            gameObject.SetActive(true);
        }

        private Vector3 CalculateNormalizedScale(Sprite sprite)
        {
            if (!normalizeSpriteHeight || sprite == null || sprite.bounds.size.y <= 0f || targetWorldHeight <= 0f)
            {
                return originalSlotScale;
            }

            float normalizedY = targetWorldHeight / sprite.bounds.size.y;
            float ratio = Mathf.Abs(originalSlotScale.y) <= Mathf.Epsilon
                ? 1f
                : normalizedY / Mathf.Abs(originalSlotScale.y);

            return new Vector3(
                originalSlotScale.x * ratio,
                Mathf.Sign(originalSlotScale.y) * normalizedY,
                originalSlotScale.z);
        }

        private float GetCurrentSpriteWorldHeight()
        {
            if (spriteRenderer == null || spriteRenderer.sprite == null)
            {
                return Mathf.Abs(transform.localScale.y);
            }

            return spriteRenderer.sprite.bounds.size.y * Mathf.Abs(transform.localScale.y);
        }

        public void SetFocused(bool focused)
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.color = focused ? normalColor : dimColor;
            }

            transform.localScale = focused ? Vector3.Scale(baseScale, focusedScale) : baseScale;
        }

        public void Clear()
        {
            Data = null;
            IsShot = false;

            if (spriteRenderer != null)
            {
                spriteRenderer.sprite = null;
            }

            if (hitCollider != null)
            {
                hitCollider.enabled = false;
            }
        }

        public void MarkShot()
        {
            IsShot = true;

            if (hitCollider != null)
            {
                hitCollider.enabled = false;
            }

            if (shotRoutine != null)
            {
                StopCoroutine(shotRoutine);
            }

            shotRoutine = StartCoroutine(PlayShotReaction());
        }

        private IEnumerator PlayShotReaction()
        {
            bool isAI = IsAITarget();

            if (spriteRenderer != null)
            {
                spriteRenderer.color = shotFlashColor;
            }

            yield return PlayImpactKick(isAI);

            if (isAI)
            {
                yield return PlayAiDisappear();
            }
            else if (spriteRenderer != null)
            {
                spriteRenderer.color = humanHitColor;
            }
        }

        private IEnumerator PlayAiDisappear()
        {
            if (spriteRenderer == null)
            {
                yield break;
            }

            transform.localPosition = basePosition;
            transform.localRotation = baseRotation;
            transform.localScale = baseScale;
            spriteRenderer.color = aiFlashColor;

            yield return new WaitForSeconds(aiHoldSeconds);

            float time = 0f;
            while (time < aiGlitchSeconds)
            {
                time += Time.deltaTime;
                float t = Mathf.Clamp01(time / aiGlitchSeconds);
                float offsetSign = Mathf.PingPong(time * glitchFlickerRate, 1f) > 0.5f ? 1f : -1f;
                float offset = offsetSign * glitchOffsetX * (1f - t);
                float blink = Mathf.PingPong(time * glitchFlickerRate, 1f);

                transform.localPosition = basePosition + new Vector3(offset, 0f, 0f);
                spriteRenderer.color = Color.Lerp(aiFlashColor, aiDownColor, t);
                spriteRenderer.enabled = blink > 0.25f;

                yield return null;
            }

            transform.localPosition = basePosition;
            spriteRenderer.enabled = true;

            time = 0f;
            Color startColor = aiDownColor;

            while (time < aiFadeSeconds)
            {
                time += Time.deltaTime;
                float t = Mathf.Clamp01(time / aiFadeSeconds);
                Color faded = Color.Lerp(startColor, new Color(startColor.r, startColor.g, startColor.b, 0f), t);
                spriteRenderer.color = faded;
                yield return null;
            }

            gameObject.SetActive(false);
        }

        private IEnumerator PlayImpactKick(bool isAI)
        {
            float time = 0f;
            float direction = transform.position.x < 0f ? -1f : 1f;
            Vector3 pushedPosition = basePosition + new Vector3(direction * impactPushX, 0f, 0f);
            Quaternion pushedRotation = baseRotation * Quaternion.Euler(0f, 0f, direction * impactRotation);

            while (time < impactSeconds)
            {
                time += Time.deltaTime;
                float t = Mathf.Clamp01(time / impactSeconds);
                float punch = Mathf.Sin(t * Mathf.PI);

                transform.localPosition = Vector3.Lerp(basePosition, pushedPosition, punch);
                transform.localRotation = Quaternion.Lerp(baseRotation, pushedRotation, punch);
                transform.localScale = Vector3.Lerp(baseScale, Vector3.Scale(baseScale, impactSquashScale), punch);

                if (spriteRenderer != null)
                {
                    Color target = isAI ? aiFlashColor : humanFlashColor;
                    float flashT = Mathf.InverseLerp(shotFlashSeconds, impactSeconds, time);
                    spriteRenderer.color = Color.Lerp(shotFlashColor, target, flashT);
                }

                yield return null;
            }

            transform.localPosition = basePosition;
            transform.localRotation = baseRotation;
            transform.localScale = baseScale;

            if (spriteRenderer != null)
            {
                spriteRenderer.color = isAI ? aiFlashColor : humanFlashColor;
            }
        }

        private bool IsAITarget()
        {
            if (useFixedAiSlotsForTesting && Data == null)
            {
                return name == "Charactor2" || name == "Charactor4";
            }

            return Data != null && Data.type == EntrantType.AI;
        }
    }
}
