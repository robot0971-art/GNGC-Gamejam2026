using UnityEngine;

namespace Gamejam2026.Presentation
{
    public class EffectSpawner : MonoBehaviour
    {
        [SerializeField] private ParticleSystem electricSparkPrefab;
        [SerializeField] private GameObject electricBoltPrefab;

        public void PlayElectricHit(Vector3 position)
        {
            if (electricSparkPrefab != null)
            {
                ParticleSystem spark = Instantiate(electricSparkPrefab, position, Quaternion.identity);
                spark.Play();
                Destroy(spark.gameObject, 1.2f);
            }

            if (electricBoltPrefab == null)
            {
                return;
            }

            for (int i = 0; i < 4; i++)
            {
                GameObject bolt = Instantiate(electricBoltPrefab, position, Quaternion.identity);
                Destroy(bolt, 0.12f);
            }
        }
    }
}
