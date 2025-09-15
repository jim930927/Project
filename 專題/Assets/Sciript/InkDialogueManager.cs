using UnityEngine;
using UnityEngine.UI;
using Ink.Runtime;
using System.Collections.Generic;

public class InkDialogueManager : MonoBehaviour
{
    [Header("UI 元件")]
    public Text nameText;
    public Text dialogueText;
    public GameObject dialoguePanel;

    [Header("選項 UI")]
    public GameObject choiceContainer;         // ChoiceContainer
    public Button[] choiceButtons;             // ChoiceButton1 / 2 / 3

    [Header("Ink 劇本")]
    public TextAsset inkJSON;

    private Story story;
    private bool canContinue = false;
    private float inputDelay = 0.2f;
    private float inputTimer = 0f;

    public bool dialogueIsPlaying { get; private set; }

    void Start()
    {
        dialoguePanel.SetActive(false);
        choiceContainer.SetActive(false);
        dialogueIsPlaying = false;
        canContinue = false;
        inputTimer = 0f;

        if (inkJSON != null)
        {
            story = new Story(inkJSON.text); // 預載入
        }
    }

    void Update()
    {
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

    public void EnterDialogueMode(TextAsset newInkJSON, string knotName = "")
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
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"⚠️ 指定節點「{knotName}」不存在於 Ink 劇本中：{e.Message}");
            }
        }

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
        Debug.Log("🟡 currentChoices 數量 = " + choices.Count); // 新增 Debug 訊息
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
                else
                {
                    Debug.LogWarning("❗ 找不到選項按鈕內的 Text 元件");
                }

                int choiceIndex = i; // 保留當前 i 值
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
            var pm = player.GetComponent<PlayerMovement>();
            if (pm != null)
                pm.canMove = canMove;
        }
    }
}
