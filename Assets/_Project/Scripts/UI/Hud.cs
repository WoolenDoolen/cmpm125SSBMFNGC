using System.Text;
using TMPro;
using UnityEngine;

namespace RockAndHardPlace
{
    public sealed class Hud : MonoBehaviour
    {
        [Tooltip("The GameManager whose state this HUD shows.")]
        [SerializeField] private GameManager gameManager;

        [Header("Text")]
        [Tooltip("Phase, turn, mountain ore, each side's health/ore/damage, and the current aim.")]
        [SerializeField] private TMP_Text statusText;
        [Tooltip("The most recent thing that happened.")]
        [SerializeField] private TMP_Text eventText;

        private readonly StringBuilder builder = new StringBuilder();

        private void Awake()
        {
            if (gameManager == null || statusText == null || eventText == null)
            {
                Debug.LogError("Hud needs a Game Manager, Status Text and Event Text assigned.", this);
                enabled = false;
            }
        }

        // Aim changes every frame while a key is held, so the HUD refreshes every frame.
        private void Update()
        {
            if (!gameManager.isActiveAndEnabled) return;

            bool over = gameManager.Phase == GamePhase.GameOver;
            builder.Clear();
            builder.Append("<b>Phase:</b> ").Append(gameManager.Phase)
                   .Append("    <b>Turn:</b> ").Append(over ? "-" : gameManager.CurrentTurret.DisplayName).Append('\n');
            builder.Append("<b>Mountain ore:</b> ").Append(gameManager.Mountain.OreRemaining)
                   .Append(" / ").Append(gameManager.Mountain.StartingOre).Append('\n');

            foreach (var turret in gameManager.Turrets)
            {
                builder.Append("<b>").Append(turret.DisplayName).Append("</b>")
                       .Append("   HP ").Append(turret.Health.Current).Append('/').Append(turret.Health.Max)
                       .Append("   Ore ").Append(turret.Bank.Ore)
                       .Append("   Damage ").Append(turret.Damage).Append('\n');
            }

            if (!over)
            {
                var active = gameManager.CurrentTurret;
                builder.Append("<b>Angle:</b> ").Append(active.Angle.ToString("F1"))
                       .Append("°   <b>Power:</b> ").Append(active.Power.ToString("F1"));
            }

            statusText.SetText(builder);
            eventText.SetText(gameManager.LastEvent);
        }
    }
}
