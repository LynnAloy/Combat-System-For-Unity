using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Inventory.Model
{
    public abstract class ItemSO : ScriptableObject
    {
        [field: SerializeField]
        public bool IsStackable { get; set; }
        public int ID => GetInstanceID();
        [field: SerializeField]
        public int MaxStackSize { get; set; } = 1;
        [field: SerializeField]
        public string Name { get; set; }
        [field: SerializeField]
        public string Description { get; set; }
        [field: SerializeField]
        public Sprite ItemImage { get; set; }
        [field: SerializeField]
        public List<ItemParameter> DefaultParameters { get; set; } = new();
    }

    [SerializeField]
    public struct ItemParameter : IEquatable<ItemParameter>
    {
        public ItemParameterSO itemParameter;
        public float Value;
        public bool Equals(ItemParameter other)
        {
            return other.itemParameter == itemParameter;
        }
    }

}