using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ChestController : MonoBehaviour
{
    [Header("🔒 狀態控制")]
    public bool isUnlocked = false; // 是否解鎖
    public bool isInteracting = false; // 是否正在輸入密碼

    [Header("🔢 密碼設定")]
    public string correctPassword = "1234";
    public TMP_InputField inputField;
    public GameObject passwordPanel;

    [Header("🖼️ 外觀切換")]
    public SpriteRenderer chestRenderer;
    public Sprite closedChest;
    public Sprite openChest;

    [Header("🎵 音效")]
    public AudioSource audioSource;
    public AudioClip openSound;
    public AudioClip errorSound;

    [Header("🔗 其他")]
    public GameObject itemInside; // 箱子內物品

    void Start()
    {
        // 初始狀態顯示
        ApplyVisualByState();
    }

    // 📦 狀態套用邏輯（由 Start() 或 讀檔呼叫）
    public void ApplyVisualByState()
    {
        if (isUnlocked)
        {
            // 顯示開啟圖片
            if (chestRenderer != null && openChest != null)
                chestRenderer.sprite = openChest;

            // 關閉密碼輸入 UI
            if (passwordPanel != null)
                passwordPanel.SetActive(false);

            // 禁止再互動
            var interactable = GetComponent<SceneInteractable>();
            if (interactable != null)
                interactable.canInteract = false;

            // 顯示內部物品
            if (itemInside != null)
                itemInside.SetActive(true);
        }
        else
        {
            // 顯示關閉圖片
            if (chestRenderer != null && closedChest != null)
                chestRenderer.sprite = closedChest;

            // 關閉內部物品（未解鎖前不可見）
            if (itemInside != null)
                itemInside.SetActive(false);

            // 可互動
            var interactable = GetComponent<SceneInteractable>();
            if (interactable != null)
                interactable.canInteract = true;
        }
    }

    // 🔑 當玩家互動時（例如按下 E）
    public void Interact()
    {
        if (isUnlocked)
        {
            Debug.Log("📦 箱子已解鎖，無需再輸入密碼");
            return;
        }

        if (isInteracting) return;
        isInteracting = true;

        if (passwordPanel != null)
        {
            passwordPanel.SetActive(true);
            inputField.text = "";
            inputField.Select();
        }
    }

    // 🧩 檢查密碼
    public void CheckPassword()
    {
        if (inputField.text == correctPassword)
        {
            StartCoroutine(OpenChest());
        }
        else
        {
            audioSource?.PlayOneShot(errorSound);
            inputField.text = "";
            Debug.Log("❌ 密碼錯誤");
        }
    }

    // 🧭 關閉輸入介面
    public void CancelInput()
    {
        if (passwordPanel != null)
            passwordPanel.SetActive(false);

        isInteracting = false;
    }

    // 📬 開啟箱子動畫流程
    IEnumerator OpenChest()
    {
        audioSource?.PlayOneShot(openSound);
        Debug.Log("✅ 密碼正確，開啟箱子");

        yield return new WaitForSeconds(0.3f);

        isUnlocked = true;
        ApplyVisualByState();

        // 通知 SceneInteractable 禁止再觸發
        var inter = GetComponent<SceneInteractable>();
        if (inter != null)
            inter.canInteract = false;

        if (passwordPanel != null)
            passwordPanel.SetActive(false);

        // 若有內部物品則顯示
        if (itemInside != null)
            itemInside.SetActive(true);

        isInteracting = false;
    }
}
