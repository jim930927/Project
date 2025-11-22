using UnityEngine;
using UnityEngine.SceneManagement;

public class GameResetManager : MonoBehaviour
{
    public ClueData clueData;  // 指到你的 ClueDatabase
    public ItemData itemData;  // 指到你的 ClueDatabase
    public HP hp;              // 指到你的 HP 單例 (也可用 HP.Instance)

    public void StartNewGame()
    {
        ResetHP();
        ResetClues();
        ResetItems(); 

        // 🟦 重新載入第一個遊戲場景
        SceneManager.LoadScene("第一關的場景名稱");
    }

    void ResetHP()
    {
        if (hp == null) hp = FindObjectOfType<HP>();
        if (hp != null)
        {
            hp.hp = 3;
            hp.hasShownHP = false;
            Debug.Log("🩸 HP 已重置為 3");

            // 等進到新場景後 HP.cs 的 OnSceneLoaded 會自動抓 UI
        }
    }

    void ResetClues()
    {
        if (clueData != null)
        {
            clueData.ResetAll();
            Debug.Log("📜 所有線索已重置");
        }
    }

    void ResetItems()
    {
        if (clueData != null)
        {
            itemData.ResetAll();
            Debug.Log("🎒 道具已重置（若有）");
        }
    }
}
