using System.Collections.Generic;
using UnityEngine;

public class ItemMagnetManager : MonoBehaviour
{
    private static readonly HashSet<ItemMagnet> Items = new();

    private int _level;

    public static void Register(ItemMagnet item)
    {
        if (item == null) return;

        Items.Add(item);

        if (Instance != null)
            item.SetPickupRange(Instance.CurrentRange);
    }

    public static void Unregister(ItemMagnet item)
    {
        Items.Remove(item);
    }

    public static ItemMagnetManager Instance { get; private set; }

    public float CurrentRange => _level * 5f;

    private void Awake()
    {
        Instance = this;
        ApplyRange();
    }

    public void SetLevel(int level)
    {
        _level = Mathf.Max(0, level);
        ApplyRange();
    }

    private void ApplyRange()
    {
        foreach (ItemMagnet item in Items)
        {
            if (item != null)
                item.SetPickupRange(CurrentRange);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}