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

    // ⭐ SceneInteractable 暫存互動完成（避免漏存）
    private static List<string> _pendingFinishedInteractions = new List<string>();

    private void Start()
    {
        closeButton?.onClick.AddListener(CloseMenu);
    }

    public void CloseMenu()
    {
        saveMenu.SetActive(false);
    }

    // ⭐ SceneInteractable 在 OnDialogueEnd() 會呼叫這裡
    public static void AddFinishedInteraction(string uniqueID)
    {
        if (!_pendingFinishedInteractions.Contains(uniqueID))
            _pendingFinishedInteractions.Add(uniqueID);

        Debug.Log($"[SaveUIManager] ⭐ 暫存互動完成：{uniqueID}");
    }

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

        // === HP ===
        HP hpRef = FindObjectOfType<HP>();
        if (hpRef != null)
            data.playerHp = hpRef.hp;

        // === 場景物件 ===
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


        // === 道具 ===
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

        // === 線索 ===
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


        // === ScriptableObject 永久收集 ===
        data.databaseCollectedClueIds.Clear();
        var anyClue = Resources.FindObjectsOfTypeAll<CluePickup>().FirstOrDefault(p => p?.clueData != null);
        if (anyClue != null)
        {
            foreach (var c in anyClue.clueData.clues)
                if (c.collected) data.databaseCollectedClueIds.Add(c.id);
        }

        data.databaseCollectedItemIds.Clear();
        var anyItem = Resources.FindObjectsOfTypeAll<ItemPickup>().FirstOrDefault(p => p?.itemData != null);
        if (anyItem != null)
        {
            foreach (var i in anyItem.itemData.items)
                if (i.collected) data.databaseCollectedItemIds.Add(i.id);
        }


        // === 互動物件 ===
        data.finishedInteractions.Clear();

        foreach (var inter in Resources.FindObjectsOfTypeAll<SceneInteractable>())
        {
            if (inter == null || !inter.gameObject.scene.IsValid()) continue;

            if (!inter.canInteract)
            {
                var id = inter.GetComponent<SaveableEntity>();
                if (id != null)
                {
                    data.finishedInteractions.Add(id.uniqueID);
                    Debug.Log($"[Save] 記錄互動物件：{inter.name}, id={id.uniqueID}");
                }
            }
        }

        // ⭐⭐⭐ 把暫存互動補上（你的 B 物件互動就是缺這步）⭐⭐⭐
        foreach (var id in _pendingFinishedInteractions)
        {
            if (!data.finishedInteractions.Contains(id))
            {
                data.finishedInteractions.Add(id);
                Debug.Log($"[Save] ⭐ 補上暫存互動：{id}");
            }
        }

        // ✅ 這裡是「根源修正」：存檔成功後清空暫存，不要一直累積舊資料
        _pendingFinishedInteractions.Clear();


        // === SpawnedObject ===
        data.spawnedObjects.Clear();
        foreach (var so in FindObjectsOfType<SaveableEntity>())
        {
            if (so.CompareTag("SpawnedObject"))
                data.spawnedObjects.Add(so.uniqueID);
        }

        // === NPC ===
        data.spawnedNPCs.Clear();
        foreach (var so in FindObjectsOfType<SaveableEntity>())
        {
            if (so.CompareTag("NPC"))
                data.spawnedNPCs.Add(so.uniqueID);
        }

        // === 敵人 ===
        if (EnemyStateManager.Instance != null)
            data.enemyStatesJson = EnemyStateManager.Instance.ToJson();

        // === 永久門 ===
        if (DoorManager.Instance != null)
            data.unlockedDoors = DoorManager.Instance.GetUnlockedDoorList();

        // === 寫入存檔 ===
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

    public float playerX;
    public float playerY;
    public float playerZ;

    public int playerHp;

    public string bedState;
    public string toiletState;
    public bool chestOpened;
    public string chestState;
    public bool safeOpened;

    public string enemyStatesJson;

    public List<string> collectedItems = new List<string>();
    public List<string> collectedClues = new List<string>();
    public List<string> finishedInteractions = new List<string>();

    public List<string> spawnedObjects = new List<string>();
    public List<string> spawnedNPCs = new List<string>();

    public List<string> databaseCollectedClueIds = new List<string>();
    public List<string> databaseCollectedItemIds = new List<string>();

    public List<string> unlockedDoors = new List<string>();
}
