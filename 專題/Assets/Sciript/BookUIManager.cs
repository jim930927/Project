using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class BookUIManager : MonoBehaviour
{
    [Header("基本UI")]
    public GameObject bookPanel;
    public Button bookIconButton;
    public Button closeButton;

    [Header("標籤切換")] // 🟦 新增
    public Button clueTabButton;
    public Button itemTabButton;
    public GameObject cluePage;  // 線索頁
    public GameObject itemPage;  // 道具頁

    [Header("線索資料庫與模板")]
    public ClueData clueData;
    public Transform clueButtonContainer;
    public Button clueButtonPrefab;

    [Header("道具資料庫與模板")] // 🟦 新增
    public ItemData itemData;
    public Transform itemButtonContainer;
    public Button itemButtonPrefab;

    [Header("細節顯示區")]
    public GameObject clueDetailPanel;
    public Text clueDetailText;
    public Button nextPageButton;
    public Button prevPageButton;
    public Button closeDetailButton;

    [Header("Ink 整合")]
    public InkDialogueManager inkManager;
    private string pendingReturnKnot = "";

    // 線索系統
    private List<Button> clueButtons = new List<Button>();
    private Dictionary<string, ClueData.Clue> clueLookup = new Dictionary<string, ClueData.Clue>();
    private ClueData.Clue currentClue;
    private int currentPage = 0;

    // 🟦 道具系統
    private List<Button> itemButtons = new List<Button>();
    private Dictionary<string, ItemData.Item> itemLookup = new Dictionary<string, ItemData.Item>();
    private ItemData.Item currentItem;
    private int currentItemPage = 0;

    void Start()
    {
        if (bookPanel != null)
            bookPanel.SetActive(false);

        bookIconButton?.onClick.AddListener(OpenBook);
        closeButton?.onClick.AddListener(CloseBook);
        nextPageButton?.onClick.AddListener(NextPage);
        prevPageButton?.onClick.AddListener(PrevPage);
        closeDetailButton?.onClick.AddListener(CloseClueDetailPanel);

        clueTabButton?.onClick.AddListener(() => SwitchTab("clue")); // 🟦 新增
        itemTabButton?.onClick.AddListener(() => SwitchTab("item")); // 🟦 新增

        GenerateClueButtons();
        GenerateItemButtons(); // 🟦 新增

        if (clueData != null)
            clueData.OnClueAdded += OnClueAddedHandler;
        if (itemData != null)
            itemData.OnItemAdded += OnItemAddedHandler; // 🟦 新增

        clueDetailPanel?.SetActive(false);

        // 🟦 預設顯示線索頁
        SwitchTab("clue");
    }

    void OnDestroy()
    {
        if (clueData != null)
            clueData.OnClueAdded -= OnClueAddedHandler;
        if (itemData != null)
            itemData.OnItemAdded -= OnItemAddedHandler;
    }

    public void OpenBook()
    {
        bookPanel?.SetActive(true);
        RefreshClueButtons();
        RefreshItemButtons();
    }

    public void CloseBook()
    {
        bookPanel?.SetActive(false);
        clueDetailPanel?.SetActive(false);
    }

    // 🟦 標籤切換
    void SwitchTab(string tab)
    {
        if (tab == "clue")
        {
            cluePage.SetActive(true);
            itemPage.SetActive(false);
        }
        else if (tab == "item")
        {
            cluePage.SetActive(false);
            itemPage.SetActive(true);
        }
    }

    // ===================== 線索 =====================
    void GenerateClueButtons()
    {
        if (clueData == null || clueButtonContainer == null || clueButtonPrefab == null) return;

        foreach (Transform child in clueButtonContainer)
            Destroy(child.gameObject);

        clueButtons.Clear();
        clueLookup.Clear();

        foreach (var clue in clueData.clues)
        {
            Button newButton = Instantiate(clueButtonPrefab, clueButtonContainer);
            newButton.GetComponentInChildren<Text>().text = clue.name;
            newButton.gameObject.SetActive(clue.collected);

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
        UpdateCluePage();
    }

    void UpdateCluePage()
    {
        if (currentClue == null || clueDetailText == null) return;

        int pageCount = (currentClue.pages != null && currentClue.pages.Count > 0) ? currentClue.pages.Count : 1;
        currentPage = Mathf.Clamp(currentPage, 0, pageCount - 1);

        string pageText = (currentClue.pages != null && currentClue.pages.Count > 0)
            ? currentClue.pages[currentPage]
            : currentClue.fullContent ?? currentClue.detail;

        clueDetailText.text = $"{pageText}\n\n<color=#999>(第 {currentPage + 1}/{pageCount} 頁)</color>";

        nextPageButton.gameObject.SetActive(currentPage < pageCount - 1);
        prevPageButton.gameObject.SetActive(currentPage > 0);
    }

    void NextPage() { currentPage++; UpdateCluePage(); }
    void PrevPage() { currentPage--; UpdateCluePage(); }

    void OnClueAddedHandler(ClueData.Clue clue) => GenerateClueButtons();

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

    // ===================== 道具 =====================
    void GenerateItemButtons()
    {
        if (itemData == null || itemButtonContainer == null || itemButtonPrefab == null) return;

        foreach (Transform child in itemButtonContainer)
            Destroy(child.gameObject);

        itemButtons.Clear();
        itemLookup.Clear();

        foreach (var item in itemData.items)
        {
            Button newButton = Instantiate(itemButtonPrefab, itemButtonContainer);
            newButton.GetComponentInChildren<Text>().text = item.name;
            newButton.gameObject.SetActive(item.collected);

            newButton.onClick.AddListener(() => ShowItemDetail(item));
            itemButtons.Add(newButton);
            itemLookup[item.id] = item;
        }
    }

    void ShowItemDetail(ItemData.Item item)
    {
        if (item == null) return;
        currentItem = item;
        currentItemPage = 0;

        clueDetailPanel?.SetActive(true);
        UpdateItemPage();
    }

    void UpdateItemPage()
    {
        if (currentItem == null || clueDetailText == null) return;

        int pageCount = (currentItem.pages != null && currentItem.pages.Count > 0) ? currentItem.pages.Count : 1;
        currentItemPage = Mathf.Clamp(currentItemPage, 0, pageCount - 1);

        string pageText = (currentItem.pages != null && currentItem.pages.Count > 0)
            ? currentItem.pages[currentItemPage]
            : currentItem.fullContent ?? currentItem.detail;

        clueDetailText.text = $"{pageText}\n\n<color=#999>(第 {currentItemPage + 1}/{pageCount} 頁)</color>";

        nextPageButton.gameObject.SetActive(currentItemPage < pageCount - 1);
        prevPageButton.gameObject.SetActive(currentItemPage > 0);
    }

    void OnItemAddedHandler(ItemData.Item item) => GenerateItemButtons();

    void RefreshItemButtons()
    {
        if (itemData == null) return;
        for (int i = 0; i < itemData.items.Count; i++)
        {
            var item = itemData.items[i];
            if (i < itemButtons.Count)
                itemButtons[i].gameObject.SetActive(item.collected);
        }
    }

    public void CloseClueDetailPanel()
    {
        if (clueDetailPanel != null)
            clueDetailPanel.SetActive(false);

        if (inkManager != null)
        {
            if (!string.IsNullOrEmpty(pendingReturnKnot))
            {
                inkManager.JumpToKnot(pendingReturnKnot);
                pendingReturnKnot = "";
            }
            else
            {
                inkManager.ContinueStory();
            }
        }
    }

    // ✅ 舊 CluePickup 相容用：開啟線索畫面
    public void OpenClueOverlay(string clueID, string returnKnotName = "")
    {
        var clue = clueData.clues.Find(c => c.id == clueID);
        if (clue == null)
        {
            Debug.LogWarning($"⚠️ 找不到線索：{clueID}");
            return;
        }

        pendingReturnKnot = returnKnotName;

        // 打開書頁並切到線索分頁
        OpenBook();
        SwitchTab("clue");

        // 顯示該線索內容
        ShowClueDetail(clue);
    }

}
