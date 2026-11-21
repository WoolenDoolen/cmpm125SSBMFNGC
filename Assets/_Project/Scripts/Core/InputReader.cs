using UnityEngine;
using UnityEngine.InputSystem;

namespace RockAndHardPlace
{
    public sealed class InputReader : MonoBehaviour
    {
        [Tooltip("Defaults to the GameManager on this object.")]
        [SerializeField] private GameManager gameManager;

        [Header("Actions (from GameControls)")]
        [SerializeField] private InputActionReference aim;
        [SerializeField] private InputActionReference power;
        [SerializeField] private InputActionReference fire;
        [SerializeField] private InputActionReference upgrade;
        [SerializeField] private InputActionReference restart;

        private void Awake()
        {
            if (gameManager == null) gameManager = GetComponent<GameManager>();
        }

        private void OnEnable() => SetActionsEnabled(true);
        private void OnDisable() => SetActionsEnabled(false);

        private void Update()
        {
            if (gameManager == null) return;
            if (WasPressed(restart))
            {
                gameManager.Restart();
                return;
            }
            gameManager.Aim(Read(aim), Read(power));
            if (WasPressed(upgrade)) gameManager.TryBuyUpgrade();
            if (WasPressed(fire)) gameManager.TryFire();
        }

        private static float Read(InputActionReference reference) =>
            reference != null ? reference.action.ReadValue<float>() : 0;

        private static bool WasPressed(InputActionReference reference) =>
            reference != null && reference.action.WasPressedThisFrame();

        private void SetActionsEnabled(bool on)
        {
            foreach (var reference in new[] { aim, power, fire, upgrade, restart })
            {
                if (reference == null) continue;
                if (on) reference.action.Enable();
                else reference.action.Disable();
            }
        }
    }
}
