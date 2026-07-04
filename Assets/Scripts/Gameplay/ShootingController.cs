using System;
using Gamejam2026.Presentation;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Gamejam2026.Gameplay
{
    public class ShootingController : MonoBehaviour
    {
        public event Action<int> ShotFired;
        public event Action<EntrantSlot, bool, int> ShotResolved;
        public event Action BulletsEmpty;

        [SerializeField] private Camera raycastCamera;
        [SerializeField] private Transform aimPoint;
        [SerializeField] private bool unlimitedBulletsForTesting;
        [SerializeField] private bool useFixedAiSlotsForTesting;

        private int bullets;
        private bool canShoot;
        private EntrantSlot overrideTarget;
        private bool blankNextHumanShot;

        public int Bullets => bullets;

        public Transform AimPoint => aimPoint;

        public void SetAimPoint(Transform newAimPoint)
        {
            aimPoint = newAimPoint;
        }

        public void SetOverrideTarget(EntrantSlot target)
        {
            overrideTarget = target;
        }

        public void ClearOverrideTarget()
        {
            overrideTarget = null;
        }

        public void Begin(int bulletCount)
        {
            bullets = Mathf.Max(0, bulletCount);
            canShoot = bullets > 0;
        }

        public void EnableBlankNextHumanShot()
        {
            blankNextHumanShot = true;
        }

        public void DisableBlankNextHumanShot()
        {
            blankNextHumanShot = false;
        }

        public void Stop()
        {
            canShoot = false;
        }

        private void Update()
        {
            if (!canShoot || !TryGetShootInput(out Vector2 screenPosition))
            {
                return;
            }

            TryShootAt(screenPosition);
        }

        private bool TryGetShootInput(out Vector2 screenPosition)
        {
            screenPosition = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                screenPosition = Mouse.current.position.ReadValue();
                return true;
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetMouseButtonDown(0))
            {
                screenPosition = Input.mousePosition;
                return true;
            }
#endif

            return false;
        }

        private void TryShootAt(Vector2 screenPosition)
        {
            Camera cameraToUse = raycastCamera != null ? raycastCamera : Camera.main;

            if (cameraToUse == null)
            {
                return;
            }

            if (overrideTarget != null && !overrideTarget.IsShot)
            {
                Shoot(overrideTarget);
                return;
            }

            Vector2 worldPosition = aimPoint != null
                ? aimPoint.position
                : cameraToUse.ScreenToWorldPoint(screenPosition);
            RaycastHit2D hit = Physics2D.Raycast(worldPosition, Vector2.zero);

            if (hit.collider == null)
            {
                return;
            }

            EntrantSlot slot = hit.collider.GetComponentInParent<EntrantSlot>();

            if (slot == null || slot.IsShot)
            {
                return;
            }

            Shoot(slot);
        }

        private bool TryConsumeBullet()
        {
            if (unlimitedBulletsForTesting)
            {
                ShotFired?.Invoke(bullets);
                return true;
            }

            if (bullets <= 0)
            {
                return false;
            }

            bullets--;
            ShotFired?.Invoke(bullets);
            return true;
        }

        private void Shoot(EntrantSlot slot)
        {
            bool correct = IsAITarget(slot);

            if (!correct && blankNextHumanShot)
            {
                blankNextHumanShot = false;
                ShotFired?.Invoke(bullets);
                return;
            }

            if (!TryConsumeBullet())
            {
                return;
            }

            slot.MarkShot();
            ShotResolved?.Invoke(slot, correct, bullets);

            if (bullets <= 0 && !unlimitedBulletsForTesting)
            {
                canShoot = false;
                BulletsEmpty?.Invoke();
            }
        }

        private bool IsAITarget(EntrantSlot slot)
        {
            if (useFixedAiSlotsForTesting && slot.Data == null)
            {
                return slot.name == "Charactor2" || slot.name == "Charactor4";
            }

            return slot.Data != null && slot.Data.type == EntrantType.AI;
        }
    }
}
