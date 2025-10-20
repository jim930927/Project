using System;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SaveUIManager : MonoBehaviour
{
    public GameObject saveMenu;
    public Button[] slotButtons;
    public TextMeshProUGUI[] slotInfoTexts;
    private string currentStoryJson;
    public Button closeButton;

    private void Start()
    {
        closeButton?.onClick.AddListener(CloseMenu);
    }

    public void CloseMenu()
    {
        saveMenu.SetActive(false);
    }

    // InkDialogueManager 呼叫 ~SaveGame() 時會傳入 story JSON
    public void OpenSaveMenu(string storyJson)
    {
        currentStoryJson = storyJson;
        saveMenu.SetActive(true);

        for (int i = 0; i < slotButtons.Length; i++)
        {
            int index = i;
            slotButtons[i].onClick.RemoveAllListeners();
            slotButtons[i].onClick.AddListener(() => SaveToSlot(index));
            UpdateSlotInfo(index);
        }
    }

    void SaveToSlot(int slotIndex)
    {
        SaveData data = new SaveData();
        data.storyState = currentStoryJson;
        data.sceneName = SceneManager.GetActiveScene().name;
        data.saveTime = DateTime.Now.ToString("yyyy/MM/dd HH:mm");

        string path = Application.persistentDataPath + $"/save_{slotIndex}.json";
        File.WriteAllText(path, JsonUtility.ToJson(data, true));

        Debug.Log($"💾 已存入存檔槽 {slotIndex + 1}");
        Debug.Log("存檔位置：" + Application.persistentDataPath);
        UpdateSlotInfo(slotIndex);
        saveMenu.SetActive(false);
    }

    public void UpdateSlotInfo(int slotIndex)
    {
        string path = Application.persistentDataPath + $"/save_{slotIndex}.json";
        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            SaveData data = JsonUtility.FromJson<SaveData>(json);
            slotInfoTexts[slotIndex].text =
                $"存檔時間：{data.saveTime}\n場景：{data.sceneName}";
        }
        else
        {
            slotInfoTexts[slotIndex].text = "尚未存檔";
        }
    }
}

[System.Serializable]
public class SaveData
{
    public string storyState;
    public string sceneName;
    public string saveTime;
}
