using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ClueDatabase", menuName = "Game/Clue Database")]
public class ClueData : ScriptableObject
{
    [System.Serializable]
    public class Clue
    {
        public string id;
        public string name;

        [TextArea(2, 5)]
        public string detail; // 簡短描述

        [TextArea(5, 20)]
        public string fullContent; // 可作為第一頁預設內容

        [Tooltip("自訂每一頁的完整內容（優先於 fullContent）")]
        public List<string> pages = new List<string>(); // 🆕 多頁內容

        public bool collected;
    }

    public List<Clue> clues = new List<Clue>();

    public delegate void ClueAddedHandler(Clue clue);
    public event ClueAddedHandler OnClueAdded;

    public bool HasClue(string id)
    {
        Clue clue = clues.Find(c => c.id == id);
        return clue != null && clue.collected;
    }

    public void AddClue(string id, string name = null)
    {
        Clue clue = clues.Find(c => c.id == id);
        if (clue != null)
        {
            clue.collected = true;
            if (!string.IsNullOrEmpty(name))
                clue.name = name;
        }
        else
        {
            clue = new Clue { id = id, name = name ?? id, collected = true };
            clues.Add(clue);
        }

        Debug.Log($"📜 獲得線索：{clue.name}");
        OnClueAdded?.Invoke(clue);
        ClueUIManager.Instance?.ShowClue(clue.name);
    }

    public void ResetAll()
    {
        foreach (var c in clues)
            c.collected = false;
    }

    public void SetClueFullContent(string id, string newContent)
    {
        Clue clue = clues.Find(c => c.id == id);
        if (clue != null)
        {
            clue.fullContent = newContent;
            Debug.Log($"📖 已更新線索「{clue.name}」的完整內容。");
        }
    }

    public void SetCluePages(string id, List<string> newPages)
    {
        Clue clue = clues.Find(c => c.id == id);
        if (clue != null)
        {
            clue.pages = newPages;
            Debug.Log($"📑 已設定線索「{clue.name}」的頁面內容，共 {newPages.Count} 頁。");
        }
    }

    public bool AllCluesCollected()
    {
        if (clues == null || clues.Count == 0)
        {
            Debug.LogWarning("⚠️ 尚未設定任何線索！");
            return false;
        }

        foreach (var c in clues)
        {
            if (!c.collected)
            {
                Debug.Log($"❌ 尚未收集線索：{c.name}");
                return false;
            }
        }

        Debug.Log("✅ 所有線索都已收集！");
        return true;
    }
    
    // ✅ 新增：檢查是否收集了指定的線索
public bool HasCollectedClues(params string[] requiredIds)
    {
        foreach (string id in requiredIds)
        {
            Clue clue = clues.Find(c => c.id == id);
            if (clue == null || !clue.collected)
            {
                Debug.Log($"❌ 尚未收集線索：{id}");
                return false;
            }
        }

        Debug.Log("✅ 已收集指定的所有線索！");
        return true;
    }

}
