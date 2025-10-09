using UnityEngine;


[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Consumable")]
public class Consumable : Item
{

    [SerializeField] private int healthGain;
    [SerializeField] private MeeleFighter meeleFighter;

    public override void Use()
    {
        meeleFighter.Heal(healthGain);
        Debug.Log(name + " consumed!");
        RemoveFromInventory();  
    }

}
