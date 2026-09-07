using System;
using System.Collections;
using System.Collections.Generic;
using Inventory.Model;
using UnityEngine;

public class Item : MonoBehaviour
{

    [field: SerializeField]
    public ItemSO InventoryItem { get; private set; }
    [field: SerializeField]
    public int Quantity { get; set; } = 1;
    [SerializeField]
    private float duration = 0.3f;

    private void Start()
    {
       MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
    }

    internal void DestroyItem()
    {
        Collider collider = GetComponent<Collider>();
        if (collider != null)
        {
            collider.enabled = false;
        }
        StartCoroutine(AnimateItemPickup());
    }

    private IEnumerator AnimateItemPickup()
    {
        Vector3 startScale = transform.localScale;
        Vector3 endScale = Vector3.zero;
        float currentTime = 0;
        while (currentTime < duration)
        {
            currentTime += Time.deltaTime;
            transform.localScale = Vector3.Lerp(startScale, endScale, currentTime / duration);
            yield return null;
        }
        Destroy(gameObject);
    }
}
