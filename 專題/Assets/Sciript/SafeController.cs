using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class SafeController : MonoBehaviour
{
    [Header("唯一識別ID（存檔用）")]
    public string uniqueID;

    [Header("密碼設定")]
    public string correctPassword = "0818";

    [Header("UI")]
    public GameObject passwordPanel;
    public TMP_InputField passwordInput;
    public Button confirmButton;
    public Button cancelButton;

    [Header("獎勵設定")]
    public List<string> rewardItemIDs = new List<string> { "key_parent" };
    //public List<string> rewardClueIDs = new List<string> { "Letter2" };

    [Header("資料庫")]
    public ItemData itemDatabase;
    public ClueData clueDatabase;


    public bool isUnlocked = false;
    public bool hasInteracted = false;

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
                dialogueManager.EnterDialogueMode(dialogueManager.inkJSON, "safe_open");
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
            UnlockSafe();
        }
        else
        {
            Debug.Log("❌ 密碼錯誤");
            hasInteracted = false;
        }
    }

    void UnlockSafe()
    {
        isUnlocked = true;
        Debug.Log("✅ 密碼正確，箱子打開");

        if (passwordPanel != null)
            passwordPanel.SetActive(false);

        // ✅ 自動新增多個道具
        if (itemDatabase != null)
        {
            foreach (string id in rewardItemIDs)
            {
                itemDatabase.AddItem(id);
            }
        }

        // ✅ 自動新增多個線索
        /*
        if (clueDatabase != null)
        {
            foreach (string id in rewardClueIDs)
            {
                clueDatabase.AddClue(id);
            }
        }
        */

        // ✅ 關閉對話，進入 safe_open
        if (dialogueManager != null)
        {
            dialogueManager.ForceEndDialogue();
            Invoke(nameof(StartSafeOpenDialogue), 0.3f);
        }

        // ✅ 停止重複互動
        var interactable = GetComponent<SceneInteractable>();
        if (interactable != null)
        {
            interactable.canInteract = false;
        }
    }

    void StartSafeOpenDialogue()
    {
        if (dialogueManager != null)
        {
            dialogueManager.EnterDialogueMode(dialogueManager.inkJSON, "safe_open");
        }
    }

    public void ClosePasswordUI()
    {
        if (passwordPanel != null)
            passwordPanel.SetActive(false);
        hasInteracted = false;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrEmpty(uniqueID))
            uniqueID = System.Guid.NewGuid().ToString();
    }
#endif
}
