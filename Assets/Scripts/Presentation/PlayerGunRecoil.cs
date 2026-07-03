using System.Collections;
using UnityEngine;

namespace Gamejam2026.Presentation
{
    public class PlayerGunRecoil : MonoBehaviour
    {
        [SerializeField] private Vector3 recoilOffset = new Vector3(0f, -0.18f, 0f);
        [SerializeField] private float kickSeconds = 0.045f;
        [SerializeField] private float returnSeconds = 0.09f;

        private Coroutine recoilRoutine;
        private Vector3 visualOffset;

        public void Play()
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            if (recoilRoutine != null)
            {
                StopCoroutine(recoilRoutine);
            }

            recoilRoutine = StartCoroutine(PlayRoutine());
        }

        private IEnumerator PlayRoutine()
        {
            Vector3 startPosition = transform.localPosition - visualOffset;
            yield return MoveVisualOffset(Vector3.zero, recoilOffset, kickSeconds, startPosition);
            yield return MoveVisualOffset(recoilOffset, Vector3.zero, returnSeconds, startPosition);

            visualOffset = Vector3.zero;
            transform.localPosition = startPosition;
            recoilRoutine = null;
        }

        private IEnumerator MoveVisualOffset(Vector3 from, Vector3 to, float duration, Vector3 basePosition)
        {
            float time = 0f;

            while (time < duration)
            {
                time += Time.deltaTime;
                float t = Mathf.Clamp01(time / duration);
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                visualOffset = Vector3.Lerp(from, to, eased);
                transform.localPosition = basePosition + visualOffset;
                yield return null;
            }

            visualOffset = to;
            transform.localPosition = basePosition + visualOffset;
        }
    }
}
