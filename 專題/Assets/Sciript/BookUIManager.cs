using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class BookUIManager : MonoBehaviour
{
    [Header("基本UI")]
    public GameObject bookPanel;
    public Button bookIconButton;
    public Button closeButton;

    [Header("線索資料庫與模板")]
    public ClueData clueData;
    public Transform clueButtonContainer;
    public Button clueButtonPrefab;

    [Header("線索細節顯示")]
    public GameObject clueDetailPanel;   // 灰底框
    public Text clueDetailText;
    public Button nextPageButton;
    public Button prevPageButton;

    [Header("退出按鈕")]
    public Button closeDetailButton;

    private List<Button> clueButtons = new List<Button>();
    private Dictionary<string, ClueData.Clue> clueLookup = new Dictionary<string, ClueData.Clue>();

    private ClueData.Clue currentClue;
    private int currentPage = 0;

    void Start()
    {
        if (bookPanel != null)
            bookPanel.SetActive(false);

        if (bookIconButton != null)
            bookIconButton.onClick.AddListener(OpenBook);
        if (closeButton != null)
            closeButton.onClick.AddListener(CloseBook);

        if (nextPageButton != null)
            nextPageButton.onClick.AddListener(NextPage);
        if (prevPageButton != null)
            prevPageButton.onClick.AddListener(PrevPage);

        if (closeDetailButton != null)
        closeDetailButton.onClick.AddListener(CloseClueDetailPanel); // 綁定退出按鈕事件

        GenerateClueButtons();

        if (clueData != null)
            clueData.OnClueAdded += OnClueAddedHandler;

        if (clueDetailPanel != null)
            clueDetailPanel.SetActive(false);
    }

    void OnDestroy()
    {
        if (clueData != null)
            clueData.OnClueAdded -= OnClueAddedHandler;
    }

    void OpenBook()
    {
        bookPanel?.SetActive(true);
        RefreshClueButtons();
    }

    void CloseBook()
    {
        bookPanel?.SetActive(false);
        clueDetailPanel?.SetActive(false);
    }

    void GenerateClueButtons()
    {
        if (clueData == null || clueButtonContainer == null || clueButtonPrefab == null)
        {
            Debug.LogWarning("⚠️ ClueData 或按鈕容器 / Prefab 未設定！");
            return;
        }

        foreach (Transform child in clueButtonContainer)
            Destroy(child.gameObject);
        clueButtons.Clear();
        clueLookup.Clear();

        foreach (var clue in clueData.clues)
        {
            Button newButton = Instantiate(clueButtonPrefab, clueButtonContainer);
            newButton.GetComponentInChildren<Text>().text = clue.name;
            newButton.gameObject.SetActive(true);

            newButton.onClick.AddListener(() => ShowClueDetail(clue));

            clueButtons.Add(newButton);
            clueLookup[clue.id] = clue;
        }
    }

    void ShowClueDetail(ClueData.Clue clue)
    {
        if (clue == null) return;
        currentClue = clue;
        currentPage = 0;

        clueDetailPanel?.SetActive(true);
        UpdatePageContent();
    }

    void UpdatePageContent()
    {
        if (currentClue == null || clueDetailText == null)
            return;

        int pageCount = currentClue.pages != null && currentClue.pages.Count > 0 ? currentClue.pages.Count : 1;
        currentPage = Mathf.Clamp(currentPage, 0, pageCount - 1);

        string pageText;

        if (currentClue.pages != null && currentClue.pages.Count > 0)
            pageText = currentClue.pages[currentPage];
        else
            pageText = currentClue.fullContent ?? currentClue.detail;

        clueDetailText.text = $"{pageText}\n\n<color=#999>(第 {currentPage + 1}/{pageCount} 頁)</color>";

        if (nextPageButton != null)
            nextPageButton.gameObject.SetActive(currentPage < pageCount - 1);
        if (prevPageButton != null)
            prevPageButton.gameObject.SetActive(currentPage > 0);
    }

    void NextPage()
    {
        currentPage++;
        UpdatePageContent();
    }

    void PrevPage()
    {
        currentPage--;
        UpdatePageContent();
    }

    void OnClueAddedHandler(ClueData.Clue clue)
    {
        GenerateClueButtons();
    }

    void RefreshClueButtons()
    {
        if (clueData == null) return;

        for (int i = 0; i < clueData.clues.Count; i++)
        {
            var clue = clueData.clues[i];
            if (i < clueButtons.Count)
                clueButtons[i].gameObject.SetActive(clue.collected);
        }
    }

    // === 新增：關閉線索詳情面板 ===
     public void CloseClueDetailPanel()
     {
         if (clueDetailPanel != null)
             clueDetailPanel.SetActive(false);
     }
 }

