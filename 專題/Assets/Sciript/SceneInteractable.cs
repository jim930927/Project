using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class SceneInteractable : MonoBehaviour
{
    [Header("互動設定")]
    public string interactionNode; // 對應 Ink 節點名稱（例如 "TV"、"sofa"）
    public KeyCode interactKey = KeyCode.Space; // 互動按鍵（預設空白鍵）

    private InkDialogueManager dialogueManager;
    private bool isPlayerInside = false;
    private GameObject player;

    void Start()
    {
        dialogueManager = FindFirstObjectByType<InkDialogueManager>();
    }

    void Update()
    {
        if (!isPlayerInside || player == null || dialogueManager == null)
            return;

        // ✅ 按下互動鍵時觸發對話
        if (Input.GetKeyDown(interactKey))
        {
            // 如果對話正在播放就不重複開
            if (!dialogueManager.dialogueIsPlaying)
            {
                dialogueManager.EnterDialogueMode(dialogueManager.inkJSON, interactionNode, OnDialogueEnd);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInside = true;
            player = other.gameObject;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInside = false;
            player = null;
        }
    }

    // ✅ 對話結束後呼叫
    private void OnDialogueEnd()
    {
        Debug.Log($"🗨️ 結束互動：{interactionNode}");
        // 可在這裡加特效或改變物件狀態（例如亮燈、顯示提示）
    }

    // 🔹 可視化提示（在 Scene 模式下顯示互動點）
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.4f);
        if (!string.IsNullOrEmpty(interactionNode))
            UnityEditor.Handles.Label(transform.position + Vector3.up * 0.5f, interactionNode);
    }
}
