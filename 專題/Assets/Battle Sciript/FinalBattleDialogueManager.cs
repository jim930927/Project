using Ink.Runtime;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class FinalBattleDialogueManager : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI dialogueText;
    public GameObject dialoguePanel;

    [Header("選項 UI")]
    public GameObject choiceContainer;
    public Button[] choiceButtons;

    public Hp_battle hp_Battle;

    public static FinalBattleDialogueManager Instance;

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
    private bool inputLockedByChoices = false; // 🔒 用來控制選項期間的輸入鎖定

    [Header("敵人圖片控制")]
    public Image enemyImage;
    public Sprite[] enemySprites; // 不同敵人的圖片

    public GameObject fullblackscreen;

    private Action onDialogueComplete;

    [Header("場景控制參數 (可自行設定)")]
    public string startKnot = "start"; // 第二章開場節點
    public string nextSceneOnDone = "The End"; // 可在 Inspector 設定切換場景名稱

    void Awake()
    {
        Instance = this;
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        if (choiceContainer != null) choiceContainer.SetActive(false);
    }

    void Update()
    {
        if (!dialogueIsPlaying) return;
        // 若正在顯示選項，完全忽略空白鍵輸入
        if (isShowingChoices)
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                Debug.Log("🛑 空白鍵被吃掉（選項中）");
            }
            return;
        }

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

        if(isShowingChoices && Input.GetKeyDown(KeyCode.Space))
{
            // 直接吃掉輸入
            Debug.Log("stop");
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
            // 🔹 若這段有 # wrong 標籤，扣血並跳回 q1
            if (story.currentTags.Contains("wrong"))
            {
                if (hp_Battle != null)
                {
                    hp_Battle.Bhp -= 1;
                    hp_Battle.PlayDamageEffect();
                    Debug.Log($"❌ 答錯！扣血 -> 當前血量：{hp_Battle.Bhp}");
                }
            }

            // 🔹 支援影片播放 #play_cg xxx
            foreach (var tag in story.currentTags)
            {
                if (tag.StartsWith("play_cg"))
                {
                    string[] parts = tag.Split(' ');
                    string cgName = parts.Length > 1 ? parts[1] : "DefaultCG";
                    StartCoroutine(PlayCGThenContinue(cgName));
                    return; // 暫停，等待 CG 播完
                }
            }


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
                    // 🩸 戰鬥勝利 → HP +1
                    var hp = FindObjectOfType<HP>();
                    if (hp != null && hp.hp < 10) hp.hp += 1;
                    Debug.Log($"🔥 最終 HP 數值 = {hp.hp}");

                    // =====🔽 新增：依照 HP 分支跳結局場景 =====
                    string targetScene = "";

                    if (hp != null)
                    {
                        if (hp.hp == 10)
                            targetScene = "TrueEnd";
                        else if (hp.hp >= 4 && hp.hp <= 9)
                            targetScene = "NormalEnd";
                        else
                            targetScene = "BadEnd"; // 預設（避免意外情況）
                    }
                    else
                    {
                        targetScene = nextSceneOnDone;
                    }
                    // =====🔼 結束：依照 HP 分支跳結局 =====

                    if (fightAnimator != null)
                    {
                        StartCoroutine(fightAnimator.PlayBattleOutro(() =>
                        {
                            UnityEngine.SceneManagement.SceneManager.LoadScene(targetScene);
                        }));
                    }
                    else
                    {
                        UnityEngine.SceneManagement.SceneManager.LoadScene(targetScene);
                    }
                    return;
                }

            }
            EndDialogue();
        }

        canContinue = false;
        inputTimer = 0f;
        skipLocked = true;

        // 🔒 避免按鍵還在按著 → 自動 continue / 自動選項 / 誤觸 UI
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);

        Input.ResetInputAxes();
    }

    private void DisplayChoices()
    {
        // 鎖一下輸入，防止剛跳出選項時空白被誤觸
        StartCoroutine(LockInputTemporarily(0.2f));

        List<Choice> choices = story.currentChoices;
        isShowingChoices = choices.Count > 0;

        if (choiceContainer != null)
            choiceContainer.SetActive(isShowingChoices);

        if (isShowingChoices && !questionsDropped && fightAnimator != null)
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

        Debug.Log($"🟢 DisplayChoices(): choices={choices.Count}, isShowingChoices={isShowingChoices}");
    }


    private void OnChoiceSelected(int choiceIndex)
    {
        UpdateClueVariablesInInk();
        isShowingChoices = false;
        if (choiceContainer != null) choiceContainer.SetActive(false);

        if (fightAnimator != null)
        {
            StartCoroutine(fightAnimator.RaiseQuestions());
            questionsDropped = false;
        }

        story.ChooseChoiceIndex(choiceIndex);

        // ✅ 延遲一點再繼續，讓 Ink 更新完 currentChoices
        StartCoroutine(ContinueAfterChoice());
    }

    private IEnumerator ContinueAfterChoice()
    {
        yield return new WaitForEndOfFrame(); // 或 WaitForSeconds(0.05f)
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
    IEnumerator LockInputTemporarily(float duration)
    {
        canContinue = false;
        skipLocked = true;
        yield return new WaitForSeconds(duration);
        canContinue = true;
        skipLocked = false;
    }

    private void UpdateClueVariablesInInk()
    {
        if (story == null) return;

        // 從 FinalBookSlider 取得目前選取的線索
        var book = FinalBookSlider.Instance;
        if (book != null)
        {
            string joinedIDs = "";

            // 如果多選模式中，取多個
            if (book.multiSelectMode)
            {
                joinedIDs = string.Join(",", book.selectedClues);
            }
            else
            {
                joinedIDs = book.currentClueId;
            }

            story.variablesState["selected_clues"] = joinedIDs ?? "";
            Debug.Log($"📤 更新 Ink 變數 selected_clues = {joinedIDs}");
        }
    }

    private IEnumerator PlayCGThenContinue(string cgName = "DefaultCG")
    {
        dialoguePanel.SetActive(false);
        fullblackscreen.SetActive(true);

        // 尋找 CGPanel
        GameObject cgPanel = GameObject.Find("CGPanel");
        if (cgPanel == null)
        {
            Debug.LogWarning("⚠️ FinalBattle 找不到 CGPanel");
            yield break;
        }

        // 找 RawImage
        var raws = cgPanel.GetComponentsInChildren<UnityEngine.UI.RawImage>(true);
        foreach (var r in raws)
            r.gameObject.SetActive(false);

        UnityEngine.UI.RawImage targetRaw = null;

        foreach (var r in raws)
        {
            if (r.name.Equals(cgName, StringComparison.OrdinalIgnoreCase) ||
                r.name.Contains(cgName))
            {
                targetRaw = r;
                break;
            }
        }

        if (targetRaw == null && raws.Length > 0)
            targetRaw = raws[0];

        if (targetRaw == null)
        {
            Debug.LogWarning("⚠️ 找不到對應的 RawImage");
            yield break;
        }

        targetRaw.gameObject.SetActive(true);

        // 找 VideoPlayer
        var video = cgPanel.GetComponent<UnityEngine.Video.VideoPlayer>();
        if (video == null)
            video = targetRaw.GetComponent<UnityEngine.Video.VideoPlayer>();

        if (video == null)
        {
            Debug.LogWarning("⚠️ 找不到 VideoPlayer");
            yield break;
        }

        // Load resources/CG/cgName
        var clip = Resources.Load<UnityEngine.Video.VideoClip>($"CG/{cgName}");
        if (clip != null)
        {
            video.source = UnityEngine.Video.VideoSource.VideoClip;
            video.clip = clip;
        }
        else
        {
            Debug.LogWarning($"⚠️ 找不到影片：Resources/CG/{cgName}.mp4");
        }

        video.Prepare();
        while (!video.isPrepared) yield return null;

        // 播放！
        targetRaw.texture = video.targetTexture;
        video.Play();

        bool finished = false;
        video.loopPointReached += (v) => { finished = true; };

        while (!finished)
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                video.Stop();
                finished = true;
            }
            yield return null;
        }

        // 關閉所有 CG 畫面
        foreach (var r in raws) r.gameObject.SetActive(false);

        // 回到對話
        if (story.canContinue)
            ContinueStory();
        else
            EndDialogue();

        dialoguePanel.SetActive(true);
    }


}
