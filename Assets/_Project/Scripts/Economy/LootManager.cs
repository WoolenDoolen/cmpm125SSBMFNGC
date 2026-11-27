using UnityEngine;
namespace RockAndHardPlace
{

public class LootManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public enum LootType
    {
        HealthPotion,
        Armor,
        WeaponUpgrade,
        BonusOre
    }
    [Header("Reward Settings")]
    [SerializeField, Min(1)] private int healthAmount = 25;
    [SerializeField, Min(1)] private int armorAmount = 20;
    [SerializeField, Min(1)] private int damageAmount = 5;
    [SerializeField, Min(1)] private Vector2Int bonusOreRange = new Vector2Int(10, 30);

    public string AwardLoot(Turret player)
    {
        if(player == null) return "";
        LootType reward = (LootType)Random.Range(0,4);
        switch (reward)
        {
            case LootType.HealthPotion:
                player.Health.Heal(healthAmount);
                return player.DisplayName + " found health potion! +" + healthAmount + "HP.";
            case LootType.Armor:
                player.Health.IncreaseMaxHealth(armorAmount);
                return player.DisplayName + " found armor! +" + armorAmount + " Armor.";
            case LootType.WeaponUpgrade:
                player.IncreaseDamage(damageAmount);
                return player.DisplayName + " found a weapon upgrade! +" + damageAmount + " Damage.";
            case LootType.BonusOre:
                int oreAmount = Random.Range(bonusOreRange.x, bonusOreRange.y + 1);
                player.Bank.Add(oreAmount);
                return player.DisplayName + " found bonus ore! +" + oreAmount + " Ore.";
            default:
                return "";
        }
    }
}
}
