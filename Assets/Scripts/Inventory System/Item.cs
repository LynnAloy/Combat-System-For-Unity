using UnityEngine;


[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item")]
public class Item : ScriptableObject
{

    [SerializeField] new private string name;    
    [SerializeField] private Sprite icon;
    [SerializeField] private bool showInInventory;

    public string Name => name;
    public Sprite Icon => icon;
    public bool ShowInInventory => showInInventory;

    public virtual void Use()
    {

    }

    public void RemoveFromInventory()
    {
        Inventory.Instance.Remove(this);
    }

}
