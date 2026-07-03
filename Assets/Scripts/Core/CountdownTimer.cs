using System;
using System.Collections;
using UnityEngine;

namespace Gamejam2026.Core
{
    public sealed class CountdownTimer
    {
        public IEnumerator Run(float seconds, Func<bool> shouldContinue, Action<float> onTick, Action onCompleted)
        {
            float remaining = Mathf.Max(0f, seconds);

            while (remaining > 0f && shouldContinue())
            {
                onTick?.Invoke(remaining);
                remaining -= Time.deltaTime;
                yield return null;
            }

            onTick?.Invoke(0f);

            if (shouldContinue())
            {
                onCompleted?.Invoke();
            }
        }
    }
}
