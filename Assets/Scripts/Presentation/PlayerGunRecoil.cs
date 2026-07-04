using System.Collections;
using UnityEngine;

namespace Gamejam2026.Presentation
{
    public class PlayerGunRecoil : MonoBehaviour
    {
        [SerializeField] private Transform recoilRoot;
        [SerializeField] private Vector3 recoilOffset = new Vector3(0f, 0.3f, 0f);
        [SerializeField] private float kickSeconds = 0.045f;
        [SerializeField] private float returnSeconds = 0.09f;

        private Coroutine recoilRoutine;
        private Vector3 visualOffset;
        private Vector3 appliedOffset;

        private Transform RecoilRoot => recoilRoot != null ? recoilRoot : transform;

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

        private void LateUpdate()
        {
            Transform target = RecoilRoot;
            Vector3 basePosition = target.localPosition - appliedOffset;
            appliedOffset = visualOffset;
            target.localPosition = basePosition + appliedOffset;
        }

        private IEnumerator PlayRoutine()
        {
            yield return MoveVisualOffset(Vector3.zero, recoilOffset, kickSeconds);
            yield return MoveVisualOffset(recoilOffset, Vector3.zero, returnSeconds);

            visualOffset = Vector3.zero;
            recoilRoutine = null;
        }

        private IEnumerator MoveVisualOffset(Vector3 from, Vector3 to, float duration)
        {
            float time = 0f;

            while (time < duration)
            {
                time += Time.deltaTime;
                float t = Mathf.Clamp01(time / duration);
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                visualOffset = Vector3.Lerp(from, to, eased);
                yield return null;
            }

            visualOffset = to;
        }
    }
}
