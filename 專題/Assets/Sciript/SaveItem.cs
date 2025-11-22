using System.Collections.Generic;
using UnityEngine;

public class SaveItem : MonoBehaviour
{
    // 儲存線索
    public static void SaveItems(string id)
    {
        PlayerPrefs.SetInt("item_" + id, 1);
        PlayerPrefs.Save(); // 立即寫入
    }

    // 是否已經取得線索
    public static bool HasItem(string id)
    {
        return PlayerPrefs.GetInt("item_" + id, 0) == 1;
    }

    // 重置所有線索（如果你需要）
    public static void ResetItems()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
    }


}
