using DG.Tweening;
using Ink.Runtime;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static ClueData;

public class InkDialogueManager : MonoBehaviour
{
    [Header("UI 元件")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI dialogueText;
    public GameObject dialoguePanel;

    [Header("選項 UI")]
    public GameObject choiceContainer;
    public Button[] choiceButtons;

    [Header("HP Mirror（Ink→Unity）")]
    [SerializeField] private HP hpRef;   // 在 Inspector 指到「hp」物件（掛著 HP.cs 的那個）

    [Header("Ink 劇本")]
    public TextAsset inkJSON;

    [Header("角色立繪區域")]
    public Image leftPortraitImage;
    public Image rightPortraitImage;
    public Sprite leftDefaultPortrait;
    public Sprite rightDefaultPortrait;
    public CharacterPortrait[] portraits;

    [Header("對話緩衝")]
    public float dialogueEndCooldown = 1f;
    private float dialogueEndTimer = 0f;

    [Header("布幕設定（只關閉時使用）")]
    public RectTransform leftCurtain;
    public RectTransform rightCurtain;
    public Vector2 leftClosePos = new Vector2(0, 0);
    public Vector2 rightClosePos = new Vector2(0, 0);
    public float curtainCloseDuration = 1.2f;
    public string battleSceneName = "BattleScene";

    [Header("對應線索 ID")]
    public string[] tagClueIDs;

    public ClueData clueDatabase;

    private Vector2 leftOriginPos;
    private Vector2 rightOriginPos;
    private bool curtainInitialized = false;

    private Story story;
    private bool canContinue = false;
    private float inputDelay = 0.5f;
    private float inputTimer = 0f;

    public bool dialogueIsPlaying { get; private set; }
    public bool IsInCooldown => dialogueEndTimer > 0f;

    private Action onDialogueComplete;

    private bool firstTagCheck = true; // 新增這個在 class 層級

    void Start()
    {
        dialoguePanel.SetActive(false);
        choiceContainer.SetActive(false);
        dialogueIsPlaying = false;

        HidePortraits();
        InitCurtain();

        // 自動啟動 Ink 劇本
        if (inkJSON != null)
        {
            Debug.Log("🎬 自動啟動 Ink 劇本，從 === CG === 開始，播放cg");
            EnterDialogueMode(inkJSON, "CG");

        }
        else
        {
            Debug.LogWarning("⚠️ Ink JSON 未指派，無法自動啟動對話。");
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

    void InitCurtain()
    {
        if (leftCurtain != null && rightCurtain != null && !curtainInitialized)
        {
            leftOriginPos = leftCurtain.anchoredPosition;
            rightOriginPos = rightCurtain.anchoredPosition;
            curtainInitialized = true;
        }
    }

    void SyncHpFromInk()
    {
        if (story?.variablesState == null) return;
        if (hpRef == null) hpRef = FindFirstObjectByType<HP>();  // 備援

        if (hpRef != null)
        {
            object v = null;
            try { v = story.variablesState["hp"]; } catch { }
            if (v != null) hpRef.hp = Mathf.Max(0, System.Convert.ToInt32(v));
        }
    }




    public void EnterDialogueMode(TextAsset newInkJSON, string knotName = "", Action onComplete = null)
    {
        if (newInkJSON == null) return;

        if (story == null || inkJSON != newInkJSON)
        {
            inkJSON = newInkJSON;
            story = new Story(inkJSON.text);
            story.BindExternalFunction("canStartBattle", () =>
            {
                // ✅ 改成只檢查特定線索
                return clueDatabase.HasCollectedClues("Letter", "Journal", "NPC_talk");
            });

            BindExternalBookFunctions(); // 🔹 綁定 Ink 外部函式
            story.ObserveVariable("hp", (string name, object value) =>
            {
                if (hpRef == null) hpRef = FindFirstObjectByType<HP>(); // 備援抓場上第一個 HP
                if (hpRef == null) return;
                hpRef.hp = Mathf.Max(0, System.Convert.ToInt32(value)); // 無上限，保底 0
            });

            // === ② 初次同步一次（避免剛進入時 Inspector 沒顯示）===
            try
            {
                var v = story.variablesState["hp"];
                if (hpRef == null) hpRef = FindFirstObjectByType<HP>();
                if (hpRef != null && v != null)
                    hpRef.hp = Mathf.Max(0, System.Convert.ToInt32(v));
            }
            catch { /* hp 可能尚未在 Ink 宣告 */ }
        }

        if (!string.IsNullOrEmpty(knotName))
        {
            try { story.ChoosePathString(knotName); } catch { }
        }

        onDialogueComplete = onComplete;

        dialoguePanel.SetActive(true);
        dialogueIsPlaying = true;
        canContinue = false;
        inputTimer = 0f;
        SetPlayerCanMove(false);

        ShowPortraits();
        ResetPortraits();
        ContinueStory();

    }

    // 🔹 Ink 外部函式綁定區
    private void BindExternalBookFunctions()
    {
        if (story == null) return;

        var bookUI = FindObjectOfType<BookUIManager>();
        if (bookUI == null)
        {
            Debug.LogWarning("⚠️ 找不到 BookUIManager，無法綁定 Ink 外部函式");
            return;
        }

        var hp = FindObjectOfType<HP>();

        if (hp != null)
        {
            // Ink 呼叫：~ HP_Add(n)
            story.BindExternalFunction("HP_Add", (int amount) =>
            {
                hp.hp += amount;
                if (hp.hp < 0) hp.hp = 0; // 無上限，只保底 0
                Debug.Log($"❤️ HP 現在為：{hp.hp}");
            });

            // Ink 呼叫：~ HP_Set(n)
            story.BindExternalFunction("HP_Set", (int value) =>
            {
                hp.hp = value < 0 ? 0 : value;
                Debug.Log($"❤️ HP 設定為：{hp.hp}");
            });

            // Ink 呼叫：VAR cur = HP_Get()
            story.BindExternalFunction("HP_Get", () =>
            {
                return hp.hp;
            });

            Debug.Log("🩸 Ink 血量外部函式已綁定完成");
        }
        else
        {
            Debug.LogWarning("⚠️ 找不到 HP 物件，血量控制未綁定");
        }
    }

    public void ContinueStory()
    {
        if (story != null && story.canContinue)
        {
            // 1️⃣ 先從 Ink 拿出下一句台詞
            string text = story.Continue().Trim();
            dialogueText.text = text;

            // 🔹 檢查是否有 #play_music 標籤
            foreach (var tag in story.currentTags)
            {
                if (tag.StartsWith("play_music"))
                {
                    string[] parts = tag.Split(' ');
                    if (parts.Length > 1)
                    {
                        string musicName = parts[1];
                        Debug.Log($"🎵 偵測到音樂標籤：{musicName}");
                        PlayMusic(musicName);
                    }
                }
            }


            // 3️⃣ 抓說話者名字（如果 Ink 有設定變數 speaker）
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
            UpdatePortrait(speakerName);

            // 4️⃣ 如果有 CG TAG，播放影片
            if (story.currentTags.Contains("play_cg"))
            {
                Debug.Log("🎬 偵測到 #play_cg，播放開場影片");
                StartCoroutine(PlayCGThenContinue());
                return; // 暫停 Ink，等影片播完再繼續
            }

            // 5️⃣ 顯示選項
            DisplayChoices();
        }
        else
        {
            // 對話結束後的處理 ↓↓↓
            string currentPath = story.state.currentPathString;

            if (!string.IsNullOrEmpty(currentPath) && currentPath.Contains("boss_talk_first"))
            {
                Debug.Log("👁️ boss_talk_first 結束，顯示血量 UI");
                if (hpRef == null) hpRef = FindFirstObjectByType<HP>();
                if (hpRef != null)
                    hpRef.ShowHPUI(true);
            }

            if (story.currentTags.Contains("show_hp"))
            {
                if (hpRef == null) hpRef = FindFirstObjectByType<HP>();
                if (hpRef != null)
                    hpRef.ShowHPUI(true);
            }

            if (story.currentTags.Contains("jump_to_battle"))
            {
                Debug.Log("⚔️ Ink 觸發戰鬥場景切換！");
                if (leftCurtain != null && rightCurtain != null)
                {
                    StartCoroutine(CloseCurtainThenSwitchScene());
                }
                else
                {
                    SceneManager.LoadScene(battleSceneName);
                }
                return;
            }

            dialoguePanel.SetActive(false);
            choiceContainer.SetActive(false);
            dialogueIsPlaying = false;
            SetPlayerCanMove(true);
            dialogueEndTimer = dialogueEndCooldown;
            HidePortraits();

            onDialogueComplete?.Invoke();
            onDialogueComplete = null;
        }
    }


    private System.Collections.IEnumerator CloseCurtainThenSwitchScene()
    {
        if (!curtainInitialized) InitCurtain();

        Sequence seq = DOTween.Sequence();
        seq.Append(leftCurtain.DOAnchorPos(leftClosePos, curtainCloseDuration));
        seq.Join(rightCurtain.DOAnchorPos(rightClosePos, curtainCloseDuration));
        yield return seq.WaitForCompletion();

        yield return new WaitForSeconds(0.3f);
        SceneManager.LoadScene(battleSceneName);
    }

    void DisplayChoices()
    {
        // 🔹 Ink Tag 檢查：播放 CG
        if (story.currentTags.Contains("play_cg"))
        {
            Debug.Log("🎬 偵測到 #play_cg，播放開場影片");
            StartCoroutine(PlayCGThenContinue());
            return; // 暫停 Ink，等待影片播完再繼續
        }


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


    bool AllCluesCollected()
    {
        var clueData = FindObjectOfType<ClueData>();
        if (clueData == null)
        {
            Debug.LogWarning("⚠️ 沒有找到 ClueData，預設視為未收集完線索");
            return false;
        }

        if (tagClueIDs == null || tagClueIDs.Length == 0)
            return true;

        foreach (var id in tagClueIDs)
        {
            if (!clueData.HasClue(id))
            {
                Debug.Log($"❌ 缺少線索：{id}");
                return false;
            }
        }

        Debug.Log("✅ 全部線索已收集！");
        return true;
    }

    private IEnumerator PlayCGThenContinue()
    {
        dialoguePanel.SetActive(false);
        SetPlayerCanMove(false);

        GameObject cgPanel = GameObject.Find("CGPanel");
        if (cgPanel == null)
        {
            Debug.LogWarning("⚠️ 找不到 CGPanel，無法播放影片");
            yield break;
        }

        var video = cgPanel.GetComponent<UnityEngine.Video.VideoPlayer>();
        var raw = cgPanel.GetComponent<UnityEngine.UI.RawImage>();

        if (video == null)
        {
            Debug.LogWarning("⚠️ CGPanel 上沒有 VideoPlayer");
            yield break;
        }

        cgPanel.SetActive(true);

        // 🔹 確保 Canvas 顯示在最上層
        Canvas canvas = cgPanel.GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            canvas.sortingOrder = 999; // 確保在最上層
        }

        // 🔹 準備影片
        video.Prepare();
        while (!video.isPrepared)
        {
            yield return null;
        }

        Debug.Log("🎞 影片已準備完成");

        // 🔹 更新 RawImage 貼圖
        if (raw != null)
        {
            raw.texture = video.targetTexture;
            raw.color = Color.white;
            raw.enabled = true;
        }

        // 🔹 播放影片
        video.Play();
        Debug.Log("▶️ CG 開始播放");

        // 等影片真正開始
        yield return new WaitUntil(() => video.isPlaying);

        bool videoFinished = false;
        video.loopPointReached += (vp) => videoFinished = true;

        // 🔹 等待播放完成或跳過
        while (!videoFinished)
        {
            if (Input.anyKeyDown)
            {
                Debug.Log("⏭ 玩家跳過 CG");
                video.Stop();
                videoFinished = true;
            }
            yield return null;
        }

        Debug.Log("⏹ CG 播放完畢");

        // 停止影片並釋放 RenderTexture
        video.Stop();
        if (video.targetTexture != null)
            video.targetTexture.Release();

        cgPanel.SetActive(false);

        // ✅ 關鍵修正：讓 Ink 前進一行並觸發 Tag（例如 #play_music）
        if (story.canContinue)
        {
            Debug.Log("📖 CG 結束，繼續 Ink 劇情（應該跳到 == start ==）");
            ContinueStory();
        }
        else
        {
            Debug.LogWarning("⚠️ CG 結束後 Ink 無法繼續！");
        }

        dialoguePanel.SetActive(true);
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

    public void JumpToKnot(string knotName)
    {
        if (story == null) return;

        story.ChoosePathString(knotName);
        dialogueIsPlaying = true;
        dialoguePanel.SetActive(true);
        ContinueStory();
    }


}

[System.Serializable]
public class CharacterPortrait
{
    public string speakerName;
    public Sprite sprite;
    public PortraitPosition position;
}

public enum PortraitPosition
{
    Left,
    Right
}
