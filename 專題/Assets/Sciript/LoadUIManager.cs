using Ink.Runtime;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class LoadUIManager : MonoBehaviour
{
    public GameObject loadMenu;
    public Button[] loadButtons;
    public TextMeshProUGUI[] loadInfoTexts;

    public Button openButton;
    public Button closeButton;

    private void Start()
    {
        for (int i = 0; i < loadButtons.Length; i++)
        {
            int index = i;
            UpdateSlotInfo(index);
            loadButtons[i].onClick.AddListener(() => LoadSlot(index));
        }

        openButton?.onClick.AddListener(OpenMenu);
        closeButton?.onClick.AddListener(CloseMenu);
    }

    public void OpenMenu() => loadMenu.SetActive(true);
    public void CloseMenu() => loadMenu.SetActive(false);

    void UpdateSlotInfo(int index)
    {
        string path = Application.persistentDataPath + $"/save_{index}.json";
        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
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
        string path = Application.persistentDataPath + $"/save_{index}.json";
        if (!File.Exists(path))
        {
            Debug.LogWarning("該存檔不存在！");
            return;
        }

        string json = File.ReadAllText(path);
        SaveData data = JsonUtility.FromJson<SaveData>(json);

        InkDialogueManager.shouldAutoStartInk = false; // 🚫 不要讓新場景自動播放 Ink
        SceneManager.LoadScene(data.sceneName);
        StartCoroutine(LoadInkAfterScene(data));

    }

    private IEnumerator LoadInkAfterScene(SaveData data)
    {
        yield return null; // 等場景載入

        InkDialogueManager inkManager = FindObjectOfType<InkDialogueManager>();
        if (inkManager != null)
        {
            if (inkManager.story == null)
                inkManager.story = new Ink.Runtime.Story(inkManager.inkJSON.text);

            inkManager.story.state.LoadJson(data.storyState);
            inkManager.justLoaded = true;

            Debug.Log("✅ 成功載入 Ink 劇情狀態");
        }

        // 恢復允許自動啟動（給下一次新場景用）
        InkDialogueManager.shouldAutoStartInk = true;
    }

}
