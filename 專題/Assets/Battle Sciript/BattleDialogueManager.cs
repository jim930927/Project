using Ink.Runtime;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BattleDialogueManager : MonoBehaviour
{
    [Header("UI")]
    public Text nameText;
    public Text dialogueText;
    public GameObject dialoguePanel;

    [Header("選項 UI")]
    public GameObject choiceContainer;
    public Button[] choiceButtons;

    public static BattleDialogueManager Instance;

    [Header("Ink 劇本 JSON")]
    public TextAsset inkJSON;

    public Story story;

    [Header("動畫控制")]
    public FightingAnimator fightAnimator;

    private bool dialogueIsPlaying = false;
    private bool questionsDropped = false;

    // ===== 🧩 空白鍵控制相關 =====
    [Header("輸入控制設定")]
    public float inputDelay = 0.5f; // 顯示新句子後的延遲秒數
    private float inputTimer = 0f;
    private bool canContinue = false;  // 是否允許繼續
    private bool skipLocked = false;   // 防止重複按
    private bool isContinuing = false; // Ink 是否正在處理
    private bool isShowingChoices = false; // 是否顯示選項中

    private Action onDialogueComplete;

    void Awake()
    {
        Instance = this;
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        if (choiceContainer != null) choiceContainer.SetActive(false);
    }

    void Update()
    {
        if (!dialogueIsPlaying) return;

        // 🕒 延遲計時
        if (!canContinue)
        {
            inputTimer += Time.deltaTime;
            if (inputTimer >= inputDelay)
            {
                canContinue = true;
                skipLocked = false;
            }
            return;
        }

        // 🚫 顯示選項時禁止空白鍵
        if (isShowingChoices)
        {
            if (Input.GetKeyDown(KeyCode.Space))
                Debug.Log("🚫 顯示選項時空白鍵無效");
            return;
        }

        // ⏩ 空白鍵繼續對話
        if (Input.GetKeyDown(KeyCode.Space) && canContinue && !skipLocked && !isContinuing)
        {
            skipLocked = true;
            StartCoroutine(SafeContinue());
        }
    }

    // ========= 🪶 Ink 對話控制 =========
    public void EnterDialogueMode(TextAsset newInkJSON, string knotName = "start", Action onComplete = null)
    {
        if (newInkJSON == null)
        {
            Debug.LogWarning("⚠️ Ink JSON 為空，無法啟動對話。");
            return;
        }

        inkJSON = newInkJSON;
        story = new Story(inkJSON.text);
        dialogueIsPlaying = true;
        onDialogueComplete = onComplete;

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

        dialoguePanel.SetActive(true);
        ContinueStory();
    }

    private IEnumerator SafeContinue()
    {
        isContinuing = true;
        canContinue = false;
        inputTimer = 0f;

        yield return new WaitForSeconds(0.05f);

        ContinueStory();

        yield return new WaitForSeconds(0.15f);
        isContinuing = false;
    }

    public void ContinueStory()
    {
        if (story != null && story.canContinue)
        {
            string line = story.Continue().Trim();
            if (dialogueText != null)
                dialogueText.text = line;

            string speakerName = "";
            try
            {
                var v = story.variablesState["speaker"];
                if (v != null) speakerName = v.ToString();
            }
            catch { }

            if (nameText != null)
                nameText.text = speakerName;

            DisplayChoices();
        }
        else
        {
            EndDialogue();
        }

        // 每次顯示新句子後重新啟動延遲
        canContinue = false;
        inputTimer = 0f;
        skipLocked = true;
    }

    private void DisplayChoices()
    {
        List<Choice> choices = story.currentChoices;
        isShowingChoices = choices.Count >= 1; // 🚫 顯示選項時空白鍵無效

        if (choiceContainer != null)
            choiceContainer.SetActive(choices.Count > 0);

        if (choices.Count > 0 && !questionsDropped && fightAnimator != null)
        {
            StartCoroutine(fightAnimator.DropQuestions(choices.Count));
            questionsDropped = true;
        }

        for (int i = 0; i < choiceButtons.Length; i++)
        {
            if (i < choices.Count)
            {
                var btn = choiceButtons[i];
                btn.gameObject.SetActive(true);

                var tmp = btn.GetComponentInChildren<TextMeshProUGUI>();
                var txt = btn.GetComponentInChildren<Text>();

                if (tmp != null) tmp.text = choices[i].text;
                else if (txt != null) txt.text = choices[i].text;

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
        isShowingChoices = false; // ✅ 解鎖空白鍵
        if (choiceContainer != null) choiceContainer.SetActive(false);

        if (fightAnimator != null)
        {
            StartCoroutine(fightAnimator.RaiseQuestions());
            questionsDropped = false;
        }

        story.ChooseChoiceIndex(choiceIndex);
        ContinueStory();
    }

    private void EndDialogue()
    {
        dialogueIsPlaying = false;
        dialoguePanel.SetActive(false);
        if (choiceContainer != null) choiceContainer.SetActive(false);
        onDialogueComplete?.Invoke();
        onDialogueComplete = null;
        Debug.Log("🏁 對話結束");
    }
}
