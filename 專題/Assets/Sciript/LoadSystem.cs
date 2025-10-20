using System.IO;
using UnityEngine;
using Ink.Runtime;
using UnityEngine.SceneManagement;

public class LoadSystem : MonoBehaviour
{
    public static void LoadSlot(int slotIndex, InkDialogueManager inkManager, GameObject player)
    {
        string savePath = Application.persistentDataPath + $"/save_{slotIndex}.json";
        if (!File.Exists(savePath))
        {
            Debug.LogWarning("該存檔不存在！");
            return;
        }

        string json = File.ReadAllText(savePath);
        SaveData data = JsonUtility.FromJson<SaveData>(json);

        // 載入場景
        SceneManager.LoadScene(data.sceneName);
        // 在場景載入完成後還原 Ink 狀態與玩家位置
        inkManager.story.state.LoadJson(data.storyState);
        //player.transform.position = new Vector2(data.playerX, data.playerY);
    }
}
