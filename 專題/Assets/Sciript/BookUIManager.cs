using UnityEngine;
using UnityEngine.UI;

public class BookUIManager : MonoBehaviour
{
    [Header("基本UI")]
    public GameObject bookPanel;         // 書本主面板
    public Button bookIconButton;        // 書右下角按鈕
    public Button closeButton;           // 書內部 X 關閉按鈕

    [Header("三個線索按鈕")]
    public Button letterButton;          // 信件按鈕（固定放在BookPanel上）
    public Button journalButton;         // 日記按鈕
    public Button talkButton;            // 主線對話按鈕

    [Header("Overlay Panels")]
    public GameObject letterOverlayPanel;    // 信件閱讀面板（原本用的）
    public GameObject journalOverlayPanel;   // 日記面板（留著不動）

    [Header("外部閱讀系統")]
    public JournalReader journalReader;      // 書本內開啟用的 JournalReader

    [Header("來源腳本（書中閱讀時暫停）")]
    public Journal journalScript;            // 場景中撿取日記用的 Journal.cs

    [Header("線索觸發狀態")]
    public bool pickupLetter = false;
    public bool pickupJournal = false;
    public bool talkedToNPC = false;

    void Start()
    {
        // 書本預設關閉
        if (bookPanel != null)
            bookPanel.SetActive(false);

        // 書的開關
        if (bookIconButton != null)
            bookIconButton.onClick.AddListener(OpenBook);
        if (closeButton != null)
            closeButton.onClick.AddListener(CloseBook);

        // 信件按鈕
        if (letterButton != null)
        {
            letterButton.gameObject.SetActive(false);
            letterButton.onClick.AddListener(OpenLetterOverlay);
        }

        // 日記按鈕（呼叫 JournalReader）
        if (journalButton != null)
        {
            journalButton.gameObject.SetActive(false);
            journalButton.onClick.AddListener(OpenJournalOverlay);
        }

        // 主線按鈕
        if (talkButton != null)
        {
            talkButton.gameObject.SetActive(false);
            talkButton.onClick.AddListener(OnTalkButtonClicked);
        }

        // Overlay 面板預設隱藏
        if (letterOverlayPanel) letterOverlayPanel.SetActive(false);
        if (journalOverlayPanel) journalOverlayPanel.SetActive(false);
    }

    void Update()
    {
        // 當玩家撿到線索 → 顯示對應按鈕
        if (pickupLetter && letterButton != null && !letterButton.gameObject.activeSelf)
            letterButton.gameObject.SetActive(true);

        if (pickupJournal && journalButton != null && !journalButton.gameObject.activeSelf)
            journalButton.gameObject.SetActive(true);

        if (talkedToNPC && talkButton != null && !talkButton.gameObject.activeSelf)
            talkButton.gameObject.SetActive(true);
    }

    // === 書開關 ===
    void OpenBook() => bookPanel.SetActive(true);
    void CloseBook() => bookPanel.SetActive(false);

    // === 信件 Overlay ===
    void OpenLetterOverlay()
    {
        if (letterOverlayPanel != null)
            letterOverlayPanel.SetActive(true);
    }

    public void CloseLetterOverlay()
    {
        if (letterOverlayPanel != null)
            letterOverlayPanel.SetActive(false);
    }

    // === 日記 Overlay（書中閱讀）===
    void OpenJournalOverlay()
    {
        // 1️⃣ 暫時停用原 Journal 腳本，避免返回時觸發 Ink
        if (journalScript != null)
        {
            journalScript.enabled = false;
            Debug.Log("⏸ 暫時停用 Journal.cs（避免重啟 Ink）");
        }

        // 2️⃣ 使用 JournalReader 顯示純閱讀模式
        if (journalReader != null)
        {
            journalReader.OpenReader();
            Debug.Log("📖 開啟書中日記閱讀模式");
        }
        else if (journalOverlayPanel != null)
        {
            // 備用方案：如果沒設定 Reader，就開舊面板
            journalOverlayPanel.SetActive(true);
            Debug.LogWarning("⚠️ 尚未設定 JournalReader，使用舊版開啟面板。");
        }
        else
        {
            Debug.LogWarning("⚠️ 沒有設定任何日記面板！");
        }
    }

    public void CloseJournalOverlay()
    {
        if (journalOverlayPanel != null)
            journalOverlayPanel.SetActive(false);
    }

    // === 主線按鈕 ===
    void OnTalkButtonClicked()
    {
        Debug.Log("💬 主線對話按鈕被點擊（可綁定 NPC 對話事件）");
    }
}
