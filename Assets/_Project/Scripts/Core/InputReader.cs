using UnityEngine;
using UnityEngine.InputSystem;

namespace RockAndHardPlace
{
    public sealed class InputReader : MonoBehaviour
    {
        [Tooltip("Defaults to the GameManager on this object.")]
        [SerializeField] private GameManager gameManager;

        [Header("Actions (from GameControls)")]
        [Tooltip("Arrow-key aim. The WASD aim action is loaded from the same action map.")]
        [SerializeField] private InputActionReference aim;
        [Tooltip("Arrow-key power. The WASD power action is loaded from the same action map.")]
        [SerializeField] private InputActionReference power;
        [SerializeField] private InputActionReference fire;
        [SerializeField] private InputActionReference upgrade;
        [SerializeField] private InputActionReference restart;
        private MatchMenu matchMenu;
        private InputAction wasdAim;
        private InputAction wasdPower;

        private void Awake()
        {
            if (gameManager == null) gameManager = GetComponent<GameManager>();
            matchMenu = FindAnyObjectByType<MatchMenu>();
            wasdAim = aim?.action?.actionMap?.FindAction("WasdAim");
            wasdPower = power?.action?.actionMap?.FindAction("WasdPower");
        }

        private void OnEnable() => SetActionsEnabled(true);
        private void OnDisable() => SetActionsEnabled(false);

        private void Update()
        {
            if (gameManager == null || !gameManager.AcceptsKeyboard) return;
            if (matchMenu != null && Keyboard.current?.escapeKey.wasPressedThisFrame == true)
            {
                matchMenu.HandleEscape();
                return;
            }
            if (WasPressed(restart) && gameManager.Phase != GamePhase.Title &&
                (matchMenu == null || matchMenu.AllowsRestart))
            {
                gameManager.Restart();
                return;
            }
            if (matchMenu != null && WasPressed(upgrade))
            {
                matchMenu.ToggleUpgrades();
                return;
            }
            if (matchMenu != null && matchMenu.IsOpen) return;
            if (!gameManager.CanHumanAct) return;
            gameManager.Aim(ReadAxis(wasdAim, aim?.action), ReadAxis(wasdPower, power?.action));
            if (WasPressed(upgrade)) gameManager.TryBuyUpgrade();
            if (WasPressed(fire)) gameManager.TryFire();
        }

        private float ReadAxis(InputAction wasd, InputAction arrows)
        {
            float keyboard = wasd?.ReadValue<float>() ?? 0;
            float arrowKeys = arrows?.ReadValue<float>() ?? 0;
            if (gameManager.Mode == MatchMode.SinglePlayer) return keyboard != 0 ? keyboard : arrowKeys;
            return gameManager.CurrentIndex == 0 && wasd != null ? keyboard : arrowKeys;
        }

        private static bool WasPressed(InputActionReference reference) =>
            reference != null && reference.action.WasPressedThisFrame();

        private void SetActionsEnabled(bool on)
        {
            foreach (var action in new[] { aim?.action, power?.action, wasdAim, wasdPower, fire?.action, upgrade?.action, restart?.action })
            {
                if (action == null) continue;
                if (on) action.Enable();
                else action.Disable();
            }
        }
    }
}
