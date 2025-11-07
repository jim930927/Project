using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using System.IO;
using System;

public class ChestController : MonoBehaviour
{
    public enum ChestState { Hidden, Found, Opened }

    [Header("箱子狀態管理")]
    public ChestState chestState = ChestState.Hidden;

    [Header("箱子圖片")]
    public SpriteRenderer chestRenderer;
    public Sprite closedChest;
    public Sprite openChest;

    [Header("道具生成")]
    public GameObject itemInside;
    public Transform itemSpawnPoint;

    [Header("密碼設定")]
    public string correctPassword = "1024";

    [Header("UI")]
    public GameObject passwordPanel;
    public TMP_InputField passwordInput;
    public Button confirmButton;
    public Button cancelButton;

    [Header("獎勵設定")]
    public List<string> rewardItemIDs = new List<string> { "key_room" };
    public List<string> rewardClueIDs = new List<string> { "book_mone", "medical_record", "award" };

    [Header("資料庫")]
    public ItemData itemDatabase;
    public ClueData clueDatabase;

    [Header("地圖圖示（可選）")]
    public GameObject mapIcon;

    private InkDialogueManager dialogueManager;
    private bool hasInteracted = false;

    void Start()
    {
        dialogueManager = FindFirstObjectByType<InkDialogueManager>();
        itemDatabase = FindFirstObjectByType<ItemData>();
        clueDatabase = FindFirstObjectByType<ClueData>();

        if (dialogueManager != null)
        {
            itemDatabase = dialogueManager.itemDatabase;
            clueDatabase = dialogueManager.clueDatabase;
        }

        if (passwordPanel != null)
            passwordPanel.SetActive(false);

        if (confirmButton != null)
            confirmButton.onClick.AddListener(OnConfirm);

        if (cancelButton != null)
            cancelButton.onClick.AddListener(ClosePasswordUI);

        Debug.Log($"[ChestController] Start() 起動 → 狀態={chestState}");

        TryApplyChestStateFromLatestSave();
        UpdateVisualByState();
    }

    // 🟩 Ink 呼叫：讓箱子出現
    public void RevealChest()
    {
        if (chestState == ChestState.Hidden)
        {
            chestState = ChestState.Found;
            UpdateVisualByState();
            Debug.Log("[ChestController] RevealChest() → 狀態切換為 Found，箱子出現在床旁");
        }
    }

    public void Interact()
    {
        if (hasInteracted)
            return;

        hasInteracted = true;

        if (chestState == ChestState.Opened)
        {
            if (dialogueManager != null)
                dialogueManager.EnterDialogueMode(dialogueManager.inkJSON, "chest_open");
            return;
        }

        if (chestState == ChestState.Found && passwordPanel != null)
        {
            passwordPanel.SetActive(true);
            passwordInput.text = "";
        }
    }

    void OnConfirm()
    {
        if (passwordInput.text == correctPassword)
            UnlockChest();
        else
        {
            Debug.Log("❌ 密碼錯誤");
            hasInteracted = false;
        }
    }

    void UnlockChest()
    {
        chestState = ChestState.Opened;
        Debug.Log("✅ 密碼正確，箱子打開");

        if (chestRenderer != null && openChest != null)
            chestRenderer.sprite = openChest;

        if (passwordPanel != null)
            passwordPanel.SetActive(false);

        if (itemInside != null && itemSpawnPoint != null)
            Instantiate(itemInside, itemSpawnPoint.position, Quaternion.identity);

        if (itemDatabase != null)
            foreach (string id in rewardItemIDs)
                itemDatabase.AddItem(id);

        if (clueDatabase != null)
            foreach (string id in rewardClueIDs)
                clueDatabase.AddClue(id);

        if (dialogueManager != null)
        {
            dialogueManager.ForceEndDialogue();
            Invoke(nameof(StartChestOpenDialogue), 0.3f);
        }

        var interactable = GetComponent<SceneInteractable>();
        if (interactable != null)
            interactable.canInteract = false;

        UpdateVisualByState();
    }

