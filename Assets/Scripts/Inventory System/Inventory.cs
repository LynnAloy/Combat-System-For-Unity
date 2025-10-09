using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Inventory : Singleton<Inventory>
{
    public delegate void OnItemChanged();
    public OnItemChanged onItemChangedCallback;
    [SerializeField] private int space = 10;  
    public List<Item> items = new List<Item>();
    public void Add(Item item)
    {
        if (item.ShowInInventory)
        {
            if (items.Count >= space)
            {
                Debug.Log("Not enough room.");
                return;
            }

            items.Add(item);

            if (onItemChangedCallback != null)
                onItemChangedCallback.Invoke();
        }
    }

    // Remove an item
    public void Remove(Item item)
    {
        items.Remove(item);
        if (onItemChangedCallback != null)
            onItemChangedCallback.Invoke();
    }

}
