using UnityEngine;
using UnityEngine.SceneManagement;

namespace RockAndHardPlace
{
    public enum PrototypePhase { Mining, MovingMountain, Combat }

    public sealed class PrototypeGame : MonoBehaviour
    {
        public ArtilleryTank[] tanks;
        public Transform mountain;
        public Material shellMaterial;
        public int mountainOre = 80;
        public int currentPlayer;
        public PrototypePhase phase;
        public bool shotActive;
        public string lastEvent = "Shoot the mountain to test resource collection.";

        private void Start()
        {
            Physics2D.gravity = new Vector2(0, -9.81f);
            foreach (var tank in tanks) tank.SetAim(40, 14);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.R))
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);

            if (phase == PrototypePhase.MovingMountain)
            {
                // Temporary movement until the mountain has an animation.
                mountain.position += Vector3.right * (8 * Time.deltaTime);
                if (mountain.position.x > 22)
                {
                    mountain.gameObject.SetActive(false);
                    phase = PrototypePhase.Combat;
                    currentPlayer = 1 - currentPlayer;
                    lastEvent = "Mountain cleared. Direct hits now reduce tank health.";
                }
                return;
            }
            if (shotActive) return;

            var tank = tanks[currentPlayer];
            float angleChange = 0;
            float powerChange = 0;
            if (Input.GetKey(KeyCode.UpArrow)) angleChange = 30 * Time.deltaTime;
            if (Input.GetKey(KeyCode.DownArrow)) angleChange = -30 * Time.deltaTime;
            if (Input.GetKey(KeyCode.RightArrow)) powerChange = 5 * Time.deltaTime;
            if (Input.GetKey(KeyCode.LeftArrow)) powerChange = -5 * Time.deltaTime;
            tank.SetAim(tank.angle + angleChange, tank.power + powerChange);
            if (Input.GetKeyDown(KeyCode.U)) BuyUpgrade();
            if (Input.GetKeyDown(KeyCode.Space)) Fire();
        }

        public bool BuyUpgrade()
        {
            if (shotActive || phase == PrototypePhase.MovingMountain) return false;
            var tank = tanks[currentPlayer];
            if (tank.ore < 20 || tank.upgraded) return false;
            tank.ore -= 20;
            tank.upgraded = true;
            lastEvent = "Test upgrade bought: damage is now 30.";
            return true;
        }

        public bool Fire()
        {
            if (shotActive || phase == PrototypePhase.MovingMountain) return false;
            shotActive = true;
            var shell = new GameObject("Test projectile");
            var visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            visual.transform.SetParent(shell.transform, false);
            visual.transform.localScale = Vector3.one * 0.26f;
            visual.GetComponent<Collider>().enabled = false;
            Destroy(visual.GetComponent<Collider>());
            visual.GetComponent<Renderer>().sharedMaterial = shellMaterial;
            shell.AddComponent<ArtilleryShell>().Launch(this, tanks[currentPlayer]);
            return true;
        }

        public void ShotFinished(ArtilleryTank shooter, Collider2D hit)
        {
            if (!shotActive) return;
            shotActive = false;
            lastEvent = "Miss. Next turn.";
            if (phase == PrototypePhase.Mining && hit != null && hit.transform == mountain)
            {
                mountainOre -= 20;
                shooter.ore += 20;
                lastEvent = "Mountain hit: collected 20 ore.";
                if (mountainOre <= 0)
                {
                    phase = PrototypePhase.MovingMountain;
                    mountain.GetComponent<Collider2D>().enabled = false;
                    lastEvent = "Ore empty. Testing the mountain leaving.";
                    return;
                }
            }
            else if (phase == PrototypePhase.Combat && hit != null)
            {
                var target = hit.GetComponent<ArtilleryTank>();
                if (target != null)
                {
                    target.health = Mathf.Max(0, target.health - shooter.Damage);
                    lastEvent = "Direct hit: " + shooter.Damage + " damage.";
                }
            }
            currentPlayer = 1 - currentPlayer;
        }

        private void OnGUI()
        {
            // Temporary readout while the gameplay systems are being tested.
            GUI.Box(new Rect(10, 10, 530, 235), "Gameplay test");
            GUI.Label(new Rect(22, 36, 510, 22), "Phase: " + phase + "    Turn: " + (currentPlayer == 0 ? "Blue" : "Red"));
            GUI.Label(new Rect(22, 60, 510, 22), "Mountain ore: " + mountainOre);
            for (int i = 0; i < tanks.Length; i++)
                GUI.Label(new Rect(22, 84 + i * 24, 510, 22), (i == 0 ? "Blue" : "Red") +
                    "   HP: " + tanks[i].health + "   Ore: " + tanks[i].ore + "   Damage: " + tanks[i].Damage);
            var active = tanks[currentPlayer];
            GUI.Label(new Rect(22, 132, 510, 22), "Angle: " + active.angle.ToString("F1") + "   Power: " + active.power.ToString("F1"));
            GUI.Label(new Rect(22, 156, 510, 22), "Up/Down: aim   Left/Right: power   Space: shoot");
            GUI.Label(new Rect(22, 180, 510, 22), "U: test damage upgrade (20 ore)   R: reset scene");
            GUI.Label(new Rect(22, 210, 510, 22), lastEvent);
        }
    }
}
