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

        // === 劇情 ===
        data.storyState = currentStoryJson;
        data.sceneName = SceneManager.GetActiveScene().name;
        data.saveTime = DateTime.Now.ToString("yyyy/MM/dd HH:mm");

        // === 玩家位置 ===
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            Vector3 pos = player.transform.position;
            data.playerX = pos.x;
            data.playerY = pos.y;
            data.playerZ = pos.z;
        }

        // === 玩家 HP ===
        HP hpRef = FindObjectOfType<HP>();
        if (hpRef != null)
            data.playerHp = hpRef.hp;

        // === 場景物件狀態 ===
        var bed = FindObjectOfType<BedController>();
        if (bed != null) data.bedState = bed.GetCurrentState();

        var toilet = FindObjectOfType<toiletController>();
        if (toilet != null) data.toiletState = toilet.GetCurrentState();

        var chest = FindObjectOfType<ChestController>();
        if (chest != null)
        {
            data.chestOpened = chest.isUnlocked;
            data.chestState = chest.GetCurrentState();
        }

        var safe = FindObjectOfType<SafeController>();
        if (safe != null)
            data.safeOpened = safe.isUnlocked;

        // === 儲存「場景實體的道具」 ===
        data.collectedItems.Clear();
        foreach (var pickup in Resources.FindObjectsOfTypeAll<ItemPickup>())
        {
            if (pickup == null || !pickup.gameObject.scene.IsValid()) continue;
            if (pickup.collected)
            {
                var id = pickup.GetComponent<SaveableEntity>();
                if (id != null) data.collectedItems.Add(id.uniqueID);
            }
        }

        // === 儲存「場景實體的線索」 ===
        data.collectedClues.Clear();

        foreach (var pickup in Resources.FindObjectsOfTypeAll<CluePickup>())
        {

            if (pickup == null || !pickup.gameObject.scene.IsValid()) continue;
            if (pickup.collected)
            {
                var id = pickup.GetComponent<SaveableEntity>();
                if (id != null) data.collectedClues.Add(id.uniqueID);
            }
        }

        // === ⭐ ScriptableObject：儲存資料庫的 Collected 狀態（永不復活）⭐⭐
        data.databaseCollectedClueIds.Clear();
        var anyClue = Resources.FindObjectsOfTypeAll<CluePickup>().FirstOrDefault(p => p?.clueData != null);
        if (anyClue != null)
        {
            Debug.Log("ClueDB saved instance: " + anyClue.clueData.GetInstanceID());

            foreach (var c in DatabaseSingleton.ClueDB.clues)
                if (c.collected)
                    data.databaseCollectedClueIds.Add(c.id);

        }

        data.databaseCollectedItemIds.Clear();
        var anyItem = Resources.FindObjectsOfTypeAll<ItemPickup>().FirstOrDefault(p => p?.itemData != null);
        if (anyItem != null)
        {
            foreach (var i in DatabaseSingleton.ItemDB.items)
                if (i.collected)
                    data.databaseCollectedItemIds.Add(i.id);

        }

        // === 儲存互動物件 ===
        data.finishedInteractions.Clear();
        foreach (var inter in FindObjectsOfType<SceneInteractable>())
        {
            if (inter == null || !inter.gameObject.scene.IsValid()) continue;
            if (!inter.canInteract)
            {
                var id = inter.GetComponent<SaveableEntity>();
                if (id != null) data.finishedInteractions.Add(id.uniqueID);
            }
        }

        // === 儲存箱子（SpawnedObject）===
        data.spawnedObjects.Clear();
        foreach (var so in FindObjectsOfType<SaveableEntity>())
        {
            if (so.CompareTag("SpawnedObject"))
                data.spawnedObjects.Add(so.uniqueID);
        }

        // === 儲存 NPC ===
        data.spawnedNPCs.Clear();
        foreach (var so in FindObjectsOfType<SaveableEntity>())
        {
            if (so.CompareTag("NPC"))
                data.spawnedNPCs.Add(so.uniqueID);
        }


        data.spawnedEnemys.Clear();
        foreach (var enemy in Resources.FindObjectsOfTypeAll<SaveableEntity>())
        {
            if (enemy == null || !enemy.gameObject.scene.IsValid()) continue;
                var id = enemy.GetComponent<SaveableEntity>();
            if (enemy.CompareTag("EnemyHide"))
                data.spawnedEnemys.Add(id.uniqueID);
        }

        // === 儲存敵人 ===
        if (EnemyStateManager.Instance != null)
            data.enemyStatesJson = EnemyStateManager.Instance.ToJson();


        // === ⭐⭐ 儲存永久解鎖的門 ⭐⭐
        if (DoorManager.Instance != null)
            data.unlockedDoors = DoorManager.Instance.GetUnlockedDoorList();

        // ===== 寫入檔案 =====
        string path = Application.persistentDataPath + $"/save_{slotIndex}.json";
        File.WriteAllText(path, JsonUtility.ToJson(data, true));

        Debug.Log("💾 已存檔：" + path);
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

    // 位置
    public float playerX;
    public float playerY;
    public float playerZ;

    public int playerHp;

    // 場景物件狀態
    public string bedState;
    public string toiletState;
    public bool chestOpened;
    public string chestState;
    public bool safeOpened;

    public string enemyStatesJson;

    // 場景內實體 uniqueID 清單
    public List<string> collectedItems = new List<string>();
    public List<string> collectedClues = new List<string>();
    public List<string> finishedInteractions = new List<string>();

    // 生成物件
    public List<string> spawnedObjects = new List<string>(); // chest
    public List<string> spawnedNPCs = new List<string>();    // NPC
    public List<string> spawnedEnemys = new List<string>();    // Enemy


    // ScriptableObject：資料庫中的「已收集 id」
    public List<string> databaseCollectedClueIds = new List<string>();
    public List<string> databaseCollectedItemIds = new List<string>();

    // ⭐ 新增：永久解鎖的門（uniqueID）
    public List<string> unlockedDoors = new List<string>();
}
