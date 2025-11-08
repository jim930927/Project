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

        // === 還原床底箱子狀態 ===
        var bed = GameObject.FindObjectOfType<BedController>();
        switch (data.chestState)
        {
            case 0:
                Debug.Log("📦 狀態 0：未生成箱子");
                break;

            case 1:
                Debug.Log("📦 狀態 1：生成但未解鎖");
                if (!FindObjectOfType<ChestController>() && bed != null)
                {
                    bed.SpawnObject("chest");
                }
                break;

            case 2:
                Debug.Log("📦 狀態 2：已解鎖");
                if (!FindObjectOfType<ChestController>() && bed != null)
                {
                    bed.SpawnObject("chest");
                }

                var chestObj = FindObjectOfType<ChestController>();
                if (chestObj != null)
                {
                    chestObj.isUnlocked = true;
                    chestObj.ApplyVisualByState();
                }
                break;
        }

        // === 恢復馬桶 ===
        var toilet = GameObject.FindObjectOfType<toiletController>();
        if (toilet != null && !string.IsNullOrEmpty(data.toiletState))
            toilet.ChangeImage(data.toiletState);

        // === 恢復保險箱 ===
        var safe = GameObject.FindObjectOfType<SafeController>();
        if (safe != null) safe.isUnlocked = data.safeOpened;

        // === EventSystem ===
        UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(null);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Debug.Log("✅ 存檔資料已完整還原");

        var evt = UnityEngine.EventSystems.EventSystem.current;
        if (evt == null)
        {
            GameObject newEvt = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.EventSystems.StandaloneInputModule));
            Debug.Log("⚙️ 自動建立新的 EventSystem");
        }
        else
        {
            evt.enabled = true;
        }

        // === 重設 Canvas ===
        int baseOrder = 0;
        foreach (var canvas in GameObject.FindObjectsOfType<Canvas>())
        {
            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay || canvas.renderMode == RenderMode.ScreenSpaceCamera)
            {
                canvas.overrideSorting = true;
                canvas.sortingOrder = baseOrder++;
            }

            var ray = canvas.GetComponent<UnityEngine.UI.GraphicRaycaster>();
            if (ray == null)
                canvas.gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        }

        // === 隱藏已收集道具與線索 ===
        foreach (var item in FindObjectsOfType<ItemPickup>())
        {
            var id = item.GetComponent<SaveableEntity>();
            if (id != null && data.collectedItems.Contains(id.uniqueID))
                item.gameObject.SetActive(false);
        }

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

        // === NPC ===
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

        Debug.Log($"📜 載入結果：道具 {data.collectedItems.Count}、線索 {data.collectedClues.Count}、互動 {data.finishedInteractions.Count}、NPC {data.spawnedNPCs.Count}");
    }

}
