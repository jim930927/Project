using UnityEngine;
using UnityEngine.UI;
using Ink.Runtime;
using System.Collections.Generic;
using System; // ✅ 為了 Action

public class InkDialogueManager : MonoBehaviour
{
    [Header("UI 元件")]
    public Text nameText;
    public Text dialogueText;
    public GameObject dialoguePanel;

    [Header("選項 UI")]
    public GameObject choiceContainer;
    public Button[] choiceButtons;

    [Header("Ink 劇本")]
    public TextAsset inkJSON;

    [Header("對話緩衝")]
    private float dialogueEndCooldown = 1f;
    private float dialogueEndTimer = 0f;

    private Story story;
    private bool canContinue = false;
    private float inputDelay = 0.2f;
    private float inputTimer = 0f;

    public bool dialogueIsPlaying { get; private set; }
    public bool IsInCooldown => dialogueEndTimer > 0f;

    // ✅ 新增：對話結束的 callback
    private Action onDialogueComplete;

    void Start()
    {
        dialoguePanel.SetActive(false);
        choiceContainer.SetActive(false);
        dialogueIsPlaying = false;
        canContinue = false;
        inputTimer = 0f;

        if (inkJSON != null)
        {
            story = new Story(inkJSON.text);
        }
    }

    void Update()
    {
        if (dialogueEndTimer > 0f)
        {
            dialogueEndTimer -= Time.deltaTime;
            return;
        }

        if (!dialoguePanel.activeSelf || !dialogueIsPlaying) return;

        if (!canContinue)
        {
            inputTimer += Time.deltaTime;
            if (inputTimer >= inputDelay)
                canContinue = true;
            return;
        }

        if (Input.GetKeyDown(KeyCode.Space) && story.currentChoices.Count == 0)
        {
            ContinueStory();
            canContinue = false;
            inputTimer = 0f;
        }
    }

    // ✅ 改寫：可以傳 callback
    public void EnterDialogueMode(TextAsset newInkJSON, string knotName = "", Action onComplete = null)
    {
        if (newInkJSON == null)
        {
            Debug.LogWarning("⚠️ Ink JSON 未指定，無法載入 Ink 對話。");
            return;
        }

        if (story == null || inkJSON != newInkJSON)
        {
            inkJSON = newInkJSON;
            story = new Story(inkJSON.text);
        }

        if (!string.IsNullOrEmpty(knotName))
        {
            try
            {
                story.ChoosePathString(knotName);
                Debug.Log($"✅ 成功跳到節點：{knotName}");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"⚠️ 指定節點「{knotName}」不存在於 Ink 劇本中：{e.Message}");
            }
        }

        onDialogueComplete = onComplete; // ✅ 記錄 callback

        dialoguePanel.SetActive(true);
        dialogueIsPlaying = true;
        canContinue = false;
        inputTimer = 0f;
        SetPlayerCanMove(false);

        ContinueStory();
    }

    public void ContinueStory()
    {
        if (story != null && story.canContinue)
        {
            string text = story.Continue().Trim();
            Debug.Log("Ink 顯示內容：" + text);
            dialogueText.text = text;
            nameText.text = ParseSpeaker(text);
            DisplayChoices();
        }
        else
        {
            dialoguePanel.SetActive(false);
            choiceContainer.SetActive(false);
            dialogueIsPlaying = false;
            Debug.Log("✅ Ink 對話結束");
            SetPlayerCanMove(true);

            dialogueEndTimer = dialogueEndCooldown;

            // ✅ 對話結束 → 呼叫 callback
            onDialogueComplete?.Invoke();
            onDialogueComplete = null;
        }
    }

    string ParseSpeaker(string line)
    {
        if (line.Contains("："))
        {
            string[] parts = line.Split('：');
            return parts[0];
        }
        return "";
    }

    void DisplayChoices()
    {
        List<Choice> choices = story.currentChoices;
        choiceContainer.SetActive(choices.Count > 0);

        for (int i = 0; i < choiceButtons.Length; i++)
        {
            if (i < choices.Count)
            {
                choiceButtons[i].gameObject.SetActive(true);
                Text choiceText = choiceButtons[i].GetComponentInChildren<Text>();
                if (choiceText != null)
                {
                    choiceText.text = choices[i].text;
                }

                int choiceIndex = i;
                choiceButtons[i].onClick.RemoveAllListeners();
                choiceButtons[i].onClick.AddListener(() => OnChoiceSelected(choiceIndex));
            }
            else
            {
                choiceButtons[i].gameObject.SetActive(false);
            }
        }
    }

    void OnChoiceSelected(int choiceIndex)
    {
        story.ChooseChoiceIndex(choiceIndex);
        choiceContainer.SetActive(false);
        ContinueStory();
    }

    void SetPlayerCanMove(bool canMove)
    {
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            var pm = player.GetComponent<Player>();
            if (pm != null)
                pm.canMove = canMove;
        }
    }
}
