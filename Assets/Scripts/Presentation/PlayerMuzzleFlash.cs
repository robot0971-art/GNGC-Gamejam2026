using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Gamejam2026.Presentation
{
    public class PlayerMuzzleFlash : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameObject muzzleFlashPrefab;
        [SerializeField] private Transform muzzlePoint;

        [Header("Shape")]
        [FormerlySerializedAs("localScale")]
        [SerializeField] private Vector3 effectScale = Vector3.one;
        [SerializeField] private bool inheritMuzzlePointScale;
        [SerializeField] private float particleSizeMultiplier = 1f;

        [Header("Render")]
        [SerializeField] private float lifetime = 0.25f;
        [SerializeField] private int sortingOrder = 100;

        [Header("Pooling")]
        [SerializeField] private int poolSize = 3;
        [SerializeField] private bool allowPoolExpansion = true;

        private readonly List<PooledMuzzleFlash> pool = new List<PooledMuzzleFlash>();
        private int nextPoolIndex;

        private void Awake()
        {
            WarmPool();
        }

        public void Play()
        {
            if (!isActiveAndEnabled || muzzleFlashPrefab == null)
            {
                return;
            }

            Transform spawnPoint = muzzlePoint != null ? muzzlePoint : transform;
            PooledMuzzleFlash pooledEffect = GetEffectFromPool();

            if (pooledEffect == null)
            {
                return;
            }

            Transform effect = pooledEffect.Transform;
            effect.position = spawnPoint.position;
            effect.rotation = spawnPoint.rotation;
            effect.localScale = GetSpawnScale(spawnPoint);
            effect.gameObject.SetActive(true);
            ForceVisibleIn2D(effect);
            pooledEffect.Play(lifetime);
        }

        private void WarmPool()
        {
            if (muzzleFlashPrefab == null)
            {
                return;
            }

            int targetSize = Mathf.Max(1, poolSize);

            while (pool.Count < targetSize)
            {
                CreatePooledEffect();
            }
        }

        private PooledMuzzleFlash GetEffectFromPool()
        {
            WarmPool();

            for (int i = 0; i < pool.Count; i++)
            {
                int index = (nextPoolIndex + i) % pool.Count;

                if (!pool[index].IsPlaying)
                {
                    nextPoolIndex = (index + 1) % pool.Count;
                    return pool[index];
                }
            }

            if (!allowPoolExpansion)
            {
                PooledMuzzleFlash reused = pool[nextPoolIndex];
                nextPoolIndex = (nextPoolIndex + 1) % pool.Count;
                return reused;
            }

            PooledMuzzleFlash expanded = CreatePooledEffect();
            nextPoolIndex = 0;
            return expanded;
        }

        private PooledMuzzleFlash CreatePooledEffect()
        {
            GameObject instance = Instantiate(muzzleFlashPrefab, transform);
            instance.name = $"{muzzleFlashPrefab.name}_Pooled";
            instance.SetActive(false);

            PooledMuzzleFlash pooledEffect = instance.AddComponent<PooledMuzzleFlash>();
            pool.Add(pooledEffect);
            return pooledEffect;
        }

        private Vector3 GetSpawnScale(Transform spawnPoint)
        {
            if (!inheritMuzzlePointScale || spawnPoint == null)
            {
                return effectScale;
            }

            return Vector3.Scale(effectScale, spawnPoint.lossyScale);
        }

        private void ForceVisibleIn2D(Transform effect)
        {
            ParticleSystemRenderer[] particleRenderers = effect.GetComponentsInChildren<ParticleSystemRenderer>(true);

            for (int i = 0; i < particleRenderers.Length; i++)
            {
                particleRenderers[i].sortingLayerName = "Default";
                particleRenderers[i].sortingOrder = sortingOrder;
            }

            ParticleSystem[] particleSystems = effect.GetComponentsInChildren<ParticleSystem>(true);

            for (int i = 0; i < particleSystems.Length; i++)
            {
                ParticleSystem.MainModule main = particleSystems[i].main;
                main.startSizeMultiplier *= particleSizeMultiplier;
                particleSystems[i].Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
                particleSystems[i].Play(false);
            }
        }

        private sealed class PooledMuzzleFlash : MonoBehaviour
        {
            private float disableTime;

            public Transform Transform => transform;
            public bool IsPlaying { get; private set; }

            public void Play(float lifetime)
            {
                IsPlaying = true;
                disableTime = Time.unscaledTime + lifetime;
                gameObject.SetActive(true);
            }

            private void Update()
            {
                if (!IsPlaying || Time.unscaledTime < disableTime)
                {
                    return;
                }

                IsPlaying = false;
                gameObject.SetActive(false);
            }
        }
    }
}
