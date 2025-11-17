using DG.Tweening;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class FinalBookSlider : MonoBehaviour
{
    public static FinalBookSlider Instance;

    [Header("書本控制")]
    public RectTransform bookWrapper;
    public Vector2 restPos = new Vector2(840f, 0f);
    public Vector2 slideOutPos = new Vector2(450f, 0f);
    public float duration = 0.5f;
    private bool isOpen = false;

    [Header("標籤控制 (分開滑出)")]
    public RectTransform[] tagRects;
    public TextMeshProUGUI[] tagLabels;
    public Vector2 tagHoverOffset = new Vector2(-20f, 0f);
    public float tagSlideDuration = 0.2f;
    private Vector2[] tagOriginalPos;

    [Header("線索細節")]
    public GameObject clueDetailPanel;
    public TextMeshProUGUI clueTitleText;
    public TextMeshProUGUI clueDescText;
    public Image clueImage;

    [Header("目前使用線索顯示")]
    public TextMeshProUGUI currentClueLabel;

    [Header("對應線索 ID")]
    public string[] tagClueIDs;

    [Header("多選模式")]
    public bool multiSelectMode = false; // 是否啟用多選模式（例如 Q2 時）
    public List<string> selectedClues = new List<string>();


    private ClueData clueData;
    [HideInInspector] public string currentClueId;


    [Header("標籤翻頁控制")]
    public int cluesPerPage = 4;  // 每頁顯示的標籤數量
    public Button nextPageButton;
    public Button prevPageButton;
    private int currentPage = 0;
    private int totalPages;
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        if (bookWrapper != null)
            bookWrapper.anchoredPosition = restPos;

        clueData = Resources.Load<ClueData>("ClueDatabase");
        if (clueDetailPanel != null)
            clueDetailPanel.SetActive(false);

        SetupTags();
        UpdateTagLabels();
        SetupPagination();
    }

    private void SetupTags()
    {
        if (tagRects == null || tagRects.Length == 0)
            return;

        tagOriginalPos = new Vector2[tagRects.Length];

        for (int i = 0; i < tagRects.Length; i++)
        {
            if (tagRects[i] == null) continue;

            tagOriginalPos[i] = tagRects[i].anchoredPosition;

            HoverTag hover = tagRects[i].gameObject.AddComponent<HoverTag>();
            hover.Init(tagRects[i], tagOriginalPos[i], tagHoverOffset, tagSlideDuration);

            Button btn = tagRects[i].GetComponent<Button>();
            if (btn == null)
                btn = tagRects[i].gameObject.AddComponent<Button>();

            string clueID = (tagClueIDs != null && i < tagClueIDs.Length) ? tagClueIDs[i] : "";
            if (!string.IsNullOrEmpty(clueID))
            {
                btn.onClick.RemoveAllListeners();
                string captured = clueID;
                btn.onClick.AddListener(() => OnClueSelected(captured));
            }
        }
    }

    public void UpdateTagLabels()
    {
        Debug.Log($"🔍 tagClueIDs.Length = {tagClueIDs.Length}");
        for (int k = 0; k < tagClueIDs.Length; k++)
            Debug.Log($"[{k}] = {tagClueIDs[k]}");

        if (clueData == null || tagLabels == null || tagClueIDs == null)
            return;

        int startIndex = currentPage * cluesPerPage;
        int endIndex = Mathf.Min(startIndex + cluesPerPage, tagClueIDs.Length);

        Debug.Log($"📘 更新標籤頁 {currentPage}, 顯示範圍: {startIndex} - {endIndex - 1}");

        for (int i = 0; i < tagLabels.Length; i++)
        {
            // 先預設隱藏內容
            tagLabels[i].text = "???";

            // 不在這一頁範圍內的，不處理
            if (i < startIndex || i >= endIndex)
                continue;

            int clueIndex = i; // 真正對應到 clueData 的 index

            if (clueIndex < tagClueIDs.Length)
            {
                string id = tagClueIDs[clueIndex];
                var clue = clueData.clues.Find(c => c.id == id);

                if (clue != null && SaveClue.HasClue(id))
                {
                    tagLabels[i].text = clue.name;
                    Debug.Log($"✅ 顯示線索[{i}] = {clue.name}");
                }
                else
                {
                    tagLabels[i].text = "???";
                    Debug.Log($"❌ 線索[{i}] 尚未收集");
                }
            }
        }
    }



    public void OnClueSelected(string clueID)
    {
        if (clueData == null) return;

        var clue = clueData.clues.Find(c => c.id == clueID);
        if (clue == null || !SaveClue.HasClue(clueID))
        {
            Debug.Log($"❌ 尚未獲得線索：{clueID}");
            return;
        }

        // 🧩 多選模式邏輯
        if (multiSelectMode)
        {
            // 若已選過，再次點擊則取消
            if (selectedClues.Contains(clueID))
            {
                selectedClues.Remove(clueID);
                Debug.Log($"🧩 取消選取線索：{clueID}");
            }
            else
            {
                selectedClues.Add(clueID);
                Debug.Log($"🧩 新增選取線索：{clueID}");
            }

            // 顯示目前已選的所有線索名稱
            if (currentClueLabel)
            {
                string joinedNames = string.Join("、", selectedClues
                    .ConvertAll(id => clueData.clues.Find(c => c.id == id)?.name ?? id));
                currentClueLabel.text = $"已選取線索 ({selectedClues.Count})：{joinedNames}";
            }

            // 同步 Ink 變數
            if (FinalBattleDialogueManager.Instance != null && FinalBattleDialogueManager.Instance.story != null)
            {
                string joinedIDs = string.Join(",", selectedClues);
                FinalBattleDialogueManager.Instance.story.variablesState["selected_clues"] = joinedIDs;
                Debug.Log($"📤 Ink變數 selected_clues 設為：{joinedIDs}");
            }

            if (clueDetailPanel != null)
                clueDetailPanel.SetActive(true);

            if (clueTitleText) clueTitleText.text = clue.name;
            if (clueDescText) clueDescText.text = string.IsNullOrEmpty(clue.detail) ? "這是 ??? 的細節" : clue.detail;
            if (clueImage) clueImage.color = Color.white;

            return;
        }


        currentClueId = clue.id;
        if (currentClueLabel)
            currentClueLabel.text = $"使用正確的線索回應敵人，線索使用中：{clue.name}";

        if (FinalBattleDialogueManager.Instance != null && FinalBattleDialogueManager.Instance.story != null)
        {
            FinalBattleDialogueManager.Instance.story.variablesState["current_clue"] = clue.id;
            Debug.Log($"🧩 Ink變數 current_clue (第二章) 設為：{clue.id}");
        }
    }


    public void SetMultiSelectMode(bool enable)
    {
        multiSelectMode = enable;
        selectedClues.Clear();
        if (currentClueLabel)
            currentClueLabel.text = enable ? "🧩 請選取多個線索來反駁敵人" : "";

        if (FinalBattleDialogueManager.Instance != null && FinalBattleDialogueManager.Instance.story != null)
            FinalBattleDialogueManager.Instance.story.variablesState["selected_clues"] = "";
    }

    private void SetupPagination()
    {
        if (tagRects == null || tagRects.Length == 0)
            return;

        totalPages = Mathf.CeilToInt(tagRects.Length / (float)cluesPerPage);
        currentPage = Mathf.Clamp(currentPage, 0, totalPages - 1);

        ShowPage(currentPage);

        if (nextPageButton != null)
            nextPageButton.onClick.AddListener(NextPage);
        if (prevPageButton != null)
            prevPageButton.onClick.AddListener(PrevPage);

        Debug.Log($"🔍 總標籤數: {tagRects.Length}, 每頁: {cluesPerPage}, 總頁數: {totalPages}");

    }

    private void ShowPage(int page)
    {
        int start = page * cluesPerPage;
        int end = Mathf.Min(start + cluesPerPage, tagRects.Length);

        for (int i = 0; i < tagRects.Length; i++)
        {
            tagRects[i].gameObject.SetActive(i >= start && i < end);
        }

        prevPageButton?.gameObject.SetActive(page > 0);
        nextPageButton?.gameObject.SetActive(page < totalPages - 1);

        // ✅ 每次顯示頁面時更新文字
        UpdateTagLabels();
    }



    public void NextPage()
    {
        if (currentPage < totalPages - 1)
        {
            currentPage++;
            ShowPage(currentPage);
            UpdateTagLabels();
        }
    }

    public void PrevPage()
    {
        if (currentPage > 0)
        {
            currentPage--;
            ShowPage(currentPage);
            UpdateTagLabels();
        }
    }

    public void CloseDetail()
    {
        if (clueDetailPanel != null)
            clueDetailPanel.SetActive(false);
    }

    public void ToggleBook()
    {
        isOpen = !isOpen;
        Vector2 target = isOpen ? slideOutPos : restPos;

        if (bookWrapper != null)
        {
            bookWrapper.DOKill();
            bookWrapper.DOAnchorPos(target, duration).SetEase(Ease.OutCubic);
        }
    }

    private class HoverTag : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private RectTransform rect;
        private Vector2 originalPos;
        private Vector2 offset;
        private float duration;

        public void Init(RectTransform rect, Vector2 originalPos, Vector2 offset, float duration)
        {
            this.rect = rect;
            this.originalPos = originalPos;
            this.offset = offset;
            this.duration = duration;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            rect.DOKill();
            rect.DOAnchorPos(originalPos + offset, duration).SetEase(Ease.OutCubic);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            rect.DOKill();
            rect.DOAnchorPos(originalPos, duration).SetEase(Ease.OutCubic);
        }
    }
}
