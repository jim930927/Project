using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class BookSlider : MonoBehaviour
{
    public static BookSlider Instance;


    [Header("書本控制")]
    public RectTransform bookWrapper;
    public Vector2 restPos = new Vector2(840f, 0f);
    public Vector2 slideOutPos = new Vector2(450f, 0f);
    public float duration = 0.5f;
    private bool isOpen = false;

    [Header("標籤控制 (分開滑出)")]
    public RectTransform[] tagRects;        // 三個標籤
    public TextMeshProUGUI[] tagLabels;     // 每個標籤上的文字
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
    public string[] tagClueIDs; // 每個標籤對應線索ID

    private ClueData clueData;
    [HideInInspector] public string currentClueId;

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
    }

    // ===========================================================
    // 🏷️ 初始化每個標籤：hover 效果 + 按鈕事件
    // ===========================================================
    private void SetupTags()
    {
        if (tagRects == null || tagRects.Length == 0)
            return;

        tagOriginalPos = new Vector2[tagRects.Length];

        for (int i = 0; i < tagRects.Length; i++)
        {
            if (tagRects[i] == null) continue;

            tagOriginalPos[i] = tagRects[i].anchoredPosition;

            // 加上 hover 控制
            HoverTag hover = tagRects[i].gameObject.AddComponent<HoverTag>();
            hover.Init(tagRects[i], tagOriginalPos[i], tagHoverOffset, tagSlideDuration);

            // 確保有按鈕元件
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

    // ===========================================================
    // 🧩 更新每個標籤上顯示的文字
    // ===========================================================
    public void UpdateTagLabels()
    {
        if (clueData == null || tagLabels == null)
            return;

        for (int i = 0; i < tagLabels.Length; i++)
        {
            if (i < tagClueIDs.Length)
            {
                string id = tagClueIDs[i];
                var clue = clueData.clues.Find(c => c.id == id);
                if (clue != null && clue.collected)
                    tagLabels[i].text = clue.name;
                else
                    tagLabels[i].text = "???";
            }
            else
            {
                tagLabels[i].text = "???";
            }
        }
    }

    // ===========================================================
    // 📖 點擊標籤 → 顯示線索詳情 + 設為使用中
    // ===========================================================
    public void OnClueSelected(string clueID)
    {
        if (clueData == null) return;

        var clue = clueData.clues.Find(c => c.id == clueID);
        if (clue == null || !clue.collected)
        {
            Debug.Log($"❌ 尚未獲得線索：{clueID}");
            return;
        }

        // 顯示詳細內容
        if (clueDetailPanel != null)
            clueDetailPanel.SetActive(true);

        if (clueTitleText) clueTitleText.text = clue.name;
        if (clueDescText) clueDescText.text = string.IsNullOrEmpty(clue.detail) ? "這是 ??? 的細節" : clue.detail;
        if (clueImage) clueImage.color = Color.white;

        // 設定目前使用線索
        currentClueId = clue.id;
        if (currentClueLabel)
            currentClueLabel.text = $"使用正確的線索回應敵人，線索使用中：{clue.name}";

        Debug.Log($"🎯 使用線索：{clue.name} ({clue.id})");

        // 設定 Ink 變數
        if (BattleDialogueManager.Instance != null && BattleDialogueManager.Instance.story != null)
        {
            BattleDialogueManager.Instance.story.variablesState["current_clue"] = clue.id;
            Debug.Log($"🧩 Ink變數 current_clue 設為：{clue.id}");
        }


    }

    public void CloseDetail()
    {
        if (clueDetailPanel != null)
            clueDetailPanel.SetActive(false);
    }

    // ===========================================================
    // 📚 開合書本動畫
    // ===========================================================
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

    // ===========================================================
    // 🪶 內部類別：標籤 hover 效果
    // ===========================================================
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