    void StartChestOpenDialogue()
    {
        if (dialogueManager != null)
            dialogueManager.EnterDialogueMode(dialogueManager.inkJSON, "chest_open");
    }

    public void ClosePasswordUI()
    {
        if (passwordPanel != null)
            passwordPanel.SetActive(false);
        hasInteracted = false;
    }

    // 🎨 根據當前狀態更新外觀
    private void UpdateVisualByState()
    {
        switch (chestState)
        {
            case ChestState.Hidden:
                gameObject.SetActive(false);
                if (mapIcon != null) mapIcon.SetActive(false);
                break;

            case ChestState.Found:
                gameObject.SetActive(true);
                if (mapIcon != null) mapIcon.SetActive(true);
                if (chestRenderer != null && closedChest != null)
                    chestRenderer.sprite = closedChest;
                break;

            case ChestState.Opened:
                gameObject.SetActive(true);
                if (mapIcon != null) mapIcon.SetActive(true);
                if (chestRenderer != null && openChest != null)
                    chestRenderer.sprite = openChest;
                break;
        }
    }

    // ✅ 提供給 LoadUIManager 呼叫（公開）
    public void UpdateMapIcon()
    {
        if (mapIcon != null)
        {
            mapIcon.SetActive(chestState != ChestState.Hidden);
            Debug.Log($"[ChestController] UpdateMapIcon() → mapIcon.Active={mapIcon.activeSelf}");
        }
        else
        {
            Debug.LogWarning("[ChestController] 沒有 mapIcon 可更新");
        }
    }

    // 📦 存檔載入後直接套用狀態
    public void ApplyLoadedState(string stateString)
    {
        if (Enum.TryParse(stateString, out ChestState newState))
        {
            chestState = newState;
            Debug.Log($"[ChestController] ApplyLoadedState() → 套用狀態={chestState}");
            UpdateVisualByState();
        }
    }

    // 🧭 自動從最新存檔讀取狀態
    private void TryApplyChestStateFromLatestSave()
    {
        try
        {
            string root = Application.persistentDataPath;
            if (!Directory.Exists(root)) return;

            string[] files = Directory.GetFiles(root, "save_*.json");
            if (files == null || files.Length == 0) return;

            string latestFile = null;
            DateTime latestTime = DateTime.MinValue;

            foreach (var f in files)
            {
                string json = File.ReadAllText(f);
                SaveDataProbe probe = JsonUtility.FromJson<SaveDataProbe>(json);
                if (probe == null || string.IsNullOrEmpty(probe.saveTime)) continue;

                if (DateTime.TryParse(probe.saveTime, out DateTime t))
                {
                    if (t > latestTime)
                    {
                        latestTime = t;
                        latestFile = f;
                    }
                }
            }

            if (string.IsNullOrEmpty(latestFile)) return;

            string latestJson = File.ReadAllText(latestFile);
            SaveDataFull data = JsonUtility.FromJson<SaveDataFull>(latestJson);

            if (!string.IsNullOrEmpty(data.chestState))
            {
                ApplyLoadedState(data.chestState);
                Debug.Log($"[ChestController] 已從 {Path.GetFileName(latestFile)} 套用 chestState={data.chestState}");
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[ChestController] 套用 chestState 失敗：{e.Message}");
        }
    }

    [Serializable]
    private class SaveDataProbe
    {
        public string saveTime;
    }

    [Serializable]
    private class SaveDataFull
    {
        public string saveTime;
        public string chestState;
        public string storyState;
        public string sceneName;
        public float playerX;
        public float playerY;
        public float playerZ;
        public bool chestOpened;
        public bool safeOpened;
        public List<string> collectedItems;
        public List<string> collectedClues;
        public List<string> finishedInteractions;
        public List<string> spawnedObjects;
        public List<string> spawnedNPCs;
    }
}
