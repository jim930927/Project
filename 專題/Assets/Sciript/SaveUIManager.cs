using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Linq;

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

        // 儲存 Ink 劇情狀態
        data.storyState = currentStoryJson;
        data.sceneName = SceneManager.GetActiveScene().name;
        data.saveTime = DateTime.Now.ToString("yyyy/MM/dd HH:mm");

        // 🔹 儲存玩家位置
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            Vector3 pos = player.transform.position;
            data.playerX = pos.x;
            data.playerY = pos.y;
            data.playerZ = pos.z;
        }

        // 🔹 儲存 HP
        HP hpRef = FindObjectOfType<HP>();
        if (hpRef != null)
            data.playerHp = hpRef.hp;

        // 🔹 儲存場景物件狀態（舉例）
        var bed = FindObjectOfType<BedController>();
        if (bed != null) data.bedState = bed.GetCurrentState();

        var toilet = FindObjectOfType<toiletController>();
        if (toilet != null) data.toiletState = toilet.GetCurrentState();

        var chest = FindObjectOfType<ChestController>();
        if (chest != null)
        {
            data.chestOpened = chest.isUnlocked;           // 舊欄位保留相容
            data.chestState = chest.GetCurrentState();     // ✅ 新增
        }

        var safe = FindObjectOfType<SafeController>();
        if (safe != null) data.safeOpened = safe.isUnlocked;

        // === 儲存可撿道具（場景實體，用 uniqueID）===
        data.collectedItems.Clear();
        foreach (var pickup in Resources.FindObjectsOfTypeAll<ItemPickup>())
        {
            if (pickup == null) continue;
            if (!pickup.gameObject.scene.IsValid()) continue; // 排除預製件
            if (pickup.collected)
            {
                var id = pickup.GetComponent<SaveableEntity>();
                if (id != null)
                    data.collectedItems.Add(id.uniqueID);
            }
        }

        // === 儲存線索（場景實體，用 uniqueID）===
        data.collectedClues.Clear();
        foreach (var pickup in Resources.FindObjectsOfTypeAll<CluePickup>())
        {
            if (pickup == null) continue;
            if (!pickup.gameObject.scene.IsValid()) continue; // 排除預製件
            if (pickup.collected)
            {
                var id = pickup.GetComponent<SaveableEntity>();
                if (id != null)
                    data.collectedClues.Add(id.uniqueID);
            }
        }

        // ⭐⭐ 新增：把「資料庫裡已收集的線索 / 道具」也記錄起來 ⭐⭐

        // --- ClueData ---
        data.databaseCollectedClueIds.Clear();
        var anyCluePickup = Resources.FindObjectsOfTypeAll<CluePickup>()
                                     .FirstOrDefault(p => p != null && p.clueData != null);
        if (anyCluePickup != null)
        {
            ClueData clueDB = anyCluePickup.clueData;
            foreach (var clue in clueDB.clues)
            {
                if (clue.collected)
                    data.databaseCollectedClueIds.Add(clue.id);
            }
        }

        // --- ItemData ---
        data.databaseCollectedItemIds.Clear();
        var anyItemPickup = Resources.FindObjectsOfTypeAll<ItemPickup>()
                                     .FirstOrDefault(p => p != null && p.itemData != null);
        if (anyItemPickup != null)
        {
            ItemData itemDB = anyItemPickup.itemData;
            foreach (var item in itemDB.items)
            {
                if (item.collected)
                    data.databaseCollectedItemIds.Add(item.id);
            }
        }

        // === 儲存互動物件 ===
        data.finishedInteractions.Clear();
        foreach (var inter in FindObjectsOfType<SceneInteractable>())
        {
            if (inter == null) continue;
            if (!inter.gameObject.scene.IsValid()) continue; // 排除預製件
            if (inter.canInteract)
            {
                var id = inter.GetComponent<SaveableEntity>();
                if (id != null)
                    data.finishedInteractions.Add(id.uniqueID);
            }
        }

        // === 儲存生成的箱子 ===
        data.spawnedObjects.Clear();
        foreach (var interactables in FindObjectsOfType<SceneInteractable>())
        {
            if (interactables == null) continue;
            foreach (var so in FindObjectsOfType<SaveableEntity>())
            {
                if (so.CompareTag("SpawnedObject"))
                    data.spawnedObjects.Add(so.uniqueID);
            }
        }

        // === 儲存 NPC ===
        data.spawnedNPCs.Clear();
        foreach (var npc in FindObjectsOfType<SaveableEntity>())
        {
            if (npc.CompareTag("NPC"))
                data.spawnedNPCs.Add(npc.uniqueID);
        }

        // === 儲存敵人狀態 ===
        if (EnemyStateManager.Instance != null)
        {
            data.enemyStatesJson = EnemyStateManager.Instance.ToJson();
            Debug.Log($"🧠 已儲存敵人狀態：{data.enemyStatesJson.Length} 字元");
        }

        Debug.Log($"💾 Save Completed: 道具 {data.collectedItems.Count}、線索 {data.collectedClues.Count}、互動 {data.finishedInteractions.Count}、箱子 {data.spawnedObjects.Count}、NPC {data.spawnedNPCs.Count}");

        string path = Application.persistentDataPath + $"/save_{slotIndex}.json";
        File.WriteAllText(path, JsonUtility.ToJson(data, true));

        Debug.Log($"💾 已存入存檔槽 {slotIndex + 1}");
        Debug.Log("存檔位置：" + Application.persistentDataPath);

        UpdateSlotInfo(slotIndex);
        saveMenu.SetActive(false);

        Debug.Log("✅ Save Completed. Items:" + data.collectedItems.Count + " Interacts:" + data.finishedInteractions.Count + " Clues:" + data.collectedClues.Count);
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
    public string chestState; // ✅ 新增

    // 位置
    public float playerX;
    public float playerY;
    public float playerZ;

    public int playerHp;

    // 場景物件狀態
    public string bedState;
    public string toiletState;
    public bool chestOpened;
    public bool safeOpened;
    public string enemyStatesJson; // 敵人狀態 JSON

    // 場景內實體物件的 uniqueID
    public List<string> collectedItems = new List<string>();
    public List<string> collectedClues = new List<string>();
    public List<string> finishedInteractions = new List<string>();
    public List<string> spawnedObjects = new List<string>();
    public List<string> spawnedNPCs = new List<string>();

    // ⭐⭐ 新增：ScriptableObject 資料庫裡「哪些條目已 collected」 ⭐⭐
    public List<string> databaseCollectedClueIds = new List<string>();
    public List<string> databaseCollectedItemIds = new List<string>();
}
