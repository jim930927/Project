using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ItemDatabase", menuName = "Game/Item Database")]
public class ItemData : ScriptableObject
{
    [System.Serializable]
    public class Item
    {
        public string id;
        public string name;

        [TextArea(2, 5)]
        public string detail; // 簡短描述

        [TextArea(5, 20)]
        public string fullContent; // 第一頁內容

        [Tooltip("多頁內容（優先於 fullContent）")]
        public List<string> pages = new List<string>();

        public bool collected;
    }

    public List<Item> items = new List<Item>();

    public delegate void ItemAddedHandler(Item item);
    public event ItemAddedHandler OnItemAdded;

    public bool HasItem(string id)
    {
        Item item = items.Find(i => i.id == id);
        return item != null && item.collected;
    }

    public void AddItem(string id, string name = null)
    {
        Item item = items.Find(i => i.id == id);
        if (item != null)
        {
            item.collected = true;
            if (!string.IsNullOrEmpty(name))
                item.name = name;
        }
        else
        {
            item = new Item { id = id, name = name ?? id, collected = true };
            items.Add(item);
        }

        Debug.Log($"🎒 獲得道具：{item.name}");
        OnItemAdded?.Invoke(item);
    }

    public void ResetAll()
    {
        foreach (var i in items)
            i.collected = false;
    }

    public void SetItemFullContent(string id, string newContent)
    {
        Item item = items.Find(i => i.id == id);
        if (item != null)
        {
            item.fullContent = newContent;
            Debug.Log($"📘 已更新道具「{item.name}」內容。");
        }
    }

    public void SetItemPages(string id, List<string> newPages)
    {
        Item item = items.Find(i => i.id == id);
        if (item != null)
        {
            item.pages = newPages;
            Debug.Log($"📑 已設定道具「{item.name}」頁面內容，共 {newPages.Count} 頁。");
        }
    }

    public bool AllItemsCollected()
    {
        if (items == null || items.Count == 0)
            return false;

        foreach (var i in items)
        {
            if (!i.collected)
                return false;
        }

        return true;
    }
}
