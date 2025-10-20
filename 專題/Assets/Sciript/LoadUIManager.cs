using Ink.Runtime;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadUIManager : MonoBehaviour
{
    public GameObject loadMenu;
    public Button[] loadButtons;
    public TextMeshProUGUI[] loadInfoTexts;
    public InkDialogueManager inkManager;
    public GameObject player;

    public Button loadbotton;
    public Button closeButton;

    public void CloseMenu()
    {
        loadMenu.SetActive(false);
    }

    public void OpenMenu()
    {
        loadMenu.SetActive(true);
    }

    void Start()
    {
        // 更新所有讀取槽資訊
        for (int i = 0; i < loadButtons.Length; i++)
        {
            int index = i;
            UpdateSlotInfo(index);
            loadButtons[i].onClick.AddListener(() => LoadSlot(index));
        }
        loadbotton?.onClick.AddListener(OpenMenu);
        closeButton?.onClick.AddListener(CloseMenu);
    }

    void UpdateSlotInfo(int index)
    {
        string savePath = Application.persistentDataPath + $"/save_{index}.json";
        if (File.Exists(savePath))
        {
            string json = File.ReadAllText(savePath);
            SaveData data = JsonUtility.FromJson<SaveData>(json);
            loadInfoTexts[index].text = $"時間：{data.saveTime}\n場景：{data.sceneName}";
        }
        else
        {
            loadInfoTexts[index].text = "尚未存檔";
        }
    }

    void LoadSlot(int index)
    {
        string savePath = Application.persistentDataPath + $"/save_{index}.json";
        if (!File.Exists(savePath))
        {
            Debug.LogWarning("該存檔不存在！");
            return;
        }

        string json = File.ReadAllText(savePath);
        SaveData data = JsonUtility.FromJson<SaveData>(json);

        SceneManager.LoadScene(data.sceneName);

        // 在場景切換完後載入 Ink 劇情與玩家位置
        StartCoroutine(LoadAfterScene(data));
    }

    System.Collections.IEnumerator LoadAfterScene(SaveData data)
    {
        yield return null; // 等待場景載入
        inkManager.story.state.LoadJson(data.storyState);
        Debug.Log("讀取完成！");
    }
}
