using Ink.Runtime;
using System.Collections;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static ClueData;

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

        // 暫存存檔資料
        pendingLoadData = data;

        InkDialogueManager.shouldAutoStartInk = false;

        // 避免 EventSystem 重複
        var evt = UnityEngine.EventSystems.EventSystem.current;
        if (evt != null)
        {
            GameObject.Destroy(evt.gameObject);
        }

        SceneManager.LoadScene(data.sceneName);
    }

    // === 在新場景中由 InkDialogueManager 呼叫 ===
    public static IEnumerator ApplyPendingLoadData()
    {
        if (pendingLoadData == null)
            yield break;

        yield return new WaitForSeconds(0.1f);

        SaveData data = pendingLoadData;
        pendingLoadData = null;

        // === Ink ===
        InkDialogueManager inkManager = GameObject.FindObjectOfType<InkDialogueManager>();
        if (inkManager == null)
        {
            Debug.LogError("❌ 找不到 InkDialogueManager");
            yield break;
        }

        inkManager.ReloadInkState(data.storyState);

        // === 恢復玩家位置 ===
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            player.transform.position = new Vector3(data.playerX, data.playerY, data.playerZ);
            var pm = player.GetComponent<Player>();
            if (pm != null) pm.canMove = true;
        }

        // === 恢復 HP ===
        HP hpRef = GameObject.FindObjectOfType<HP>();
        if (hpRef != null)
            hpRef.hp = data.playerHp;

        // === 恢復場景物件 ===
        var bed = GameObject.FindObjectOfType<BedController>();
        if (bed != null && !string.IsNullOrEmpty(data.bedState))
            bed.ChangeImage(data.bedState);

        var toilet = GameObject.FindObjectOfType<toiletController>();
        if (toilet != null && !string.IsNullOrEmpty(data.toiletState))
            toilet.ChangeImage(data.toiletState);

        // === 恢復箱子狀態 ===
        if (!string.IsNullOrEmpty(data.chestState))
            ChestController.pendingOverrideState = data.chestState;
        else
            ChestController.pendingOverrideState = data.chestOpened ? "Open" : "Closed";

        var safe = GameObject.FindObjectOfType<SafeController>();
        if (safe != null)
            safe.isUnlocked = data.safeOpened;

        // === 還原 ScriptableObject：線索、道具 ===
        ClueData clueDB = null;
        ItemData itemDB = null;

        var clueSample = GameObject.FindObjectOfType<CluePickup>();
        if (clueSample != null) clueDB = clueSample.clueData;

        var itemSample = GameObject.FindObjectOfType<ItemPickup>();
        if (itemSample != null) itemDB = itemSample.itemData;

        // === 從存檔還原 ScriptableObject 狀態 ===
        if (clueDB != null)
        {

            clueDB.SyncFromSave(data.databaseCollectedClueIds);
            Debug.Log("Loaded Clue IDs: " + string.Join(",", data.databaseCollectedClueIds));
            foreach (var clue in clueDB.clues)
            {
                clue.collected = data.databaseCollectedClueIds.Contains(clue.id);
                clue.collectedTime = clue.collected ? Time.time : 0f;
            }
        }

        Debug.Log("ClueDB instance: " + clueDB.GetInstanceID());


        if (clueDB != null)
        {
            // 同步到 ScriptableObject（請確保你的 ClueData.SyncFromSave 接受 List<string>）
            clueDB.SyncFromSave(data.databaseCollectedClueIds);
            Debug.Log("Applied Clue IDs to ClueDB: " + string.Join(",", data.databaseCollectedClueIds));

            // 兼容舊有的 PlayerPrefs 機制：把已收集的 id 寫回 PlayerPrefs（避免 UI 依賴 PlayerPrefs 時出事）
            if (data.databaseCollectedClueIds != null)
            {
                foreach (var id in data.databaseCollectedClueIds)
                {
                    PlayerPrefs.SetInt("clue_" + id, 1);
                }
                PlayerPrefs.Save();
            }
        }

        if (itemDB != null)
        {
            itemDB.SyncFromSave(data.databaseCollectedItemIds);
            Debug.Log("Applied Item IDs to ItemDB: " + string.Join(",", data.databaseCollectedItemIds));

            if (data.databaseCollectedItemIds != null)
            {
                foreach (var id in data.databaseCollectedItemIds)
                {
                    PlayerPrefs.SetInt("item_" + id, 1);
                }
                PlayerPrefs.Save();
            }
        }

        var book = GameObject.FindObjectOfType<BookUIManager>();
        if (book != null)
        {
            book.currentClueListPage = 0;
            book.currentItemListPage = 0;
            book.GenerateClueButtons();
            book.GenerateItemButtons();
            Debug.Log("Book UI regenerated after load. Clue buttons count: " + book.transform.childCount);
        }

        // === UI 修復 ===
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
            if (canvas.GetComponent<UnityEngine.UI.GraphicRaycaster>() == null)
                canvas.gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        }

        // === 移除已撿到的「場景實體道具」 ===
        foreach (var item in GameObject.FindObjectsOfType<ItemPickup>())
        {
            var id = item.GetComponent<SaveableEntity>();
            if (id != null && data.collectedItems.Contains(id.uniqueID))
            {
                item.collected = true;
                item.gameObject.SetActive(false);
            }
        }

        // === 移除已撿到的「場景實體線索」 ===
        foreach (var clue in GameObject.FindObjectsOfType<CluePickup>())
        {
            var id = clue.GetComponent<SaveableEntity>();
            if (id != null && data.collectedClues.Contains(id.uniqueID))
            {
                clue.collected = true;
                clue.gameObject.SetActive(false);
            }
        }

        // === 還原互動物件 ===
        foreach (var inter in GameObject.FindObjectsOfType<SceneInteractable>())
        {
            var id = inter.GetComponent<SaveableEntity>();
            if (id != null && data.finishedInteractions.Contains(id.uniqueID))
                inter.canInteract = false;
        }

        // === 生成箱子 ===
        foreach (string id in data.spawnedObjects)
        {
            bool exists = false;
            foreach (var so in GameObject.FindObjectsOfType<SaveableEntity>())
            {
                if (so.uniqueID == id)
                {
                    exists = true;
                    break;
                }
            }

            if (!exists)
            {
                var beds = GameObject.FindObjectOfType<BedController>();
                if (beds != null) beds.SpawnObject("chest");
            }
        }

        // === 生成 NPC ===
        foreach (string id in data.spawnedNPCs)
        {
            bool exists = false;
            foreach (var so in GameObject.FindObjectsOfType<SaveableEntity>())
            {
                if (so.uniqueID == id)
                {
                    exists = true;
                    break;
                }
            }

            if (!exists)
            {
                var npcManager = GameObject.FindObjectOfType<NPCManager>();
                if (npcManager != null) npcManager.SpawnNPC("Guard");
            }
        }

        // === 移除已撿到的「場景實體敵人」 ===
        foreach (var enemy in GameObject.FindObjectsOfType<SaveableEntity>())
        {
            var id = enemy.GetComponent<SaveableEntity>();
            if (id != null && data.spawnedEnemys.Contains(id.uniqueID))
            {
                enemy.gameObject.SetActive(false);
            }
        }

        // === 敵人狀態 ===
        if (!string.IsNullOrEmpty(data.enemyStatesJson))
        {
            if (EnemyStateManager.Instance != null)
                EnemyStateManager.Instance.LoadFromJson(data.enemyStatesJson);
        }

        // ⭐⭐⭐ === 還原「永久解鎖的門」=== ⭐⭐⭐
        if (DoorManager.Instance != null && data.unlockedDoors != null)
        {
            foreach (string doorID in data.unlockedDoors)
            {
                DoorManager.Instance.UnlockDoor(doorID);
            }
        }

        // 🟢 讀檔後恢復背景音樂（如果之前有播放過）
        var bgm = GameObject.FindObjectOfType<BGMManager>();
        if (bgm != null)
        {
            bgm.ResumeMusic();   // ← 這會繼續播放 audioSource 裡的音樂
            Debug.Log("🎵 已恢復讀檔前的背景音樂");
        }


        Debug.Log("✅ 載入資料全部還原完成（包含永久解鎖的門）");
    }

    public static void ResetDatabase()
    {
        var clue = Resources.Load<ClueData>("ClueDatabase");
        var item = Resources.Load<ItemData>("ItemDatabase");

        clue?.ResetAll();
        item?.ResetAll();

        SaveClue.ResetClues();
        SaveItem.ResetItems();

        Debug.Log("📕 已重置線索與道具資料庫");
    }

}
