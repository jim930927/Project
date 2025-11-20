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

    public static SaveData pendingLoadData;

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

        pendingLoadData = data;

        InkDialogueManager.shouldAutoStartInk = false;

        var evt = UnityEngine.EventSystems.EventSystem.current;
        if (evt != null)
            GameObject.Destroy(evt.gameObject);

        SceneManager.LoadScene(data.sceneName);
    }

    // ========== 新場景讀檔流程 ==========

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

        inkManager.SetExternalStateFromSave(data.finishedInteractions);


        // === 玩家位置 ===
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            player.transform.position = new Vector3(data.playerX, data.playerY, data.playerZ);

            var pc = player.GetComponent<Collider2D>();
            if (pc != null)
            {
                pc.enabled = false;
                pc.enabled = true;
            }

            Physics2D.SyncTransforms();

            var pm = player.GetComponent<Player>();
            if (pm != null) pm.canMove = true;
        }

        // === HP ===
        HP hpRef = GameObject.FindObjectOfType<HP>();
        if (hpRef != null)
            hpRef.hp = data.playerHp;

        // === 場景物件 ===
        var bed = GameObject.FindObjectOfType<BedController>();
        if (bed != null && !string.IsNullOrEmpty(data.bedState))
            bed.ChangeImage(data.bedState);

        var toilet = GameObject.FindObjectOfType<toiletController>();
        if (toilet != null && !string.IsNullOrEmpty(data.toiletState))
            toilet.ChangeImage(data.toiletState);

        // === 箱子 ===
        if (!string.IsNullOrEmpty(data.chestState))
            ChestController.pendingOverrideState = data.chestState;
        else
            ChestController.pendingOverrideState = data.chestOpened ? "Open" : "Closed";

        var safe = GameObject.FindObjectOfType<SafeController>();
        if (safe != null)
            safe.isUnlocked = data.safeOpened;

        // === ScriptableObject ===
        ClueData clueDB = null;
        ItemData itemDB = null;

        var clueSample = GameObject.FindObjectOfType<CluePickup>();
        if (clueSample != null) clueDB = clueSample.clueData;

        var itemSample = GameObject.FindObjectOfType<ItemPickup>();
        if (itemSample != null) itemDB = itemSample.itemData;

        if (clueDB != null)
        {
            foreach (var c in clueDB.clues)
                c.collected = data.databaseCollectedClueIds.Contains(c.id);
        }

        if (itemDB != null)
        {
            foreach (var i in itemDB.items)
                i.collected = data.databaseCollectedItemIds.Contains(i.id);
        }

        // === UI ===
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

        // === 道具 ===
        foreach (var item in GameObject.FindObjectsOfType<ItemPickup>())
        {
            var id = item.GetComponent<SaveableEntity>();
            if (id != null && data.collectedItems.Contains(id.uniqueID))
            {
                item.collected = true;
                item.gameObject.SetActive(false);
            }
        }

        // === 線索 ===
        foreach (var clue in GameObject.FindObjectsOfType<CluePickup>())
        {
            var id = clue.GetComponent<SaveableEntity>();
            if (id != null && data.collectedClues.Contains(id.uniqueID))
            {
                clue.collected = true;
                clue.gameObject.SetActive(false);
            }
        }

        // === 已互動之物件 ===
        foreach (var inter in GameObject.FindObjectsOfType<SceneInteractable>())
        {
            var id = inter.GetComponent<SaveableEntity>();
            if (id != null && data.finishedInteractions.Contains(id.uniqueID))
            {
                inter.canInteract = false;

                // ⭐⭐ 最小更動：補上 loadedFromSave = true ⭐⭐
                inter.loadedFromSave = true;
            }
        }

        // === 生成物件 ===
        foreach (string id in data.spawnedObjects)
        {
            bool exists = false;
            foreach (var so in GameObject.FindObjectsOfType<SaveableEntity>())
                if (so.uniqueID == id) exists = true;

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
                if (so.uniqueID == id) exists = true;

            if (!exists)
            {
                var npcManager = GameObject.FindObjectOfType<NPCManager>();
                if (npcManager != null) npcManager.SpawnNPC("Guard");
            }
        }

        // === 敵人 ===
        if (!string.IsNullOrEmpty(data.enemyStatesJson))
        {
            if (EnemyStateManager.Instance != null)
                EnemyStateManager.Instance.LoadFromJson(data.enemyStatesJson);
        }

        // === 永久解鎖門 ===
        if (DoorManager.Instance != null && data.unlockedDoors != null)
        {
            foreach (string doorID in data.unlockedDoors)
                DoorManager.Instance.UnlockDoor(doorID);
        }

        // === Trigger 修復 ===
        Physics2D.SyncTransforms();
        ForceRefreshInteractables();

        Debug.Log("✅ 載入資料全部還原完成（包含互動、道具、NPC、敵人、門）");
    }

    // === 強制刷新所有互動 Trigger ===
    static void ForceRefreshInteractables()
    {
        var player = GameObject.FindWithTag("Player");
        if (player == null) return;

        var playerCol = player.GetComponent<Collider2D>();
        if (playerCol == null) return;

        foreach (var inter in GameObject.FindObjectsOfType<SceneInteractable>())
        {
            var col = inter.GetComponent<Collider2D>();

            if (col != null && col.IsTouching(playerCol))
            {
                Debug.Log($"[Load Fix] 🔄 強制觸發互動器 OnTriggerEnter2D：{inter.name}");
                inter.SendMessage("OnTriggerEnter2D", playerCol, SendMessageOptions.DontRequireReceiver);
            }
        }
    }
}
