using System.Collections.Generic;
using UnityEngine;

public class SaveItem : MonoBehaviour
{
    // 儲存道具
    public static void SaveItems(string id)
    {
        PlayerPrefs.SetInt("item_" + id, 1);
        PlayerPrefs.Save();
    }

    // 是否已經取得道具
    public static bool HasItem(string id)
    {
        return PlayerPrefs.GetInt("item_" + id, 0) == 1;
    }

    // 🟩 新增：刪除單一道具（使用後移除）
    public static void RemoveItem(string id)
    {
        PlayerPrefs.DeleteKey("item_" + id);
        PlayerPrefs.Save();
    }

    // 重置所有道具
    public static void ResetItems()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
    }
}
