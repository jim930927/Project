using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class ChestController : MonoBehaviour
{
    public enum ChestState
    {
        Closed,
        Open
    }

    // 🟢 靜態欄位：由 LoadUIManager 在載入時設定，任何新生成的箱子都會自動吃到這個值
    public static string pendingOverrideState = null;

    [Header("箱子狀態圖片")]
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

    public bool isUnlocked = false;
    public bool hasInteracted = false;
    public ChestState currentState = ChestState.Closed;

    private InkDialogueManager dialogueManager;

    // ✅ 改在 Awake 初始化 + 自動吃載入狀態
    void Awake()
    {
        ApplyState();

        // 🟢 如果是從讀檔載入的，這裡會自動套用狀態
        if (!string.IsNullOrEmpty(pendingOverrideState))
        {
            ChangeState(pendingOverrideState);
            Debug.Log($"[Chest] Apply pending override -> {pendingOverrideState}");
            pendingOverrideState = null; // 用完即清空，避免新箱子被誤套
        }
    }

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
    }

    // ======================
    // 狀態控制部分
    // ======================
    public string GetCurrentState()
    {
        return currentState.ToString();
    }

    public void ChangeState(string newState)
    {
        if (System.Enum.TryParse(newState, true, out ChestState state))
        {
            currentState = state;
            ApplyState();
            Debug.Log($"[Chest] ChangeState -> {currentState}");
        }
        else
        {
            Debug.LogWarning($"[Chest] ❌ 無法解析狀態字串：{newState}");
        }
    }

    private void ApplyState()
    {
        switch (currentState)
        {
            case ChestState.Closed:
                if (chestRenderer != null && closedChest != null)
                    chestRenderer.sprite = closedChest;
                isUnlocked = false;
                break;

            case ChestState.Open:
                if (chestRenderer != null && openChest != null)
                    chestRenderer.sprite = openChest;
                isUnlocked = true;
                break;
        }

        Debug.Log($"[Chest] ApplyState -> {currentState}");
    }

    // ======================
    // 互動邏輯
    // ======================
    public void Interact()
    {
        if (hasInteracted)
            return;

        hasInteracted = true;

        if (isUnlocked)
        {
            if (dialogueManager != null)
                dialogueManager.EnterDialogueMode(dialogueManager.inkJSON, "chest_open");
            return;
        }

        if (passwordPanel != null)
        {
            passwordPanel.SetActive(true);
            passwordInput.text = "";
        }
    }

    void OnConfirm()
    {
        if (passwordInput.text == correctPassword)
        {
            UnlockChest();
        }
        else
        {
            Debug.Log("❌ 密碼錯誤");
            hasInteracted = false;
        }
    }

    void UnlockChest()
    {
        isUnlocked = true;
        currentState = ChestState.Open;
        Debug.Log("✅ 密碼正確，箱子打開");

        ApplyState();

        if (passwordPanel != null)
            passwordPanel.SetActive(false);

        if (itemInside != null && itemSpawnPoint != null)
            Instantiate(itemInside, itemSpawnPoint.position, Quaternion.identity);

        // ✅ 獎勵加入資料庫
        if (itemDatabase != null)
        {
            foreach (string id in rewardItemIDs)
                itemDatabase.AddItem(id);
        }

        if (clueDatabase != null)
        {
            foreach (string id in rewardClueIDs)
                clueDatabase.AddClue(id);
        }

        if (dialogueManager != null)
        {
            dialogueManager.ForceEndDialogue();
            Invoke(nameof(StartChestOpenDialogue), 0.3f);
        }

        var interactable = GetComponent<SceneInteractable>();
        if (interactable != null)
            interactable.canInteract = false;
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
}
