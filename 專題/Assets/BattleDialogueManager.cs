using Ink.Runtime;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BattleDialogueManager : MonoBehaviour
{
    [Header("UI")]
    public Text nameText;
    public Text dialogueText;
    public GameObject dialoguePanel;

    [Header("選項 UI")]
    public GameObject choiceContainer;
    public Button[] choiceButtons;

    [Header("Ink 劇本 JSON")]
    public TextAsset inkJSON;

    private Story story;
    private bool canContinue;
    private float inputDelay = 0.2f;
    private float inputTimer;

    public bool dialogueIsPlaying { get; private set; }
    private Action onDialogueComplete;


    void Awake()
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

        if (Input.GetKeyDown(KeyCode.Space) && story.currentChoices.Count == 0)
        {
            Debug.Log("⏩ 玩家按下空白鍵，繼續對話");
            ContinueStory();
            canContinue = false;
            inputTimer = 0f;
        }
    }

    // ===== 外部呼叫入口 =====
    public void EnterDialogueMode(TextAsset newInkJSON, string knotName = "start", Action onComplete = null)
    {
        Debug.Log("🎬 呼叫 EnterDialogueMode");

        if (newInkJSON == null)
        {
            Debug.LogWarning("⚠️ EnterDialogueMode：Ink JSON 為空，無法啟動對話。");
            return;
        }

        if (dialogueText != null)
        {
            dialogueText.text = "測試文字 (UI 應該要顯示)";
            Debug.Log("✅ 測試文字已設定");
        }

        inkJSON = newInkJSON;
        story = new Story(inkJSON.text);
        Debug.Log("📖 已建立 Ink Story");

        if (!string.IsNullOrEmpty(knotName))
        {
            try
            {
                story.ChoosePathString(knotName);
                Debug.Log($"📍 跳到 knot：{knotName}");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"⚠️ 指定的 knot 「{knotName}」不存在：{e.Message}");
            }
        }

        onDialogueComplete = onComplete;

        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(true);
            Debug.Log("🖼️ 對話面板已啟用");
        }
        else
        {
            Debug.LogError("❌ dialoguePanel 沒有指派，無法顯示對話框");
        }

        dialogueIsPlaying = true;
        canContinue = false;
        inputTimer = 0f;

        ContinueStory();
    }

    public void ContinueStory()
    {
        if (story != null && story.canContinue)
        {
            string line = story.Continue().Trim();
            if (dialogueText != null)
            {
                dialogueText.text = line;
                Debug.Log("📝 Ink 行文：" + line);
            }
            else
            {
                Debug.LogError("❌ dialogueText 沒有指派，文字無法顯示");
            }

            string speakerName = "";
            try
            {
                var v = story.variablesState["speaker"];
                if (v != null) speakerName = v.ToString();
            }
            catch { }

            if (nameText != null)
            {
                nameText.text = speakerName;
                Debug.Log("🎙️ 說話者：" + speakerName);
            }

            DisplayChoices();
        }
        else
        {
            Debug.Log("📕 Ink 劇本已結束，呼叫 EndDialogue()");
            EndDialogue();
        }
    }

    private void DisplayChoices()
    {
        List<Choice> choices = story.currentChoices;
        if (choiceContainer != null) choiceContainer.SetActive(choices.Count > 0);

        Debug.Log("🔀 當前選項數量：" + choices.Count);

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
                Debug.Log($"👉 選項 {i}：{choices[i].text}");
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
        Debug.Log($"✅ 玩家選擇選項 {choiceIndex}");
        story.ChooseChoiceIndex(choiceIndex);
        if (choiceContainer != null) choiceContainer.SetActive(false);
        ContinueStory();
    }

    private void EndDialogue()
    {
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        if (choiceContainer != null) choiceContainer.SetActive(false);

        dialogueIsPlaying = false;
        Debug.Log("🏁 對話結束");

        onDialogueComplete?.Invoke();
        onDialogueComplete = null;
    }
}
