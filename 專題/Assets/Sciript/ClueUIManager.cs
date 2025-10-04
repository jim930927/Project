using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class ClueUIManager : MonoBehaviour
{
    public static ClueUIManager Instance;

    [Header("UI")]
    public CanvasGroup cluePanel;
    public TextMeshProUGUI clueText;

    [Header("線索按鈕")]
    public Button[] clueButtons; // 三個線索欄位按鈕
    public TextMeshProUGUI[] clueLabels; // 每個按鈕上的文字（名稱）

    [Header("目前使用中線索顯示")]
    public TextMeshProUGUI currentClueLabel;

    [Header("動畫設定")]
    public float fadeDuration = 0.5f;
    public float displayTime = 2f;

    [Header("線索詳情顯示")]
    public TextMeshProUGUI clueDetailText;

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

        if (cluePanel != null)
            cluePanel.alpha = 0;

        // 初始化線索按鈕事件
        for (int i = 0; i < clueButtons.Length; i++)
        {
            int index = i;
            clueButtons[i].onClick.AddListener(() => OnClueButtonClicked(index));
        }

        UpdateClueButtons();
    }

    // 顯示獲得線索的提示
    public void ShowClue(string clueName)
    {
        if (cluePanel == null || clueText == null) return;

        clueText.text = $"📜 獲得線索：{clueName}";
        StopAllCoroutines();
        StartCoroutine(ShowRoutine());

        // 同時更新UI上的線索列表
        UpdateClueButtons();
    }

    IEnumerator ShowRoutine()
    {
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            cluePanel.alpha = Mathf.Lerp(0, 1, t / fadeDuration);
            yield return null;
        }

        yield return new WaitForSeconds(displayTime);

        t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            cluePanel.alpha = Mathf.Lerp(1, 0, t / fadeDuration);
            yield return null;
        }
    }

    // 🧩 更新三個線索按鈕上的文字
    public void UpdateClueButtons()
    {
        var clueData = Resources.Load<ClueData>("ClueDatabase");
        if (clueData == null || clueLabels == null) return;

        for (int i = 0; i < clueLabels.Length; i++)
        {
            if (i < clueData.clues.Count && clueData.clues[i].collected)
            {
                clueLabels[i].text = clueData.clues[i].name;
                clueButtons[i].interactable = true;
            }
            else
            {
                clueLabels[i].text = "???";
                clueButtons[i].interactable = false;
            }
        }
    }

    // 🧭 玩家點擊某個線索
    private void OnClueButtonClicked(int index)
    {
        var clueData = Resources.Load<ClueData>("ClueDatabase");
        if (clueData == null || index >= clueData.clues.Count) return;

        var clue = clueData.clues[index];
        if (!clue.collected) return;

        currentClueId = clue.id;
        if (currentClueLabel != null)
            currentClueLabel.text = $"🔍 使用中：{clue.name}";

        // 顯示詳細內容
        if (clueDetailText != null)
            clueDetailText.text = $"📜 {clue.name}\n{clue.detail}";

        Debug.Log($"🎯 玩家選擇線索：{clue.name} ({clue.id})");
    }
}
