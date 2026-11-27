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
    [SerializeField] private int healthAmount = 25;
    [SerializeField] private int armorAmount = 20;
    [SerializeField] private int damageAmount = 5;
    [SerializeField] private Vector2Int bonusOreRange = new Vector2Int(10, 30);

    [Header("Reward Icons")]
    [SerializeField] private Sprite healthIcon;
    [SerializeField] private Sprite armorIcon;
    [SerializeField] private Sprite weaponUpgradeIcon;
    [SerializeField] private Sprite bonusOreIcon;

    [Header("Reward Pop Up UI")]
    [SerializeField] private RewardPopUpUI rewardPopUpUI;

    public string AwardLoot(Turret player)
    {
        if(player == null) return "";
        LootType reward = (LootType)Random.Range(0,4);
        string message = "";
        Sprite icon = null;
        switch (reward)
        {
            case LootType.HealthPotion:
                int beforeHeal = player.Health.Current;
                player.Health.Heal(healthAmount);
                int restored = player.Health.Current - beforeHeal;
                message = restored > 0 ? player.DisplayName + " found a potion. +" + restored + " HP." :
                    player.DisplayName + " found a potion at full HP.";
                icon = healthIcon;
                break;
            case LootType.Armor:
                player.Health.IncreaseMaxHealth(armorAmount);
                message = player.DisplayName + " found armor. +" + armorAmount + " max HP.";
                icon = armorIcon;
                break;
            case LootType.WeaponUpgrade:
                player.IncreaseDamage(damageAmount);
                message = player.DisplayName + " found a weapon upgrade. +" + damageAmount + " damage.";
                icon = weaponUpgradeIcon;
                break;
            case LootType.BonusOre:
                int oreAmount = Random.Range(bonusOreRange.x, bonusOreRange.y + 1);
                player.Bank.Add(oreAmount);
                message = player.DisplayName + " found bonus ore. +" + oreAmount + " ore.";
                icon = bonusOreIcon;
                break;
            default:
                return "";
        }
        if(rewardPopUpUI != null && icon != null && !string.IsNullOrEmpty(message))
        {
            rewardPopUpUI.Show(icon, message);
        }
        return message;
    }
}
}
