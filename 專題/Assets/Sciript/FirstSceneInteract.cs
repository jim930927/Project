using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System;

[RequireComponent(typeof(Collider2D))]
public class FirstSceneInteract : MonoBehaviour
{
    [Header("互動設定")]
    public string interactionNode; // 對應 Ink 節點名稱（例如 "TV"、"sofa"）
    public KeyCode interactKey = KeyCode.Space; // 互動按鍵（預設空白鍵）

    private firstDialogueManager dialogueManager;
    private bool isPlayerInside = false;
    private GameObject player;
    public Image Porpsimage;


    [Header("互動限制")]
    public bool canInteract = true; // 是否允許互動

    void Start()
    {
        dialogueManager = FindFirstObjectByType<firstDialogueManager>();
    }

    void Update()
    {
        if (!isPlayerInside || player == null || dialogueManager == null)
            return;

        // ✅ 不可互動就直接 return
        if (!canInteract) return;
        
        if (Input.GetKeyDown(interactKey))
        {
            if (!dialogueManager.dialogueIsPlaying && canInteract)
            {
                canInteract = false; // 🔹 暫時鎖定互動
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
        StartCoroutine(UnlockInteraction());
    }

    private IEnumerator UnlockInteraction()
    {
        yield return new WaitForSeconds(0.5f); // 避免立即重觸發
        canInteract = true;
    }

    /*
    // 🔹 可視化提示（在 Scene 模式下顯示互動點）
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.4f);
        if (!string.IsNullOrEmpty(interactionNode))
            UnityEditor.Handles.Label(transform.position + Vector3.up * 0.5f, interactionNode);
    }
    */
}
