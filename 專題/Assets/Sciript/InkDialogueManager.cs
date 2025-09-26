using UnityEngine;
using UnityEngine.UI;
using Ink.Runtime;
using System.Collections.Generic;
using System;

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

    [Header("角色立繪區域")]
    public Image leftPortraitImage;       // 左側立繪
    public Image rightPortraitImage;      // 右側立繪
    public Sprite leftDefaultPortrait;    // 左側預設立繪
    public Sprite rightDefaultPortrait;   // 右側預設立繪
    public CharacterPortrait[] portraits; // 設定角色對應的立繪 + 位置

    [Header("對話緩衝")]
    public float dialogueEndCooldown = 1f;
    private float dialogueEndTimer = 0f;

    private Story story;
    private bool canContinue = false;
    private float inputDelay = 0.2f;
    private float inputTimer = 0f;

    public bool dialogueIsPlaying { get; private set; }
    public bool IsInCooldown => dialogueEndTimer > 0f;

    private Action onDialogueComplete;

    void Start()
    {
        dialoguePanel.SetActive(false);
        choiceContainer.SetActive(false);
        dialogueIsPlaying = false;

        HidePortraits(); // 🚩 一開始隱藏立繪
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

    public void EnterDialogueMode(TextAsset newInkJSON, string knotName = "", Action onComplete = null)
    {
        if (newInkJSON == null) return;

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
            catch { }
        }

        onDialogueComplete = onComplete;

        dialoguePanel.SetActive(true);
        dialogueIsPlaying = true;
        canContinue = false;
        inputTimer = 0f;
        SetPlayerCanMove(false);

        ShowPortraits();   // 🚩 對話開始 → 顯示立繪區
        ResetPortraits();  // 🚩 初始化為左右 defaultPortrait
        ContinueStory();
    }

    public void ContinueStory()
    {
        if (story != null && story.canContinue)
        {
            string text = story.Continue().Trim();
            dialogueText.text = text;

            // 從 Ink 變數抓 speaker
            string speakerName = "";
            try
            {
                var value = story.variablesState["speaker"];
                if (value != null) speakerName = value.ToString();
            }
            catch
            {
                Debug.LogWarning("⚠️ Ink 變數 'speaker' 不存在");
            }

            nameText.text = speakerName;

            // 切換立繪
            UpdatePortrait(speakerName);

            DisplayChoices();
        }
        else
        {
            dialoguePanel.SetActive(false);
            choiceContainer.SetActive(false);
            dialogueIsPlaying = false;
            SetPlayerCanMove(true);

            dialogueEndTimer = dialogueEndCooldown;

            HidePortraits(); // 🚩 對話結束 → 隱藏立繪

            onDialogueComplete?.Invoke();
            onDialogueComplete = null;
        }
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
                choiceButtons[i].GetComponentInChildren<Text>().text = choices[i].text;
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

    void UpdatePortrait(string speakerName)
    {
        // 預設兩邊先放上 default
        leftPortraitImage.sprite = leftDefaultPortrait;
        rightPortraitImage.sprite = rightDefaultPortrait;

        foreach (var entry in portraits)
        {
            if (entry.speakerName == speakerName)
            {
                if (entry.position == PortraitPosition.Left)
                    leftPortraitImage.sprite = entry.sprite;
                else
                    rightPortraitImage.sprite = entry.sprite;

                return;
            }
        }
    }
    void HidePortraits()
    {
        if (leftPortraitImage != null) leftPortraitImage.gameObject.SetActive(false);
        if (rightPortraitImage != null) rightPortraitImage.gameObject.SetActive(false);
    }
    void ShowPortraits()
    {
        if (leftPortraitImage != null) leftPortraitImage.gameObject.SetActive(true);
        if (rightPortraitImage != null) rightPortraitImage.gameObject.SetActive(true);
    }
    void ResetPortraits()
    {
        if (leftPortraitImage != null) leftPortraitImage.sprite = leftDefaultPortrait;
        if (rightPortraitImage != null) rightPortraitImage.sprite = rightDefaultPortrait;
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

[System.Serializable]
public class CharacterPortrait
{
    public string speakerName;     // Ink 變數 speaker 的值
    public Sprite sprite;          // 對應立繪
    public PortraitPosition position; // 左 / 右
}

public enum PortraitPosition
{
    Left,
    Right
}
