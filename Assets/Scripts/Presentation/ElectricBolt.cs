using UnityEngine;

namespace Gamejam2026.Presentation
{
    [RequireComponent(typeof(LineRenderer))]
    public class ElectricBolt : MonoBehaviour
    {
        [SerializeField] private float length = 0.55f;
        [SerializeField] private float bend = 0.18f;
        [SerializeField] private float lifeTime = 0.06f;

        private LineRenderer line;
        private float timer;

        private void Awake()
        {
            line = GetComponent<LineRenderer>();
        }

        private void OnEnable()
        {
            timer = 0f;
            GenerateBolt();
        }

        private void Update()
        {
            timer += Time.deltaTime;
            float alpha = Mathf.Clamp01(1f - timer / lifeTime);

            if (line != null)
            {
                Color start = line.startColor;
                Color end = line.endColor;
                start.a = alpha;
                end.a = alpha;
                line.startColor = start;
                line.endColor = end;
            }
        }

        private void GenerateBolt()
        {
            if (line == null)
            {
                return;
            }

            Vector2 direction = Random.insideUnitCircle.normalized;

            if (direction.sqrMagnitude <= 0.001f)
            {
                direction = Vector2.right;
            }

            Vector3 start = transform.position;
            Vector3 end = start + (Vector3)(direction * length);
            Vector3 middle = Vector3.Lerp(start, end, 0.5f) + (Vector3)(Random.insideUnitCircle * bend);

            line.positionCount = 3;
            line.SetPosition(0, start);
            line.SetPosition(1, middle);
            line.SetPosition(2, end);
        }
    }
}
