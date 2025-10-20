using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class ChestController : MonoBehaviour
{
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


    private bool isUnlocked = false;
    private bool hasInteracted = false;

    private InkDialogueManager dialogueManager;

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
        Debug.Log("✅ 密碼正確，箱子打開");

        if (chestRenderer != null && openChest != null)
            chestRenderer.sprite = openChest;

        if (passwordPanel != null)
            passwordPanel.SetActive(false);

        if (itemInside != null && itemSpawnPoint != null)
            Instantiate(itemInside, itemSpawnPoint.position, Quaternion.identity);

        // ✅ 自動新增多個道具
        if (itemDatabase != null)
        {
            foreach (string id in rewardItemIDs)
            {
                itemDatabase.AddItem(id);
            }
        }

        // ✅ 自動新增多個線索
        if (clueDatabase != null)
        {
            foreach (string id in rewardClueIDs)
            {
                clueDatabase.AddClue(id);
            }
        }

        // ✅ 關閉對話，進入 chest_open
        if (dialogueManager != null)
        {
            dialogueManager.ForceEndDialogue();
            Invoke(nameof(StartChestOpenDialogue), 0.3f);
        }

        // ✅ 停止重複互動
        var interactable = GetComponent<SceneInteractable>();
        if (interactable != null)
        {
            interactable.canInteract = false;
        }
    }

    void StartChestOpenDialogue()
    {
        if (dialogueManager != null)
        {
            dialogueManager.EnterDialogueMode(dialogueManager.inkJSON, "chest_open");
        }
    }

    public void ClosePasswordUI()
    {
        if (passwordPanel != null)
            passwordPanel.SetActive(false);
        hasInteracted = false;
    }
}
