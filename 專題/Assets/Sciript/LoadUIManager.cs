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

        // 🔐 Chest 狀態
        if (!string.IsNullOrEmpty(data.chestState))
        {
            ChestController.pendingOverrideState = data.chestState;
        }
        else
        {
            ChestController.pendingOverrideState = data.chestOpened ? "Open" : "Closed";
        }

        var safe = GameObject.FindObjectOfType<SafeController>();
        if (safe != null) safe.isUnlocked = data.safeOpened;

        // ⭐⭐ 還原 ClueData / ItemData collected 狀態 ⭐⭐
        ClueData clueDB = null;
        ItemData itemDB = null;

        var clueSample = GameObject.FindObjectOfType<CluePickup>();
        if (clueSample != null) clueDB = clueSample.clueData;

        var itemSample = GameObject.FindObjectOfType<ItemPickup>();
        if (itemSample != null) itemDB = itemSample.itemData;

        if (clueDB != null)
        {
            foreach (var clue in clueDB.clues)
                clue.collected = data.databaseCollectedClueIds.Contains(clue.id);
        }

        if (itemDB != null)
        {
            foreach (var item in itemDB.items)
                item.collected = data.databaseCollectedItemIds.Contains(item.id);
        }

        // 🛠 UI 系統修復
        var evt = UnityEngine.EventSystems.EventSystem.current;
        if (evt == null)
        {
            new GameObject("EventSystem",
                typeof(UnityEngine.EventSystems.EventSystem),
                typeof(UnityEngine.EventSystems.StandaloneInputModule));
        }
        else evt.enabled = true;

        foreach (var canvas in GameObject.FindObjectsOfType<Canvas>())
        {
            canvas.overrideSorting = true;
            var ray = canvas.GetComponent<UnityEngine.UI.GraphicRaycaster>();
            if (ray == null) canvas.gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        }

        // === 道具 ===
        foreach (var item in GameObject.FindObjectsOfType<ItemPickup>())
        {
            var id = item.GetComponent<SaveableEntity>();
            if (id != null && data.collectedItems.Contains(id.uniqueID))
            {
                item.collected = true;        // 🔥 必須還原
                item.gameObject.SetActive(false);
            }
        }

        // === 線索 ===
        foreach (var clue in GameObject.FindObjectsOfType<CluePickup>())
        {
            var id = clue.GetComponent<SaveableEntity>();
            if (id != null && data.collectedClues.Contains(id.uniqueID))
            {
                clue.collected = true;         // 🔥 必須還原
                clue.gameObject.SetActive(false);
            }
        }

        // === 互動物件 ===
        foreach (var inter in GameObject.FindObjectsOfType<SceneInteractable>())
        {
            var id = inter.GetComponent<SaveableEntity>();
            if (id != null && data.finishedInteractions.Contains(id.uniqueID))
                inter.canInteract = false;
        }

        // === 生成箱子 ===
        foreach (string id in data.spawnedObjects)
        {
            bool found = false;
            foreach (var so in GameObject.FindObjectsOfType<SaveableEntity>())
                if (so.uniqueID == id) found = true;

            if (!found)
            {
                var beds = GameObject.FindObjectOfType<BedController>();
                if (beds != null) beds.SpawnObject("chest");
            }
        }

        // === 生成 NPC ===
        foreach (string id in data.spawnedNPCs)
        {
            bool found = false;
            foreach (var npc in GameObject.FindObjectsOfType<SaveableEntity>())
                if (npc.uniqueID == id) found = true;

            if (!found)
            {
                var npcManager = GameObject.FindObjectOfType<NPCManager>();
                if (npcManager != null) npcManager.SpawnNPC("Guard");
            }
        }

        // === 敵人狀態 ===
        if (!string.IsNullOrEmpty(data.enemyStatesJson))
        {
            if (EnemyStateManager.Instance != null)
                EnemyStateManager.Instance.LoadFromJson(data.enemyStatesJson);
        }

        Debug.Log("✅ 載入資料全部還原完成");
    }
}
