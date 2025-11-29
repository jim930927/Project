using DG.Tweening.Core.Easing;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static ClueData;

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

    public Button BackMenuButton;

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
    public Button CDnextPageButton;
    public Button CDprevPageButton;

    public GameObject itemDetailPanel;
    public Text itemDetailText;
    public Button IDnextPageButton;
    public Button IDprevPageButton;
    public Button closeCDetailButton;
    public Button closeIDetailButton;

    [Header("Ink 整合")]
    public InkDialogueManager inkManager;
    private string pendingReturnKnot = "";

    [Header("線索兩頁容器")]
    public Transform clueLeftContainer;
    public Transform clueRightContainer;
    public Button cluePrevPageButton;
    public Button clueNextPageButton;

    [Header("道具兩頁容器")]
    public Transform itemLeftContainer;
    public Transform itemRightContainer;
    public Button itemPrevPageButton;
    public Button itemNextPageButton;

    [Header("頁面設定")]
    public int cluesPerPage = 9; // 左頁顯示幾個
    public int itemsPerPage = 9; // 左頁顯示幾個
    public int currentClueListPage = 0;
    public int currentItemListPage = 0;

    public bool closeBook = false;

    // 線索系統
    private List<Button> clueButtons = new List<Button>();
    private Dictionary<string, ClueData.Clue> clueLookup = new Dictionary<string, ClueData.Clue>();
    private ClueData.Clue currentClue;
    public int currentPage = 0;

    // 🟦 道具系統
    private List<Button> itemButtons = new List<Button>();
    private Dictionary<string, ItemData.Item> itemLookup = new Dictionary<string, ItemData.Item>();
    private ItemData.Item currentItem;
    public int currentItemPage = 0;

    void Start()
    {
        if (bookPanel != null)
            bookPanel.SetActive(false);


        bookIconButton?.onClick.AddListener(OpenBook);
        closeButton?.onClick.AddListener(CloseBook);

        CDnextPageButton?.onClick.AddListener(CDNextPage);
        CDprevPageButton?.onClick.AddListener(CDPrevPage);

        IDnextPageButton?.onClick.AddListener(IDNextPage);
        IDprevPageButton?.onClick.AddListener(IDPrevPage);


        closeCDetailButton?.onClick.AddListener(CloseClueDetailPanel);
        closeIDetailButton?.onClick.AddListener(CloseItemDetailPanel);

        clueTabButton?.onClick.AddListener(() => SwitchTab("clue")); // 🟦 新增
        itemTabButton?.onClick.AddListener(() => SwitchTab("item")); // 🟦 新增

        clueNextPageButton?.onClick.AddListener(NextClueListPage);
        cluePrevPageButton?.onClick.AddListener(PrevClueListPage);
        itemNextPageButton?.onClick.AddListener(NextItemListPage);
        itemPrevPageButton?.onClick.AddListener(PrevItemListPage);

        BackMenuButton?.onClick.AddListener(BackMenu);

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

        // 直接重建按鈕，避免 Refresh 與 Generate 的 index 不一致問題
        GenerateClueButtons();
        GenerateItemButtons();

        inkManager.SetPlayerCanMove(false);
        closeBook = true;
    }


    public void CloseBook()
    {
        closeBook = false;
        bookPanel?.SetActive(false);
        clueDetailPanel?.SetActive(false);
        inkManager.SetPlayerCanMove(true);
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
    public void GenerateClueButtons()
    {
        if (clueData == null || clueLeftContainer == null || clueRightContainer == null || clueButtonPrefab == null)
            return;

        // 清空左右頁
        foreach (Transform child in clueLeftContainer) Destroy(child.gameObject);
        foreach (Transform child in clueRightContainer) Destroy(child.gameObject);

        clueButtons.Clear();
        clueLookup.Clear();

        // 篩出已收集線索
        //var collectedClues = clueData.clues.FindAll(c => c.collected);
        var collectedClues = clueData.clues.FindAll(c => SaveClue.HasClue(c.id));

        collectedClues.Sort((a, b) => a.collectedTime.CompareTo(b.collectedTime));
        int total = collectedClues.Count;
        int cluesPerDoublePage = cluesPerPage * 2; // 一次顯示左右兩頁總共的數量
        int totalPages = Mathf.CeilToInt(total / (float)cluesPerDoublePage);



        // 確保頁數在合法範圍
        currentClueListPage = Mathf.Clamp(currentClueListPage, 0, Mathf.Max(totalPages - 1, 0));

        // 計算當前要顯示的線索範圍
        int startIndex = currentClueListPage * cluesPerDoublePage;
        int endIndex = Mathf.Min(startIndex + cluesPerDoublePage, total);

        // 取出這一組線索
        var currentSet = collectedClues.GetRange(startIndex, endIndex - startIndex);

        // 左右分頁顯示
        for (int i = 0; i < currentSet.Count; i++)
        {
            var clue = currentSet[i];
            Transform targetContainer = (i < cluesPerPage) ? clueLeftContainer : clueRightContainer;

            Button newButton = Instantiate(clueButtonPrefab, targetContainer);
            newButton.GetComponentInChildren<Text>().text = clue.name;
            newButton.onClick.AddListener(() => ShowClueDetail(clue));

            clueButtons.Add(newButton);
            clueLookup[clue.id] = clue;
        }

        // 控制上一頁／下一頁按鈕顯示
        cluePrevPageButton?.gameObject.SetActive(currentClueListPage > 0);
        clueNextPageButton?.gameObject.SetActive(currentClueListPage < totalPages - 1);
    }

    void NextClueListPage()
    {
        currentClueListPage++;
        GenerateClueButtons();
    }

    void PrevClueListPage()
    {
        currentClueListPage--;
        GenerateClueButtons();
    }

    void BackMenu()
    {
        SceneManager.LoadScene("MainMenu");
        LoadUIManager.ResetDatabase();

        GenerateClueButtons();
        GenerateItemButtons();
    }
    public void ShowClueDetail(ClueData.Clue clue)
    {
        if (clue == null) return;
        currentClue = clue;
        currentPage = 0;

        clueDetailPanel?.SetActive(true);

        // ============================
        // ⭐ 用 clueID 從 Resources 載入圖片
        // ============================
        var image = Resources.Load<Sprite>($"Clues/{clue.id}");
        if (image != null && PreviewImageManager.Instance != null)
        {
            Debug.Log($"🖼️ 顯示線索圖片：{clue.id}");
            PreviewImageManager.Instance.ShowImage(image);
        }
        else
        {
            Debug.LogWarning($"⚠️ 找不到圖片：Resources/Clues/{clue.id}.png 或 PreviewImageManager 未初始化");
        }

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

        clueDetailText.text = $"{pageText}";

        CDnextPageButton.gameObject.SetActive(currentPage < pageCount - 1);
        CDprevPageButton.gameObject.SetActive(currentPage > 0);
        IDnextPageButton.gameObject.SetActive(false);
        IDprevPageButton.gameObject.SetActive(false);

    }

    void CDNextPage() { currentPage++; UpdateCluePage(); }
    void CDPrevPage() { currentPage--; UpdateCluePage(); }

    void IDNextPage() { currentItemPage++; UpdateItemPage(); }
    void IDPrevPage() { currentItemPage--; UpdateItemPage(); }


    void OnClueAddedHandler(ClueData.Clue clue) => GenerateClueButtons();

    void RefreshClueButtons()
    {
        GenerateClueButtons();
        /*
        if (clueData == null) return;
        for (int i = 0; i < clueData.clues.Count; i++)
        {
            var clue = clueData.clues[i];
            if (i < clueButtons.Count)
                clueButtons[i].gameObject.SetActive(clue.collected);
        }
        */
    }

    // ===================== 道具 =====================
    public void GenerateItemButtons()
    {
        if (itemData == null || itemLeftContainer == null || itemRightContainer == null || itemButtonPrefab == null)
            return;

        foreach (Transform child in itemLeftContainer) Destroy(child.gameObject);
        foreach (Transform child in itemRightContainer) Destroy(child.gameObject);

        itemButtons.Clear();
        itemLookup.Clear();

        //var collectedItems = itemData.items.FindAll(i => i.collected);
        var collectedItems = itemData.items.FindAll(i => SaveItem.HasItem(i.id));

        collectedItems.Sort((a, b) => a.collectedTime.CompareTo(b.collectedTime));
        int total = collectedItems.Count;
        int itemsPerDoublePage = itemsPerPage * 2;
        int totalPages = Mathf.CeilToInt(total / (float)itemsPerDoublePage);

        currentItemListPage = Mathf.Clamp(currentItemListPage, 0, Mathf.Max(totalPages - 1, 0));

        int startIndex = currentItemListPage * itemsPerDoublePage;
        int endIndex = Mathf.Min(startIndex + itemsPerDoublePage, total);
        var currentSet = collectedItems.GetRange(startIndex, endIndex - startIndex);

        for (int i = 0; i < currentSet.Count; i++)
        {
            var item = currentSet[i];
            Transform targetContainer = (i < itemsPerPage) ? itemLeftContainer : itemRightContainer;

            Button newButton = Instantiate(itemButtonPrefab, targetContainer);
            newButton.GetComponentInChildren<Text>().text = item.name;
            newButton.onClick.AddListener(() => ShowItemDetail(item));

            itemButtons.Add(newButton);
            itemLookup[item.id] = item;
        }

        itemPrevPageButton?.gameObject.SetActive(currentItemListPage > 0);
        itemNextPageButton?.gameObject.SetActive(currentItemListPage < totalPages - 1);
    }

    void NextItemListPage()
    {
        currentItemListPage++;
        GenerateItemButtons();
    }

    void PrevItemListPage()
    {
        currentItemListPage--;
        GenerateItemButtons();
    }

    void ShowItemDetail(ItemData.Item item)
    {
        if (item == null) return;
        currentItem = item;
        currentItemPage = 0;
 
        itemDetailPanel?.SetActive(true);

        var image = Resources.Load<Sprite>($"Clues/{item.id}");
        if (image != null && PreviewImageManager.Instance != null)
        {
            Debug.Log($"🖼️ 顯示線索圖片：{item.id}");
            PreviewImageManager.Instance.ShowImage(image);
        }
        else
        {
            Debug.LogWarning($"⚠️ 找不到圖片：Resources/Clues/{item.id}.png 或 PreviewImageManager 未初始化");
        }
        UpdateItemPage();
    }


    void UpdateItemPage()
    {
        if (currentItem == null || itemDetailText == null) return;

        int pageCount = (currentItem.pages != null && currentItem.pages.Count > 0) ? currentItem.pages.Count : 1;
        currentItemPage = Mathf.Clamp(currentItemPage, 0, pageCount - 1);

        string pageText = (currentItem.pages != null && currentItem.pages.Count > 0)
            ? currentItem.pages[currentItemPage]
            : currentItem.fullContent ?? currentItem.detail;

        itemDetailText.text = $"{pageText}";

        IDnextPageButton.gameObject.SetActive(currentItemPage < pageCount - 1);
        IDprevPageButton.gameObject.SetActive(currentItemPage > 0);
        CDnextPageButton.gameObject.SetActive(false);
        CDprevPageButton.gameObject.SetActive(false);
    }

    void OnItemAddedHandler(ItemData.Item item) => GenerateItemButtons();

    void RefreshItemButtons()
    {
        GenerateItemButtons();
        /*
        if (itemData == null) return;
        for (int i = 0; i < itemData.items.Count; i++)
        {
            var item = itemData.items[i];
            if (i < itemButtons.Count)
                itemButtons[i].gameObject.SetActive(item.collected);
        }
        */
    }

    public void CloseClueDetailPanel()
    {
        Debug.LogWarning("關閉面板");
        if (clueDetailPanel != null)
            clueDetailPanel.SetActive(false);
        PreviewImageManager.Instance.HideImage();
        if (inkManager != null)
        {
            if (!string.IsNullOrEmpty(pendingReturnKnot))
            {
                inkManager.ShowPortraits();   // 🟩 新增
                inkManager.ResetPortraits();  // 🟩 新增
                inkManager.JumpToKnot(pendingReturnKnot);
                pendingReturnKnot = "";
            }
            else
            {
                inkManager.ShowPortraits();   // 🟩 新增
                inkManager.ResetPortraits();  // 🟩 新增
                if (closeBook == false)
                inkManager.ContinueStory();
            }
        }
    }
    public void CloseItemDetailPanel()
    {
        Debug.LogWarning("關閉面板");
        if (itemDetailPanel != null)
            itemDetailPanel.SetActive(false);
        PreviewImageManager.Instance.HideImage();
        if (inkManager != null)
        {
            if (!string.IsNullOrEmpty(pendingReturnKnot))
            {
                inkManager.ShowPortraits();   // 🟩 新增
                inkManager.ResetPortraits();  // 🟩 新增
                inkManager.JumpToKnot(pendingReturnKnot);
                pendingReturnKnot = "";
            }
            else
            {
                inkManager.ShowPortraits();   // 🟩 新增
                inkManager.ResetPortraits();  // 🟩 新增
                if (closeBook == false)
                    inkManager.ContinueStory();
            }
        }
    }


    // ✅ 舊 CluePickup 相容用：開啟線索畫面
    // ✅ 撿到線索時直接顯示內容（不開整本書）
    public void OpenClueOverlay(string clueID, string returnKnotName = "")
    {
        var clue = clueData.clues.Find(c => c.id == clueID);
        if (clue == null)
        {
            Debug.LogWarning($"⚠️ 找不到線索：{clueID}");
            return;
        }

        inkManager.SetPlayerCanMove(false);
        pendingReturnKnot = returnKnotName;

        // ✅ 不打開整本書，只打開線索內容面板
        clueDetailPanel?.SetActive(true);

        // 顯示該線索的內容（支援分頁）
        currentClue = clue;
        currentPage = 0;
        UpdateCluePage();
    }

    // ✅ 撿到道具時直接顯示內容（不開整本書）
    public void OpenItemOverlay(string itemID, string returnKnotName = "")
    {
        var item = itemData.items.Find(i => i.id == itemID);
        if (item == null)
        {
            Debug.LogWarning($"⚠️ 找不到道具：{itemID}");
            return;
        }

        inkManager.SetPlayerCanMove(false);

        pendingReturnKnot = returnKnotName;

        // ✅ 不打開整本書，只打開細節視窗
        itemDetailPanel?.SetActive(true);

        // 顯示該道具的內容（支援分頁）
        currentItem = item;
        currentItemPage = 0;
        UpdateItemPage();
    }


}
