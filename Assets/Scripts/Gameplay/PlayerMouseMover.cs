using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Gamejam2026.Gameplay
{
    public class PlayerMouseMover : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;
        [SerializeField] private float followSpeed = 18f;
        [SerializeField] private float minX = -6f;
        [SerializeField] private float maxX = 6f;

        private void Awake()
        {
            ResolveCamera();
        }

        private void Update()
        {
            ResolveCamera();

            if (targetCamera == null || !TryGetMousePosition(out Vector2 screenPosition))
            {
                return;
            }

            Vector3 worldPosition = targetCamera.ScreenToWorldPoint(screenPosition);
            Vector3 targetPosition = transform.position;
            targetPosition.x = Mathf.Clamp(worldPosition.x, minX, maxX);

            transform.position = Vector3.Lerp(
                transform.position,
                targetPosition,
                1f - Mathf.Exp(-followSpeed * Time.deltaTime));
        }

        private void ResolveCamera()
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }
        }

        private static bool TryGetMousePosition(out Vector2 screenPosition)
        {
            screenPosition = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
            if (Mouse.current == null)
            {
                return false;
            }

            screenPosition = Mouse.current.position.ReadValue();
            return true;
#elif ENABLE_LEGACY_INPUT_MANAGER
            screenPosition = Input.mousePosition;
            return true;
#else
            return false;
#endif
        }
    }
}
