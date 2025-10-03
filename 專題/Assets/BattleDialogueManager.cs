using UnityEngine;
using UnityEngine.UI;
using Ink.Runtime;
using System;
using System.Collections.Generic;

public class BattleDialogueManager : MonoBehaviour
{
    [Header("UI")]
    public Text nameText;
    public Text dialogueText;
    public GameObject dialoguePanel;

    [Header("選項 UI")]
    public GameObject choiceContainer;
    public Button[] choiceButtons; // 依序拖入 1~3 個按鈕

    [Header("Ink 劇本 JSON")]
    public TextAsset inkJSON; // 拖入戰鬥用 .ink 對應的 JSON

    private Story story;
    private bool canContinue;
    private float inputDelay = 0.2f;
    private float inputTimer;

    public bool dialogueIsPlaying { get; private set; }
    private Action onDialogueComplete;

    void Start()
    {
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        if (choiceContainer != null) choiceContainer.SetActive(false);

        dialogueIsPlaying = false;
        canContinue = false;
        inputTimer = 0f;
    }

    void Update()
    {
        if (!dialogueIsPlaying) return;

        if (!canContinue)
        {
            inputTimer += Time.deltaTime;
            if (inputTimer >= inputDelay) canContinue = true;
            return;
        }

        // 沒有選項時，空白鍵繼續
        if (Input.GetKeyDown(KeyCode.Space) && story.currentChoices.Count == 0)
        {
            ContinueStory();
            canContinue = false;
            inputTimer = 0f;
        }
    }

    // ===== 外部呼叫入口 =====
    public void EnterDialogueMode(TextAsset newInkJSON, string knotName = "start", Action onComplete = null)
    {
        if (newInkJSON == null)
        {
            Debug.LogWarning("⚠️ EnterDialogueMode：Ink JSON 為空，無法啟動對話。");
            return;
        }

        inkJSON = newInkJSON;
        story = new Story(inkJSON.text);

        if (!string.IsNullOrEmpty(knotName))
        {
            try
            {
                story.ChoosePathString(knotName);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"⚠️ 指定的 knot 「{knotName}」不存在：{e.Message}");
            }
        }

        onDialogueComplete = onComplete;

        // 🚩 確保打開面板
        if (dialoguePanel != null) dialoguePanel.SetActive(true);

        dialogueIsPlaying = true;
        canContinue = false;
        inputTimer = 0f;

        Debug.Log($"🎬 進入對話模式（knot = {knotName}）");

        ContinueStory();
    }

    // 使用 inspector 設定好的 JSON
    public void EnterDialogueMode(string knotName = "start", Action onComplete = null)
    {
        EnterDialogueMode(inkJSON, knotName, onComplete);
    }

    public void ContinueStory()
    {
        if (story != null && story.canContinue)
        {
            string line = story.Continue().Trim();
            if (dialogueText != null) dialogueText.text = line;

            Debug.Log("📝 Ink 行文：" + line);

            // 可選：Ink 變數 speaker
            string speakerName = "";
            try
            {
                var v = story.variablesState["speaker"];
                if (v != null) speakerName = v.ToString();
            }
            catch { /* 沒有 speaker 就跳過 */ }

            if (nameText != null) nameText.text = speakerName;

            DisplayChoices();
        }
        else
        {
            EndDialogue();
        }
    }

    private void DisplayChoices()
    {
        List<Choice> choices = story.currentChoices;
        if (choiceContainer != null) choiceContainer.SetActive(choices.Count > 0);

        for (int i = 0; i < choiceButtons.Length; i++)
        {
            if (i < choices.Count)
            {
                var btn = choiceButtons[i];
                btn.gameObject.SetActive(true);
                var txt = btn.GetComponentInChildren<Text>();
                if (txt != null) txt.text = choices[i].text;

                int choiceIndex = i;
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => OnChoiceSelected(choiceIndex));
            }
            else
            {
                if (choiceButtons[i] != null)
                    choiceButtons[i].gameObject.SetActive(false);
            }
        }
    }

    private void OnChoiceSelected(int choiceIndex)
    {
        story.ChooseChoiceIndex(choiceIndex);
        if (choiceContainer != null) choiceContainer.SetActive(false);
        ContinueStory();
    }

    private void EndDialogue()
    {
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        if (choiceContainer != null) choiceContainer.SetActive(false);

        dialogueIsPlaying = false;
        Debug.Log("✅ 對話結束");

        onDialogueComplete?.Invoke();
        onDialogueComplete = null;
    }
}
