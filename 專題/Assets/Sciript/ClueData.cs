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
        public string detail;
        public bool collected;
    }

    public List<Clue> clues = new List<Clue>();

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
        ClueUIManager.Instance?.ShowClue(clue.name);
    }

    public void ResetAll()
    {
        foreach (var c in clues) c.collected = false;
    }

    public void SetClueDetail(string id, string newDetail)
    {
        Clue clue = clues.Find(c => c.id == id);
        if (clue != null)
        {
            clue.detail = newDetail;
            Debug.Log($"📝 已更新線索「{clue.name}」的詳細內容：{newDetail}");
        }
        else
        {
            Debug.LogWarning($"⚠️ 找不到線索 {id}，無法設定詳細內容");
        }
    }

    // ✅ 新增：檢查所有線索是否收集完成
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
}
