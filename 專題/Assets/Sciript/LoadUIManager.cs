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

    public static SaveData pendingLoadData; // 🔹 跨場景保存資料

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

        // 🔹 將資料暫存起來，讓下一個場景的 InkDialogueManager 讀取
        pendingLoadData = data;

        InkDialogueManager.shouldAutoStartInk = false;
        SceneManager.LoadScene(data.sceneName);
    }

    // ✅ 在新場景中由 InkDialogueManager 呼叫
    public static IEnumerator ApplyPendingLoadData()
    {
        if (pendingLoadData == null)
            yield break;

        yield return new WaitForSeconds(0.1f); // 確保新場景物件初始化完畢

        SaveData data = pendingLoadData;
        pendingLoadData = null; // 清除暫存

        InkDialogueManager inkManager = GameObject.FindObjectOfType<InkDialogueManager>();
        if (inkManager == null)
        {
            Debug.LogError("❌ 找不到 InkDialogueManager");
            yield break;
        }

        inkManager.ReloadInkState(data.storyState);

        // 恢復玩家位置
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            player.transform.position = new Vector3(data.playerX, data.playerY, data.playerZ);
            var pm = player.GetComponent<Player>();
            if (pm != null) pm.canMove = true;
        }

        // 恢復 HP
        HP hpRef = GameObject.FindObjectOfType<HP>();
        if (hpRef != null)
            hpRef.hp = data.playerHp;

        // 恢復場景物件狀態
        var bed = GameObject.FindObjectOfType<BedController>();
        if (bed != null && !string.IsNullOrEmpty(data.bedState))
            bed.ChangeImage(data.bedState);

        var toilet = GameObject.FindObjectOfType<toiletController>();
        if (toilet != null && !string.IsNullOrEmpty(data.toiletState))
            toilet.ChangeImage(data.toiletState);

        var chest = GameObject.FindObjectOfType<ChestController>();
        if (chest != null) chest.isUnlocked = data.chestOpened;

        var safe = GameObject.FindObjectOfType<SafeController>();
        if (safe != null) safe.isUnlocked = data.safeOpened;

        UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(null);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Debug.Log("✅ 存檔資料已完整還原（Ink + 玩家位置 + 場景物件）");
    }
}
