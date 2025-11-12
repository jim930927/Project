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

        // 🔹 將資料暫存起來
        pendingLoadData = data;

        InkDialogueManager.shouldAutoStartInk = false;

        // 🩹 修正：避免 EventSystem 重複
        var evt = UnityEngine.EventSystems.EventSystem.current;
        if (evt != null)
        {
            GameObject.Destroy(evt.gameObject);
            Debug.Log("🧹 已刪除舊 EventSystem，避免 UI 鎖死");
        }

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

        // ✅ 改為：不再直接尋找 ChestController，而是預先設定靜態狀態覆寫
        if (!string.IsNullOrEmpty(data.chestState))
        {
            ChestController.pendingOverrideState = data.chestState;
            Debug.Log($"🟢 [Load] 設定 ChestController.pendingOverrideState = {data.chestState}");
        }
        else
        {
            ChestController.pendingOverrideState = data.chestOpened ? "Open" : "Closed";
            Debug.Log($"🟡 [Load] 使用舊欄位 chestOpened -> {ChestController.pendingOverrideState}");
        }

        var safe = GameObject.FindObjectOfType<SafeController>();
        if (safe != null) safe.isUnlocked = data.safeOpened;

        // 🩹 確保 EventSystem 存在且可互動
        var evt = UnityEngine.EventSystems.EventSystem.current;
        if (evt == null)
        {
            GameObject newEvt = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.EventSystems.StandaloneInputModule));
            Debug.Log("⚙️ 自動建立新的 EventSystem");
        }
        else
        {
            evt.enabled = true;
            Debug.Log("⚙️ EventSystem 已啟用");
        }

        // 🩹 修正：重設所有 Canvas 的互動層級
        int baseOrder = 0;
        foreach (var canvas in GameObject.FindObjectsOfType<Canvas>())
        {
            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay ||
                canvas.renderMode == RenderMode.ScreenSpaceCamera)
            {
                canvas.overrideSorting = true;
                canvas.sortingOrder = baseOrder++;
            }

            // 確保 Canvas 可以互動
            var ray = canvas.GetComponent<UnityEngine.UI.GraphicRaycaster>();
            if (ray == null)
                canvas.gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        }
        Debug.Log("🧩 已重建 Canvas SortingOrder 與 Raycaster");

        // === 道具 ===
        foreach (var item in FindObjectsOfType<ItemPickup>())
        {
            var id = item.GetComponent<SaveableEntity>();
            if (id != null && data.collectedItems.Contains(id.uniqueID))
                item.gameObject.SetActive(false);
        }

        // === 線索 ===
        foreach (var clue in FindObjectsOfType<CluePickup>())
        {
            var id = clue.GetComponent<SaveableEntity>();
            if (id != null && data.collectedClues.Contains(id.uniqueID))
                clue.gameObject.SetActive(false);
        }

        // === 互動物件 ===
        foreach (var inter in FindObjectsOfType<SceneInteractable>())
        {
            var id = inter.GetComponent<SaveableEntity>();
            if (id != null && data.finishedInteractions.Contains(id.uniqueID))
                inter.canInteract = false;
        }

        // === 生成箱子 ===
        foreach (string id in data.spawnedObjects)
        {
            bool found = false;
            foreach (var so in FindObjectsOfType<SaveableEntity>())
                if (so.uniqueID == id) found = true;

            if (!found)
            {
                var beds = FindObjectOfType<BedController>();
                if (beds != null) beds.SpawnObject("chest");
            }
        }

        // === 生成 NPC ===
        foreach (string id in data.spawnedNPCs)
        {
            bool found = false;
            foreach (var npc in FindObjectsOfType<SaveableEntity>())
                if (npc.uniqueID == id) found = true;

            if (!found)
            {
                var npcManager = FindObjectOfType<NPCManager>();
                if (npcManager != null) npcManager.SpawnNPC("Guard");
            }
        }

        Debug.Log($"📜 載入結果：道具 {data.collectedItems.Count}、線索 {data.collectedClues.Count}、互動 {data.finishedInteractions.Count}、生成物 {data.spawnedObjects.Count}、NPC {data.spawnedNPCs.Count}");
        Debug.Log("✅ 存檔資料已完整還原（Ink + 玩家位置 + 場景物件）");
    }
}
