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
    public TextMeshProUGUI[] slotInfoTexts; // 顯示每個存檔資訊 (可用Text或TMP_Text)
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


    public void OpenSaveMenu(string storyJson)
    {
        currentStoryJson = storyJson;
        saveMenu.SetActive(true);

        // 更新按鈕與資訊
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

        string savePath = Application.persistentDataPath + $"/save_{slotIndex}.json";
        File.WriteAllText(savePath, JsonUtility.ToJson(data, true));

        Debug.Log($"已存入存檔槽 {slotIndex + 1}");
        UpdateSlotInfo(slotIndex);
        saveMenu.SetActive(false);
    }

    public void UpdateSlotInfo(int slotIndex)
    {
        string savePath = Application.persistentDataPath + $"/save_{slotIndex}.json";
        if (File.Exists(savePath))
        {
            string json = File.ReadAllText(savePath);
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
