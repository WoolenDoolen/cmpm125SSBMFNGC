using UnityEngine;

namespace RockAndHardPlace
{
    public sealed class ArtilleryTank : MonoBehaviour
    {
        public int playerIndex;
        public Transform barrelPivot;
        public int health = 100;
        public int ore;
        public bool upgraded;
        public float angle = 40;
        public float power = 8;
        public int Damage => upgraded ? 30 : 20;

        public Vector2 Direction => new Vector2(
            (playerIndex == 0 ? 1 : -1) * Mathf.Cos(angle * Mathf.Deg2Rad),
            Mathf.Sin(angle * Mathf.Deg2Rad));
        public Vector2 Muzzle => (Vector2)barrelPivot.position + Direction * 1.25f;

        public void SetAim(float newAngle, float newPower)
        {
            angle = Mathf.Clamp(newAngle, 5, 85);
            power = Mathf.Clamp(newPower, 5, 20);
            barrelPivot.localRotation = Quaternion.Euler(0, 0, playerIndex == 0 ? angle : 180 - angle);
        }
    }
}
