using DG.Tweening.Core.Easing;
using System.Collections.Generic;
using UnityEngine;
using static ClueData;
using static SaveItem;

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

        public float collectedTime = 0f;
    }

    public List<Item> items = new List<Item>();

    public delegate void ItemAddedHandler(Item item);
    public event ItemAddedHandler OnItemAdded;

    public bool HasItem(string id)
    {
        Item item = items.Find(i => i.id == id);
        return SaveItem.HasItem(id) && item.collected;
    }

    public void AddItem(string id, string name = null)
    {
        Item item = items.Find(i => i.id == id);
        if (item != null)
        {
            item.collected = true;
            item.collectedTime = Time.time;
            if (!string.IsNullOrEmpty(name))
                item.name = name;
            SaveItem.SaveItems(id);
        }
        else
        {
            item = new Item { id = id, name = name ?? id, collected = true };
            items.Add(item);
        }

        Debug.Log($"🎒 獲得道具：{item.name}");
        OnItemAdded?.Invoke(item);
    }

    // ============================
    // ➖ 移除 / 消耗道具
    // ============================
    public void RemoveItem(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            Debug.LogWarning("❌ RemoveItem 被呼叫但 id 為空！");
            return;
        }

        Item item = items.Find(i => i.id == id);
        Debug.Log($"🔍 正在嘗試移除 {id} ，擁有狀態 = {(item != null && item.collected)}");

        if (item != null && item.collected)
        {
            item.collected = false;
            item.collectedTime = 0f;

            Debug.Log($"🗑️ 道具已移除：{item.name} ({id})");
        }
    }

    // ============================
    // 🔄 重置所有道具
    // ============================
    public void ResetAll()
    {
        foreach (var i in items)
        {
            i.collected = false;
            i.collectedTime = 0f;
        }
        SaveItem.ResetItems();
    }

    // ============================
    // 📝 修改內容
    // ============================
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

    // ============================
    // ✔ 是否所有道具都已取得？
    // ============================
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

    public void SyncFromSave(List<string> savedIds)
    {
        foreach (var item in items)
        {
            item.collected = savedIds.Contains(item.id);
            item.collectedTime = item.collected ? Time.time : 0f;
        }
    }

    public void SyncFromPlayerPrefs()
    {
        foreach (var item in items)
        {
            item.collected = PlayerPrefs.GetInt("item_" + item.id, 0) == 1;
        }
    }


}
