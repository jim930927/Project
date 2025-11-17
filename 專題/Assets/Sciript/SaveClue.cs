using System.Collections.Generic;
using UnityEngine;

public class SaveClue : MonoBehaviour
{
    // 儲存線索
    public static void SaveClues(string id)
    {
        PlayerPrefs.SetInt("clue_" + id, 1);
        PlayerPrefs.Save(); // 立即寫入
    }

    // 是否已經取得線索
    public static bool HasClue(string id)
    {
        return PlayerPrefs.GetInt("clue_" + id, 0) == 1;
    }

    // 重置所有線索（如果你需要）
    public static void ResetClues()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
    }


}
