using Ink.Runtime;
using Unity.VisualScripting;
using TMPro;
using UnityEngine.UI;
using UnityEngine;

public class PickupBook : MonoBehaviour
{
    public GameObject bookIconUI; // 書本圖示 UI
    public InkDialogueManager dialogueManager; // 對話管理器
    public TextAsset inkJSONAsset; // 要播放的 Ink 劇本

    private bool isPlayerNear = false;
    private bool hasPickedUp = false;

    private GameObject player;

    public TextMeshProUGUI interactHint;

    void Start()
    {
        if (bookIconUI != null)
            bookIconUI.SetActive(false); // 預設隱藏書本圖示

        if (interactHint != null)
            interactHint.canvasRenderer.SetAlpha(0); // 初始隱藏
    }

    void Update()
    {
        if (isPlayerNear && Input.GetKeyDown(KeyCode.Space) && !hasPickedUp)
        {
            hasPickedUp = true;

            if (bookIconUI != null)
                bookIconUI.SetActive(true); // 顯示書圖示 UI


            if (dialogueManager != null && inkJSONAsset != null)
            {
                Debug.Log("✅ 撿到書，播放 book_found 節點對話");
                dialogueManager.EnterDialogueMode(inkJSONAsset, "book_found"); // 🔥 指定節點
            }
            else
            {
                Debug.LogWarning("⚠️ dialogueManager 或 inkJSONAsset 尚未設定！");
            }

            gameObject.SetActive(false); // 隱藏場景中的書本
        }

        if (isPlayerNear && player != null)
        {
            // 顯示提示
            if (interactHint != null)
                interactHint.canvasRenderer.SetAlpha(1);
        }
        else
        {
            // 離開範圍，隱藏提示
            if (interactHint != null)
                interactHint.canvasRenderer.SetAlpha(0);
        }


    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            isPlayerNear = true;
            player = other.gameObject;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            isPlayerNear = false;
            player = null;

        if (interactHint != null)
            interactHint.canvasRenderer.SetAlpha(0); // 離開立即隱藏
    }
}
