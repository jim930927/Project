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
        if (chest != null) data.chestOpened = chest.isUnlocked;

        var safe = FindObjectOfType<SafeController>();
        if (safe != null) data.safeOpened = safe.isUnlocked;

        // === 儲存可撿道具 ===
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
        // === 儲存線索 ===
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

        // === 儲存可撿道具 ===
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

        Debug.Log($"💾 Save Completed: 道具 {data.collectedItems.Count}、線索 {data.collectedClues.Count}、互動 {data.finishedInteractions.Count}、箱子 {data.spawnedObjects.Count}、NPC {data.spawnedNPCs.Count}");




        string path = Application.persistentDataPath + $"/save_{slotIndex}.json";
        File.WriteAllText(path, JsonUtility.ToJson(data, true));

        Debug.Log($"💾 已存入存檔槽 {slotIndex + 1}");
        Debug.Log("存檔位置：" + Application.persistentDataPath);

        UpdateSlotInfo(slotIndex);
        saveMenu.SetActive(false);

        Debug.Log("✅ Save Completed. Items:" + data.collectedItems.Count + " Interacts:" + data.finishedInteractions.Count + "Clues:" + data.collectedClues.Count);

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

    // 🔹 新增
    public float playerX;
    public float playerY;
    public float playerZ;

    public int playerHp;

    // 🔹 可擴充場景內互動物件（例如床、箱子、門的狀態）
    public string bedState;
    public string toiletState;
    public bool chestOpened;
    public bool safeOpened;

    public List<string> collectedItems = new List<string>();
    public List<string> collectedClues = new List<string>();
    public List<string> finishedInteractions = new List<string>();
    public List<string> spawnedObjects = new List<string>();
    public List<string> spawnedNPCs = new List<string>();

}

