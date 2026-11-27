using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Tilemaps;

namespace RockAndHardPlace
{
    // World-space cues sit behind the screen HUD and follow the gameplay state.
    internal sealed class BattlefieldFeedback : MonoBehaviour
    {
        private static readonly Color Ink = new Color32(35, 42, 47, 255);
        private static readonly Color Gold = new Color32(255, 215, 131, 255);
        private static readonly Color Hit = new Color32(253, 118, 83, 255);
        private readonly List<CastleOutline> castles = new List<CastleOutline>();
        private readonly List<Vector3> path = new List<Vector3>(320);
        private readonly LineRenderer[] chips = new LineRenderer[7];
        private GameManager game;
        private MatchMenu menu;
        private ShotTrajectory trajectory;
        private Material material;
        private LineRenderer arcShadow, arc, impact;
        private Turret lastShooter;
        private float lastAngle, lastPower, chipTime;
        private Vector2 chipOrigin;

        public void Initialize(GameManager manager, MatchMenu matchMenu)
        {
            game = manager;
            menu = matchMenu;
            material = Resources.Load<Material>("Prototype/LineUnlit");
            if (material == null)
            {
                Debug.LogError("Battlefield cues need the LineUnlit material.", this);
                enabled = false;
                return;
            }
            foreach (var turret in game.Turrets)
            {
                var masonry = turret.GetComponentInChildren<TilemapCollider2D>();
                if (masonry != null) BuildOutline(turret, masonry.GetComponent<Tilemap>());
            }
            trajectory = new ShotTrajectory();
            arcShadow = CreateLine("Trajectory shadow", 0.092f, 40, Ink);
            arc = CreateLine("EZ trajectory", 0.046f, 41, Gold);
            impact = CreateLine("Predicted impact", 0.04f, 42, Gold);
            impact.loop = true;
            impact.positionCount = 4;
            for (int i = 0; i < chips.Length; i++)
            {
                chips[i] = CreateLine("Ore chip " + (i + 1), 0.055f, 43, Gold);
                chips[i].positionCount = 2;
            }
            game.OreImpact += OnOreImpact;
        }

        private void OnDestroy()
        {
            if (game != null) game.OreImpact -= OnOreImpact;
        }

        private void LateUpdate()
        {
            bool visible = !menu.IsOpen;
            foreach (var castle in castles) castle.Refresh(game, visible);
            UpdateArc(visible && game.EasyMode && game.CanHumanAct);
            UpdateChips(visible);
        }

        private void UpdateArc(bool show)
        {
            if (!show)
            {
                arcShadow.enabled = arc.enabled = impact.enabled = false;
                lastShooter = null;
                return;
            }
            var shooter = game.CurrentTurret;
            if (shooter == lastShooter && shooter.Angle == lastAngle && shooter.Power == lastPower) return;
            lastShooter = shooter;
            lastAngle = shooter.Angle;
            lastPower = shooter.Power;
            var hit = trajectory.Trace(shooter, path);
            arcShadow.enabled = arc.enabled = path.Count > 1;
            arcShadow.positionCount = arc.positionCount = path.Count;
            for (int i = 0; i < path.Count; i++)
            {
                var point = WithDepth(path[i]);
                arcShadow.SetPosition(i, point);
                arc.SetPosition(i, point);
            }
            impact.enabled = hit.collider != null;
            if (!impact.enabled) return;
            Vector3 center = WithDepth(hit.point);
            impact.SetPosition(0, center + Vector3.up * 0.18f);
            impact.SetPosition(1, center + Vector3.right * 0.18f);
            impact.SetPosition(2, center + Vector3.down * 0.18f);
            impact.SetPosition(3, center + Vector3.left * 0.18f);
        }

