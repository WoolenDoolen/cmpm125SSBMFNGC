using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RockAndHardPlace
{
    [RequireComponent(typeof(Hud))]
    public sealed class MatchMenu : MonoBehaviour
    {
        private enum Page { Closed, Title, Pause, Options, Help, Credits, Upgrades, Results }

        private readonly Dictionary<Page, RectTransform> panels = new Dictionary<Page, RectTransform>();
        private readonly Dictionary<Page, Button> firstButtons = new Dictionary<Page, Button>();
        private readonly Dictionary<UpgradeId, UpgradeButton> upgradeButtons = new Dictionary<UpgradeId, UpgradeButton>();
        private Hud hud;
        private GameManager game;
        private TMP_FontAsset font;
        private RectTransform overlay;
        private Image backdrop;
        private TMP_Text optionValue, upgradeTitle, upgradeOre, resultTitle, resultSubtitle;
        private Page current;
        private Page backPage;

        public bool IsOpen => current != Page.Closed;
        public bool AllowsRestart => current == Page.Closed || current == Page.Results;

        private void Start()
        {
            hud = GetComponent<Hud>();
            game = hud.Game;
            if (!hud.enabled || game == null || !game.enabled)
            {
                enabled = false;
                return;
            }
            font = hud.Font;
            overlay = MatchUi.Child((RectTransform)transform, "Match Menu");
            MatchUi.Stretch(overlay);
            backdrop = MatchUi.Fill(overlay, Color.clear, true);
            BuildTitle();
            BuildPause();
            BuildOptions();
            BuildHelp();
            BuildCredits();
            BuildUpgrades();
            BuildResults();
            hud.ConnectMenu(this);
            overlay.SetAsLastSibling();
            game.gameObject.AddComponent<BattlefieldFeedback>().Initialize(game, this);
            Show(game.Phase == GamePhase.Title ? Page.Title : Page.Closed);
        }

        private void LateUpdate()
        {
            if (game.Phase == GamePhase.GameOver && current != Page.Results) Show(Page.Results);
        }

        public void HandleEscape()
        {
            if (current == Page.Closed) Show(Page.Pause);
            else if (current == Page.Pause || current == Page.Upgrades) Show(Page.Closed);
            else if (current == Page.Options || current == Page.Help || current == Page.Credits) Show(backPage);
        }

        public void ToggleUpgrades()
        {
            if (current == Page.Upgrades) Show(Page.Closed);
            else if (current == Page.Closed && game.CanHumanAct) Show(Page.Upgrades);
        }

        private void OpenFrom(Page next)
        {
            backPage = current;
            Show(next);
        }

        private void Show(Page next)
        {
            current = next;
            bool open = next != Page.Closed;
            overlay.gameObject.SetActive(open);
            backdrop.color = new Color32(12, 24, 30, (byte)(next == Page.Title ? 96 : 159));
            foreach (var (page, panel) in panels) panel.gameObject.SetActive(page == next);
            hud.SetMatchVisible(!open);

            // Upgrades can only open between shots. They block keyboard play while
            // leaving the purchase actions available on that page.
            bool matchRunning = game.HasTurn || game.Phase == GamePhase.MountainLeaving;
            game.SetPaused(matchRunning && open && next != Page.Upgrades);
            game.IgnoreKeyboardThisFrame();

            if (next == Page.Options) RefreshOption();
            if (next == Page.Upgrades) RefreshUpgrades();
            if (next == Page.Results)
            {
                resultTitle.text = game.Winner.DisplayName + (game.IsCpu(game.Winner) ? " CPU wins" : " wins");
                resultTitle.color = MatchUi.Team(game.Winner);
                foreach (var turret in game.Turrets)
                    if (turret != game.Winner) resultSubtitle.text = turret.DisplayName + " castle fell.";
            }
            if (EventSystem.current != null)
            {
                var selection = firstButtons.TryGetValue(next, out var first) ? first.gameObject : null;
                EventSystem.current.SetSelectedGameObject(selection);
            }
        }

        private RectTransform Panel(Page page, float width, float height)
        {
            var frame = MatchUi.Child(overlay, page + " Screen");
            MatchUi.Anchor(frame, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(width, height));
            var shadow = MatchUi.At(frame, "Shadow", 9, 11, width, height);
            MatchUi.Fill(shadow, new Color32(8, 17, 22, 175));
            var border = MatchUi.At(frame, "Border", 0, 0, width, height);
            MatchUi.Fill(border, MatchUi.Edge);
            var background = MatchUi.At(frame, "Background", 2, 2, width - 4, height - 4);
            MatchUi.Fill(background, MatchUi.Panel);
            var accent = MatchUi.At(frame, "Gold rule", 2, 2, width - 4, 4);
            MatchUi.Fill(accent, MatchUi.Gold);
            panels.Add(page, frame);
            return frame;
        }

        private TMP_Text Text(RectTransform panel, string name, string value, float x, float y, float width,
            float height, float size, Color color, TextAlignmentOptions align = TextAlignmentOptions.MidlineLeft,
            bool bold = false) =>
            MatchUi.Text(MatchUi.At(panel, name, x, y, width, height), font, value, size, color, align, bold);

        private Button Button(RectTransform panel, string label, System.Action action, float x, float y,
            float width, float height = 56, bool primary = false) =>
            MatchUi.LabeledButton(MatchUi.At(panel, label, x, y, width, height), font, label, action, primary);

        private void Heading(RectTransform panel, string title, float width) =>
            Text(panel, "Heading", title, 42, 27, width - 84, 61, 48, MatchUi.TextColor, bold: true);

        private void BuildTitle()
        {
            var panel = Panel(Page.Title, 700, 352);
            Text(panel, "Game title", "Between a Rock\nand a Hard Place", 42, 27, 616, 153, 63,
                MatchUi.TextColor, TextAlignmentOptions.Center, true);
            firstButtons[Page.Title] = Button(panel, "Single Player", () => StartMode(MatchMode.SinglePlayer),
                42, 190, 300, 66, true);
            Button(panel, "2 Player", () => StartMode(MatchMode.TwoPlayer), 358, 190, 300, 66);
            Button(panel, "How to Play", () => OpenFrom(Page.Help), 42, 272, 196);
            Button(panel, "Options", () => OpenFrom(Page.Options), 252, 272, 196);
            Button(panel, "Credits", () => OpenFrom(Page.Credits), 462, 272, 196);
        }

        private void StartMode(MatchMode mode)
        {
            game.BeginMatch(mode);
            Show(Page.Closed);
        }

        private void BuildPause()
        {
            var panel = Panel(Page.Pause, 562, 448);
            Heading(panel, "Paused", 562);
            firstButtons[Page.Pause] = Button(panel, "Resume", () => Show(Page.Closed), 42, 114, 478, 66, true);
            Button(panel, "How to Play", () => OpenFrom(Page.Help), 42, 194, 478);
            Button(panel, "Options", () => OpenFrom(Page.Options), 42, 262, 478);
            Button(panel, "Main Menu", game.ReturnToTitle, 42, 330, 478);
        }

        private void BuildOptions()
        {
            var panel = Panel(Page.Options, 680, 358);
            Heading(panel, "Options", 680);
            Text(panel, "EZ Mode", "EZ Mode", 42, 114, 426, 53, 33, MatchUi.Gold, bold: true);
            var toggle = Button(panel, "EZ Mode Toggle", () =>
            {
                game.SetEasyMode(!game.EasyMode);
                RefreshOption();
            }, 492, 114, 146);
            optionValue = toggle.GetComponentInChildren<TMP_Text>();
            firstButtons[Page.Options] = toggle;
            Text(panel, "EZ Mode description", "Show your shell's predicted path and first impact while you aim.",
                42, 182, 596, 74, 24, MatchUi.Muted, TextAlignmentOptions.TopLeft);
            Button(panel, "Back", () => Show(backPage), 42, 268, 184);
        }

        private void RefreshOption() => optionValue.text = game.EasyMode ? "On" : "Off";

        private void BuildHelp()
        {
            var panel = Panel(Page.Help, 1080, 660);
            Heading(panel, "How to Play", 1080);
            Text(panel, "Match heading", "The match", 42, 113, 480, 40, 28, MatchUi.Gold, bold: true);
            string[] rules =
            {
                "<b>Single Player.</b> You play Blue with WASD or arrows. The CPU plays Red.",
                "<b>2 Player.</b> Share a keyboard and use your side's keys.",
                "<b>Mine and upgrade.</b> Hit the mountain for ore and loot. Spend ore before firing.",
                "<b>Duel.</b> Once the mountain leaves, bring the other castle to zero HP."
            };
            for (int i = 0; i < rules.Length; i++)
                Text(panel, "Match rule " + (i + 1), rules[i], 42, 163 + i * 61, 484, 57, 22, MatchUi.TextColor, TextAlignmentOptions.TopLeft);
            Text(panel, "Controls heading", "Controls", 565, 113, 473, 40, 28, MatchUi.Gold, bold: true);
            string[] keys = { "Blue    W / S", "Blue    A / D", "Red     ↑ / ↓", "Red     ← / →", "Space", "U", "Esc", "R" };
            string[] actions = { "Raise / lower aim", "Lower / raise power", "Raise / lower aim", "Lower / raise power",
                "Fire on your turn", "Open your upgrade tree", "Pause / back", "Restart match" };
            for (int i = 0; i < keys.Length; i++)
            {
                Color keyColor = i < 2 ? MatchUi.Blue : i < 4 ? MatchUi.Red : MatchUi.TextColor;
                Text(panel, keys[i], keys[i], 565, 160 + i * 31, 200, 30, 22, keyColor, bold: true);
                Text(panel, actions[i], actions[i], 778, 160 + i * 31, 260, 30, 22, MatchUi.Muted);
            }
            MatchUi.Fill(MatchUi.At(panel, "Divider", 42, 420, 996, 2), MatchUi.Edge);
            Text(panel, "Upgrade guide heading", "Upgrade tree", 42, 425, 996, 47, 28, MatchUi.Gold, bold: true);
            var weapon = game.Turrets[0].Weapon;
            Text(panel, "Cannon branch", BranchGuide(UpgradeId.ForgedShells, UpgradeId.HeavyShot, weapon),
                42, 474, 484, 80, 22, MatchUi.Muted, TextAlignmentOptions.TopLeft);
            Text(panel, "Fortification branch", BranchGuide(UpgradeId.ReinforcedWalls, UpgradeId.InnerKeep, weapon),
                565, 474, 473, 80, 22, MatchUi.Muted, TextAlignmentOptions.TopLeft);
            firstButtons[Page.Help] = Button(panel, "Back", () => Show(backPage), 42, 575, 184);
        }

        private static string BranchGuide(UpgradeId first, UpgradeId second, WeaponData weapon)
        {
            var start = CastleUpgrades.Get(first);
            var next = CastleUpgrades.Get(second);
            return $"<b>{start.Name}</b>  {start.Cost(weapon)} ore · {start.Effect(weapon)}\n" +
                $"  ↓ <b>{next.Name}</b>  {next.Cost(weapon)} ore · {next.Effect(weapon)}";
        }

        private void BuildCredits()
        {
            var panel = Panel(Page.Credits, 708, 446);
            Heading(panel, "Credits", 708);
            Text(panel, "Project team", "<b>Project team</b>\nTodd Crandell · DrewHinesUCSC · mlcastag",
                42, 114, 624, 82, 26, MatchUi.TextColor, TextAlignmentOptions.TopLeft);
            Text(panel, "Asset credits", "<b>Pixel tiles</b>\nKenney · Platformer Art Pixel (CC0)\n\n" +
                "<b>Production tools</b>\nUnity · TextMesh Pro · Codex",
                42, 207, 624, 146, 23, MatchUi.Muted, TextAlignmentOptions.TopLeft);
            firstButtons[Page.Credits] = Button(panel, "Back", () => Show(backPage), 42, 362, 184);
        }

        private void BuildUpgrades()
        {
            var panel = Panel(Page.Upgrades, 1040, 606);
            upgradeTitle = Text(panel, "Upgrade title", "", 42, 27, 688, 62, 48, MatchUi.TextColor, bold: true);
            upgradeOre = Text(panel, "Available ore", "", 744, 31, 254, 55, 29, MatchUi.Gold, TextAlignmentOptions.MidlineRight, true);
            Text(panel, "Purchase help", "Spend your ore on either branch. Unlock the top upgrade first.",
                42, 105, 956, 41, 24, MatchUi.Muted);
            Text(panel, "Cannon heading", "Cannon", 42, 161, 464, 42, 29, MatchUi.Gold, bold: true);
            Text(panel, "Fortification heading", "Fortification", 534, 161, 464, 42, 29, MatchUi.Gold, bold: true);
            AddUpgrade(panel, UpgradeId.ForgedShells, 42, 213);
            AddUpgrade(panel, UpgradeId.HeavyShot, 42, 363);
            AddUpgrade(panel, UpgradeId.ReinforcedWalls, 534, 213);
            AddUpgrade(panel, UpgradeId.InnerKeep, 534, 363);
            Text(panel, "Cannon link", "↓", 42, 317, 464, 43, 36, MatchUi.Muted, TextAlignmentOptions.Center);
            Text(panel, "Fortification link", "↓", 534, 317, 464, 43, 36, MatchUi.Muted, TextAlignmentOptions.Center);
            firstButtons[Page.Upgrades] = Button(panel, "Back to aiming", () => Show(Page.Closed), 42, 510, 254);
        }

        private void AddUpgrade(RectTransform panel, UpgradeId id, float x, float y)
        {
            var upgrade = CastleUpgrades.Get(id);
            var node = MatchUi.At(panel, upgrade.Name, x, y, 464, 106);
            var button = MatchUi.Control(node, () =>
            {
                game.TryBuyUpgrade(id);
                RefreshUpgrades();
                if (!upgradeButtons[id].Button.interactable && EventSystem.current != null)
                    EventSystem.current.SetSelectedGameObject(firstButtons[Page.Upgrades].gameObject);
            });
            Text(node, "Name", upgrade.Name, 18, 10, 428, 42, 29, MatchUi.TextColor, bold: true);
            var effect = Text(node, "Effect and cost", "", 18, 58, 246, 32, 20, MatchUi.Muted);
            var state = Text(node, "Availability", "", 268, 58, 178, 34, 18, MatchUi.Gold, TextAlignmentOptions.MidlineRight);
            upgradeButtons.Add(id, new UpgradeButton(button, effect, state));
        }

        private void RefreshUpgrades()
        {
            var turret = game.CurrentTurret;
            upgradeTitle.text = turret.DisplayName + " upgrades";
            upgradeTitle.color = MatchUi.Team(turret);
            upgradeOre.text = turret.Bank.Ore + " ore";
            foreach (var upgrade in CastleUpgrades.Nodes)
            {
                var node = upgradeButtons[upgrade.Id];
                int cost = upgrade.Cost(turret.Weapon);
                var state = CastleUpgrades.StateFor(upgrade, turret);
                node.Button.interactable = state == UpgradeState.Available;
                node.Effect.text = upgrade.Effect(turret.Weapon) + " · " + cost + " ore";
                node.State.text = state switch
                {
                    UpgradeState.Available => "Buy",
                    UpgradeState.Purchased => "Unlocked",
                    UpgradeState.Locked => "Needs " + CastleUpgrades.Get(upgrade.Requires.Value).Name,
                    _ => "Need " + (cost - turret.Bank.Ore) + " more ore"
                };
                node.State.color = state == UpgradeState.Purchased ? new Color32(171, 219, 165, 255) : MatchUi.Gold;
            }
        }

        private void BuildResults()
        {
            var panel = Panel(Page.Results, 712, 394);
            resultTitle = Text(panel, "Winner", "", 42, 65, 628, 96, 70, MatchUi.Gold, TextAlignmentOptions.Center, true);
            resultSubtitle = Text(panel, "Defeated kingdom", "", 42, 174, 628, 53, 30, MatchUi.TextColor, TextAlignmentOptions.Center);
            firstButtons[Page.Results] = Button(panel, "Rematch", game.Restart, 42, 277, 303, 62, true);
            Button(panel, "Main Menu", game.ReturnToTitle, 367, 277, 303, 62);
        }

        private readonly struct UpgradeButton
        {
            public Button Button { get; }
            public TMP_Text Effect { get; }
            public TMP_Text State { get; }

            public UpgradeButton(Button button, TMP_Text effect, TMP_Text state)
            {
                Button = button;
                Effect = effect;
                State = state;
            }
        }
    }
}
