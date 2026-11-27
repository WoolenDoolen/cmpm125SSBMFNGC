using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
        private CastleStatus[] castles;
        private GameObject[] matchWidgets;
        private Graphic[] rewardGraphics;
        private Button upgradesButton;
        private bool matchVisible = true;

        internal GameManager Game => gameManager;
        internal TMP_FontAsset Font => statusText.font;

        private void Awake()
        {
            if (gameManager == null || statusText == null || eventText == null)
            {
                Debug.LogError("Hud needs a Game Manager, Status Text and Event Text assigned.", this);
                enabled = false;
            }
        }

        internal void ConnectMenu(MatchMenu menu)
        {
            var canvas = (RectTransform)transform;
            var statusPanel = (RectTransform)statusText.transform.parent;
            MatchUi.Anchor(statusPanel, new Vector2(0.5f, 1), new Vector2(0, -22), new Vector2(450, 0));
            statusPanel.GetComponent<Image>().color = new Color32(22, 32, 39, 237);
            statusText.alignment = TextAlignmentOptions.Center;
            statusText.fontSize = 22;
            var reward = GetComponentInChildren<RewardPopUpUI>();
            rewardGraphics = reward != null ? reward.GetComponentsInChildren<Graphic>() : System.Array.Empty<Graphic>();

            var upgradeRect = MatchUi.Child(canvas, "Open upgrade tree");
            MatchUi.Anchor(upgradeRect, Vector2.zero, new Vector2(20, 20), new Vector2(213, 64));
            upgradesButton = MatchUi.LabeledButton(upgradeRect, Font, "Upgrades  [U]", menu.ToggleUpgrades, size: 23);
            var menuRect = MatchUi.Child(canvas, "Open pause menu");
            MatchUi.Anchor(menuRect, Vector2.right, new Vector2(-20, 20), new Vector2(189, 64));
            MatchUi.LabeledButton(menuRect, Font, "Menu  [Esc]", menu.HandleEscape, size: 23);

            int count = gameManager.Turrets.Count;
            castles = new CastleStatus[count];
            matchWidgets = new GameObject[4 + count];
            matchWidgets[0] = statusPanel.gameObject;
            matchWidgets[1] = eventText.transform.parent.gameObject;
            matchWidgets[2] = upgradeRect.gameObject;
            matchWidgets[3] = menuRect.gameObject;
            for (int i = 0; i < count; i++)
            {
                var turret = gameManager.Turrets[i];
                var card = MatchUi.Child(canvas, turret.DisplayName + " kingdom status");
                var anchor = i == 0 ? Vector2.up : Vector2.one;
                MatchUi.Anchor(card, anchor, new Vector2(i == 0 ? 20 : -20, -22), new Vector2(334, 112));
                castles[i] = new CastleStatus(card, Font, turret, i == 0 ? "WASD" : "ARROWS");
                matchWidgets[4 + i] = card.gameObject;
            }
        }

        internal void SetMatchVisible(bool visible)
        {
            matchVisible = visible;
            foreach (var widget in matchWidgets) widget.SetActive(visible);
            foreach (var graphic in rewardGraphics) graphic.enabled = visible;
        }

        // Aim changes every frame while a key is held, so the HUD refreshes every frame.
        private void Update()
        {
            if (!gameManager.isActiveAndEnabled || !matchVisible) return;
            if (castles != null)
            {
                UpdateMatchDisplay();
                return;
            }

            // Older scenes with just the original HUD can still display their match state.
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

        private void UpdateMatchDisplay()
        {
            bool hasTurn = gameManager.HasTurn;
            var active = gameManager.CurrentTurret;
            builder.Clear();
            if (hasTurn)
            {
                builder.Append("<size=28><b><color=#").Append(ColorUtility.ToHtmlStringRGB(MatchUi.Team(active)))
                    .Append(">").Append(active.DisplayName.ToUpperInvariant())
                    .Append(gameManager.ShotActive ? "'S SHOT" : "'S TURN").Append("</color></b></size>\n");
                builder.Append("Aim ").Append(active.Angle.ToString("F1")).Append("°    Power ")
                    .Append(active.Power.ToString("F1")).Append('\n');
                if (gameManager.Phase == GamePhase.Mining)
                    builder.Append("Mountain ore ").Append(gameManager.Mountain.OreRemaining)
                        .Append(" / ").Append(gameManager.Mountain.StartingOre);
                else builder.Append("Kingdom duel");
                if (gameManager.EasyMode) builder.Append("    ·    EZ");
            }
            else
            {
                builder.Append("<size=26><b>The mountain is leaving!</b></size>");
            }
            statusText.SetText(builder);
            eventText.SetText(gameManager.LastEvent);
            upgradesButton.interactable = gameManager.CanAct;
            foreach (var castle in castles) castle.Refresh(hasTurn && castle.Turret == active);
        }

        private sealed class CastleStatus
        {
            public Turret Turret { get; }
            private readonly Image rule, fill, trail;
            private readonly TMP_Text hp, stats;
            private readonly Color team;
            private int previousHealth;
            private float hitFlash;

            public CastleStatus(RectTransform card, TMP_FontAsset font, Turret turret, string aimKeys)
            {
                Turret = turret;
                team = MatchUi.Team(turret);
                previousHealth = turret.Health.Current;
                MatchUi.Fill(card, new Color32(22, 32, 39, 237));
                rule = MatchUi.Fill(MatchUi.At(card, "Turn rule", 0, 0, 334, 4), team);
                MatchUi.Text(MatchUi.At(card, "Kingdom name", 17, 12, 175, 34),
                    font, turret.DisplayName + " kingdom", 23, team, bold: true);
                hp = MatchUi.Text(MatchUi.At(card, "Health total", 180, 12, 138, 34),
                    font, "", 21, MatchUi.TextColor, TextAlignmentOptions.MidlineRight, true);
                hp.textWrappingMode = TextWrappingModes.NoWrap;
                var track = MatchUi.At(card, "Health bar", 17, 52, 300, 15);
                MatchUi.Fill(track, new Color32(53, 59, 65, 255));
                trail = HealthFill(track, "Damage trail", MatchUi.Gold);
                fill = HealthFill(track, "Remaining health", team);
                stats = MatchUi.Text(MatchUi.At(card, "Ore and damage", 17, 74, 223, 28),
                    font, "", 21, MatchUi.Muted);
                MatchUi.Text(MatchUi.At(card, "Aim keys", 246, 74, 72, 28), font, aimKeys, 16, team,
                    TextAlignmentOptions.MidlineRight, true);
            }

            public void Refresh(bool active)
            {
                int current = Turret.Health.Current;
                int max = Turret.Health.Max;
                float fraction = (float)current / max;
                if (current < previousHealth) hitFlash = 0.36f;
                previousHealth = current;
                hitFlash = Mathf.Max(0, hitFlash - Time.deltaTime);
                float trailing = Mathf.MoveTowards(trail.rectTransform.anchorMax.x, fraction, Time.deltaTime * 0.85f);
                trail.rectTransform.anchorMax = new Vector2(Mathf.Max(fraction, trailing), 1);
                fill.rectTransform.anchorMax = new Vector2(fraction, 1);
                fill.color = Color.Lerp(team, Color.white, hitFlash / 0.36f);
                hp.color = Color.Lerp(MatchUi.TextColor, MatchUi.Red, hitFlash / 0.36f);
                hp.text = current + "/" + max + " HP";
                stats.text = "Ore " + Turret.Bank.Ore + "    ·    Damage " + Turret.Damage;
                rule.color = active ? team : new Color(team.r, team.g, team.b, 0.23f);
            }

            private static Image HealthFill(RectTransform track, string name, Color color)
            {
                var rect = MatchUi.Child(track, name);
                MatchUi.Stretch(rect);
                return MatchUi.Fill(rect, color);
            }
        }
    }
}