        private void BuildOutline(Turret turret, Tilemap map)
        {
            var bounds = map.cellBounds;
            var edge = new List<Vector3>();
            int previousTop = bounds.yMin;
            edge.Add(WithDepth(map.CellToWorld(new Vector3Int(bounds.xMin, bounds.yMin))));
            // Follow the actual crenellations. The walls fill every column from the footing to the cap.
            for (int x = bounds.xMin; x < bounds.xMax; x++)
            {
                int top = bounds.yMax;
                while (top > bounds.yMin && !map.HasTile(new Vector3Int(x, top - 1))) top--;
                if (top == previousTop) continue;
                edge.Add(WithDepth(map.CellToWorld(new Vector3Int(x, previousTop))));
                edge.Add(WithDepth(map.CellToWorld(new Vector3Int(x, top))));
                previousTop = top;
            }
            edge.Add(WithDepth(map.CellToWorld(new Vector3Int(bounds.xMax, previousTop))));
            edge.Add(WithDepth(map.CellToWorld(new Vector3Int(bounds.xMax, bounds.yMin))));
            if (edge.Count < 4) return;

            // The first point is shared with the first riser. Avoid a zero-length segment.
            edge.RemoveAt(0);
            int order = map.GetComponent<TilemapRenderer>().sortingOrder;
            var outer = CreateLine(turret.DisplayName + " turn outline shadow", 0.15f, order - 2, Ink);
            var inner = CreateLine(turret.DisplayName + " turn outline", 0.09f, order - 1, Gold);
            var points = edge.ToArray();
            foreach (var line in new[] { outer, inner })
            {
                line.loop = true;
                line.positionCount = points.Length;
                line.SetPositions(points);
            }
            castles.Add(new CastleOutline(turret, outer, inner));
        }

        private void OnOreImpact(Vector2 point)
        {
            chipOrigin = point;
            chipTime = 0.27f;
        }

        private void UpdateChips(bool visible)
        {
            chipTime = Mathf.Max(0, chipTime - Time.deltaTime);
            float remaining = chipTime / 0.27f;
            for (int i = 0; i < chips.Length; i++)
            {
                var chip = chips[i];
                chip.enabled = visible && remaining > 0;
                if (!chip.enabled) continue;
                float angle = (i * 47f + 19) * Mathf.Deg2Rad;
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                var tip = chipOrigin + direction * ((1 - remaining) * (0.5f + 0.06f * i));
                chip.SetPosition(0, WithDepth(tip));
                chip.SetPosition(1, WithDepth(tip + direction * (0.26f * remaining)));
                chip.startColor = chip.endColor = new Color(Gold.r, Gold.g, Gold.b, remaining);
            }
        }

        private LineRenderer CreateLine(string name, float width, int order, Color color)
        {
            var line = new GameObject(name).AddComponent<LineRenderer>();
            line.transform.SetParent(transform, false);
            line.sharedMaterial = material;
            line.sortingOrder = order;
            line.widthMultiplier = width;
            line.startColor = line.endColor = color;
            line.numCornerVertices = 2;
            line.numCapVertices = 2;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.enabled = false;
            return line;
        }

        private static Vector3 WithDepth(Vector2 point) => new Vector3(point.x, point.y, -0.75f);

        private sealed class CastleOutline
        {
            private readonly Turret turret;
            private readonly LineRenderer shadow, outline;
            private int health;
            private float flash;

            public CastleOutline(Turret turret, LineRenderer shadow, LineRenderer outline)
            {
                this.turret = turret;
                this.shadow = shadow;
                this.outline = outline;
                health = turret.Health.Current;
            }

            public void Refresh(GameManager game, bool visible)
            {
                int hp = turret.Health.Current;
                flash = hp < health ? 0.32f : Mathf.Max(0, flash - Time.deltaTime);
                health = hp;
                bool acting = game.HasTurn && game.CurrentTurret == turret;
                outline.enabled = shadow.enabled = visible && (acting || flash > 0);
                if (outline.enabled) outline.startColor = outline.endColor = Color.Lerp(Gold, Hit, flash / 0.32f);
            }
        }
    }
}
