using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class JournalReader : MonoBehaviour
{
    [Header("UI 元件")]
    public GameObject overlayPanel;
    public Text overlayText;
    public Button returnButton;
    public Button nextPageButton;
    public Button prevPageButton;

    [Header("來源（可選）")]
    [Tooltip("如果本 Reader 的 pages 為空，會自動從這個 Journal 複製 pages")]
    public Journal journalSource;
    [Tooltip("當 pages 為空時，自動從 journalSource 複製內容")]
    public bool copyPagesFromJournalIfEmpty = true;

    [Header("日記內容")]
    [TextArea(3, 10)]
    public List<string> pages = new List<string>();

    private int currentPage = 0;

    void Start()
    {
        // 若本身沒內容且允許自動帶入，從 Journal 複製 pages
        if (pages.Count == 0 && copyPagesFromJournalIfEmpty && journalSource != null)
        {
            pages = new List<string>(journalSource.pages);
        }

        if (overlayPanel != null)
            overlayPanel.SetActive(false);

        if (returnButton != null)
            returnButton.onClick.AddListener(CloseReader);

        if (nextPageButton != null)
            nextPageButton.onClick.AddListener(NextPage);

        if (prevPageButton != null)
            prevPageButton.onClick.AddListener(PrevPage);
    }

    public void OpenReader()
    {
        if (overlayPanel == null || pages.Count == 0)
        {
            Debug.LogWarning("⚠️ 無法開啟日記：overlayPanel 未設定或沒有內容");
            return;
        }

        currentPage = 0;
        overlayPanel.SetActive(true);
        ShowPage();
    }

    void ShowPage()
    {
        if (overlayText != null)
            overlayText.text = pages[currentPage];

        if (prevPageButton != null)
            prevPageButton.gameObject.SetActive(currentPage > 0);

        if (nextPageButton != null)
            nextPageButton.gameObject.SetActive(currentPage < pages.Count - 1);

        if (returnButton != null)
            returnButton.gameObject.SetActive(currentPage == pages.Count - 1);
    }

    void NextPage()
    {
        if (currentPage < pages.Count - 1)
        {
            currentPage++;
            ShowPage();
        }
    }

    void PrevPage()
    {
        if (currentPage > 0)
        {
            currentPage--;
            ShowPage();
        }
    }

    void CloseReader()
    {
        if (overlayPanel != null)
            overlayPanel.SetActive(false);

        // 關閉書中閱讀後，恢復場景的 Journal 腳本
        var bookUI = FindFirstObjectByType<BookUIManager>();
        if (bookUI != null && bookUI.journalScript != null)
        {
            bookUI.journalScript.enabled = true;
            Debug.Log("▶️ 已恢復 Journal.cs");
        }
    }

}
