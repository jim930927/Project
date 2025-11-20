using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Collider2D))]
public class SceneInteractable : MonoBehaviour
{
    [System.NonSerialized] public bool loadedFromSave = false;

    [Header("互動設定")]
    public string interactionNode;
    public KeyCode interactKey = KeyCode.Space;

    private InkDialogueManager dialogueManager;
    private bool isPlayerInside = false;
    private GameObject player;
    public Image Porpsimage;

    [Header("互動限制")]
    public bool canInteract = true; // 是否允許互動

    private SaveableEntity saveID;

    void Start()
    {
        dialogueManager = FindFirstObjectByType<InkDialogueManager>();
        saveID = GetComponent<SaveableEntity>();

        Debug.Log(
            $"[SceneInteractable][{name}] Start()  " +
            $"interactionNode='{interactionNode}', canInteract={canInteract}, loadedFromSave={loadedFromSave}, " +
            $"dialogueManager={(dialogueManager ? dialogueManager.name : "NULL")}"
        );

        if (loadedFromSave)
        {
            Debug.Log($"[SceneInteractable][{name}] 🔒 由存檔載入，照理應該已還原互動狀態。");
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(interactKey))
        {
            bool dialogueIsPlaying = dialogueManager != null && dialogueManager.dialogueIsPlaying;

            Debug.Log(
                $"[SceneInteractable][{name}] 按下互動鍵 {interactKey}：" +
                $" isPlayerInside={isPlayerInside}," +
                $" player={(player ? player.name : "NULL")}," +
                $" dialogueManager={(dialogueManager ? dialogueManager.name : "NULL")}," +
                $" canInteract={canInteract}, dialogueIsPlaying={dialogueIsPlaying}," +
                $" interactionNode='{interactionNode}'"
            );
        }

        if (!isPlayerInside || player == null || dialogueManager == null)
            return;

        if (!canInteract)
            return;

        if (Input.GetKeyDown(interactKey))
        {
            if (!dialogueManager.dialogueIsPlaying && canInteract)
            {
                Debug.Log($"[SceneInteractable][{name}] ✅ 滿足條件，開始進入對話節點：{interactionNode}");
                canInteract = false;
                dialogueManager.EnterDialogueMode(dialogueManager.inkJSON, interactionNode, OnDialogueEnd);
            }
            else
            {
                Debug.Log(
                    $"[SceneInteractable][{name}] ❌ 按鍵觸發但未進入對話：" +
                    $" dialogueIsPlaying={dialogueManager.dialogueIsPlaying}, canInteract={canInteract}"
                );
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"[SceneInteractable][{name}] OnTriggerEnter2D：其他物件 = {other.name}, tag = {other.tag}");

        if (other.CompareTag("Player"))
        {
            isPlayerInside = true;
            player = other.gameObject;
            Debug.Log($"[SceneInteractable][{name}] ✅ Player 進入互動範圍");
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        Debug.Log($"[SceneInteractable][{name}] OnTriggerExit2D：其他物件 = {other.name}, tag = {other.tag}");

        if (other.CompareTag("Player"))
        {
            isPlayerInside = false;
            player = null;
            Debug.Log($"[SceneInteractable][{name}] ⭕ Player 離開互動範圍");
        }
    }

    // -----------------------------
    // ⭐ 對話結束
    // -----------------------------
    private void OnDialogueEnd()
    {
        Debug.Log($"[SceneInteractable][{name}] 🗨️ 結束互動：{interactionNode}");

        // ⭐ 立刻寫入已互動清單（避免漏存 B 物件）
        if (saveID != null)
        {
            SaveUIManager.AddFinishedInteraction(saveID.uniqueID);
            Debug.Log($"[SceneInteractable][{name}] 💾 已記錄互動 uniqueID={saveID.uniqueID}");
        }

        // ⭐ 不可再把 canInteract 恢復 true（否則 Save 會誤以為沒互動）
        // StartCoroutine(UnlockInteraction()); ← ❌ 不能啟動這個

        Debug.Log($"[SceneInteractable][{name}] 🔒 此物件已永久關閉互動 (canInteract=false)");
    }

    // ❌ 保留原本程式碼，但不會再被呼叫
    private System.Collections.IEnumerator UnlockInteraction()
    {
        yield return new WaitForSeconds(0.5f);
        canInteract = true;
        Debug.Log($"[SceneInteractable][{name}] 🔓 互動解鎖（目前已停用，不會再被使用）");
    }
}
