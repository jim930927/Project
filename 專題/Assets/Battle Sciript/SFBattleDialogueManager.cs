using Ink.Runtime;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SFBattleDialogueManager : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI dialogueText;
    public GameObject dialoguePanel;

    [Header("選項 UI")]
    public GameObject choiceContainer;
    public Button[] choiceButtons;

    public static SFBattleDialogueManager Instance;

    [Header("Ink 劇本 JSON")]
    public TextAsset inkJSON;

    public Story story;

    [Header("動畫控制")]
    public FightingAnimator fightAnimator;

    private bool dialogueIsPlaying = false;
    private bool questionsDropped = false;

    [Header("輸入控制設定")]
    public float inputDelay = 0.5f;
    private float inputTimer = 0f;
    private bool canContinue = false;
    private bool skipLocked = false;
    private bool isContinuing = false;
    private bool isShowingChoices = false;

    private Action onDialogueComplete;

    [Header("場景控制參數 (可自行設定)")]
    public string startKnot = "intro"; // 第二章開場節點
    public string nextSceneOnDone = "Third scene"; // 可在 Inspector 設定切換場景名稱

    void Awake()
    {
        Instance = this;
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        if (choiceContainer != null) choiceContainer.SetActive(false);
    }

    void Update()
    {
        if (!dialogueIsPlaying) return;

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

        if (isShowingChoices)
        {
            if (Input.GetKeyDown(KeyCode.Space))
                Debug.Log("🚫 顯示選項時空白鍵無效");
            return;
        }

        if (Input.GetKeyDown(KeyCode.Space) && canContinue && !skipLocked && !isContinuing)
        {
            skipLocked = true;
            StartCoroutine(SafeContinue());
        }
    }

    // ========= Ink 對話控制 =========
    public void EnterDialogueMode(TextAsset newInkJSON, string knotName = "", Action onComplete = null)
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

        string start = string.IsNullOrEmpty(knotName) ? startKnot : knotName;
        try
        {
            story.ChoosePathString(start);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"⚠️ 指定的 knot 「{start}」不存在：{e.Message}");
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

            foreach (var tag in story.currentTags)
            {
                if (tag.StartsWith("play_music"))
                {
                    string[] parts = tag.Split(' ');
                    if (parts.Length > 1)
                    {
                        string musicName = parts[1];
                        PlayMusic(musicName);
                    }
                }
            }

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
            if (story != null)
            {
                List<string> tags = story.currentTags ?? new List<string>();
                if (tags.Contains("DONE"))
                {
                    if (fightAnimator != null)
                    {
                        StartCoroutine(fightAnimator.PlayBattleOutro(() =>
                        {
                            UnityEngine.SceneManagement.SceneManager.LoadScene(nextSceneOnDone);
                        }));
                    }
                    else
                    {
                        UnityEngine.SceneManagement.SceneManager.LoadScene(nextSceneOnDone);
                    }
                    return;
                }
            }
            EndDialogue();
        }

        canContinue = false;
        inputTimer = 0f;
        skipLocked = true;
    }

    private void DisplayChoices()
    {
        List<Choice> choices = story.currentChoices;
        isShowingChoices = choices.Count >= 1;

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
        isShowingChoices = false;
        if (choiceContainer != null) choiceContainer.SetActive(false);

        if (fightAnimator != null)
        {
            StartCoroutine(fightAnimator.RaiseQuestions());
            questionsDropped = false;
        }

        story.ChooseChoiceIndex(choiceIndex);
        ContinueStory();
    }

    private void PlayMusic(string musicName)
    {
        var bgmManager = FindObjectOfType<BGMManager>();
        if (bgmManager != null)
        {
            bgmManager.PlayMusic(musicName);
        }
        else
        {
            Debug.LogWarning("⚠️ 找不到 BGMManager，無法播放音樂：" + musicName);
        }
    }

    private void EndDialogue()
    {
        dialogueIsPlaying = false;
        dialoguePanel.SetActive(false);
        if (choiceContainer != null) choiceContainer.SetActive(false);

        Debug.Log("🏁 第二章對話結束");

        if (fightAnimator != null)
        {
            StartCoroutine(fightAnimator.PlayBattleOutro(() =>
            {
                Debug.Log("🎞️ 第二章結束布幕動畫播放完畢");
                onDialogueComplete?.Invoke();
                onDialogueComplete = null;
            }));
        }
        else
        {
            onDialogueComplete?.Invoke();
            onDialogueComplete = null;
        }
    }
}
