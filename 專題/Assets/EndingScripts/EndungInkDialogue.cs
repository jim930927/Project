using DG.Tweening;
using Ink.Runtime;
using NUnit.Framework.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.U2D;
using UnityEngine.UI;
using static ClueData;
using static FinalInkDialogue;
using static UnityEngine.EventSystems.EventTrigger;

public class EndingInkDialogue : MonoBehaviour
{
    [Header("UI 元件")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI dialogueText;
    public GameObject dialoguePanel;

    [Header("Ink 劇本")]
    public TextAsset inkJSON;

    [Header("對話緩衝")]
    public float dialogueEndCooldown = 0.3f;
    private float dialogueEndTimer = 0f;

    [Header("布幕設定（只關閉時使用）")]
    public RectTransform leftCurtain;
    public RectTransform rightCurtain;
    public Vector2 leftClosePos = new Vector2(0, 0);
    public Vector2 rightClosePos = new Vector2(0, 0);
    public float curtainCloseDuration = 1.2f;
    public string SceneName = "MainMenu";
    public GameObject fullblackScreen;
    public CanvasGroup blackScreenCanvasGroup; // 黑幕
    public GameObject endblackScreen;

    public bool justLoaded = false;  // ← 新增：判斷是否剛載入存檔
    private bool canAutoContinue = true; // ← 控制是否自動Continue

    public Transform player;
    public Transform player_fake;
    public Transform Hospital;
    public Transform School;
    public Transform Home;
    public Transform BackMountain;
    public Transform Theater;
    public Transform monster;

    public GameObject crack;

    public GameObject talismanFX;

    public GameObject classmate;

    [Header("角色立繪區域（Main / Guide / Him）")]
    public Image mainPortraitImage;
    public Image guidePortraitImage;
    public Image himPortraitImage;

    public Sprite mainDefaultPortrait;
    public Sprite guideDefaultPortrait;
    public Sprite himDefaultPortrait;

    public Story GetStory() => story;
    private bool curtainInitialized = false;

    public Story story;
    private bool canContinue = false;
    private float inputDelay = 0.5f;
    private float inputTimer = 0f;

    private bool isShowingChoices = false;
    private bool skipLocked = false;

    public SpriteRenderer spriteRenderer1;
    public SpriteRenderer spriteRenderer2;
    public GameObject Him;

    public CanvasGroup whiteScreenCanvasGroup;


    public bool dialogueIsPlaying { get; private set; }
    public bool IsInCooldown => dialogueEndTimer > 0f;

    private Action onDialogueComplete;

    private bool firstTagCheck = true;

    public static bool shouldAutoStartInk = true;  


    private void Start()
    {
        if (LoadUIManager.pendingLoadData != null)
        {
            Debug.Log("🟡 檢測到待載入存檔，暫停自動初始化 Ink 劇情");
            justLoaded = true;
            shouldAutoStartInk = false;

            // 關閉所有對話 UI，避免擋住互動
            dialoguePanel?.SetActive(false);
        }
 

        // 🔸 原本的初始化流程
        if (inkJSON != null && shouldAutoStartInk && !justLoaded)
        {
            EnterDialogueMode(inkJSON, "CG");
        }
        else
        {
            Debug.Log("🟢 InkDialogueManager 等待存檔資料載入");
            StartCoroutine(LoadUIManager.ApplyPendingLoadData()); // 由這裡繼續載入
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

        // 🛡️ 若正在顯示選項，吃掉空白鍵輸入
        if (isShowingChoices)
        {
            if (Input.GetKeyDown(KeyCode.Space))
                Debug.Log("🛑 空白鍵被吃掉（選項中）");
            return;
        }

        if (!canContinue)
        {
            inputTimer += Time.deltaTime;
            if (inputTimer >= inputDelay)
                canContinue = true;
            return;
        }

        // ✅ 僅當沒有選項時允許繼續
        if (Input.GetKeyDown(KeyCode.Space) && story.currentChoices.Count == 0 && canContinue && !skipLocked)
        {
            skipLocked = true;
            StartCoroutine(SafeContinue());
        }
    }


    private IEnumerator SafeContinue()
    {
        canContinue = false;
        yield return new WaitForSeconds(0.05f);
        ContinueStory();
        yield return new WaitForSeconds(0.15f);
        skipLocked = false;
    }


    IEnumerator LockInputTemporarily(float duration)
    {
        canContinue = false;
        skipLocked = true;
        yield return new WaitForSeconds(duration);
        canContinue = true;
        skipLocked = false;
    }

    void InitCurtain()
    {
        if (leftCurtain != null && rightCurtain != null && !curtainInitialized)
        {
            curtainInitialized = true;
        }
    }

    public void EnterDialogueMode(TextAsset newInkJSON, string knotName = "", Action onComplete = null)
    {
        SetPlayerCanMove(false);
        if (justLoaded)
        {
            Debug.Log("🟡 已從存檔載入，跳過開場 CG，但仍恢復對話介面");
            dialoguePanel.SetActive(true);
            dialogueIsPlaying = true;
            canContinue = true;
            justLoaded = false;
            return;
        }


        if (newInkJSON == null) return;

        if (story == null || inkJSON != newInkJSON)
        {
            inkJSON = newInkJSON;
            story = new Story(inkJSON.text);

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

            if (!string.IsNullOrEmpty(knotName))
            {
                try { story.ChoosePathString(knotName); } catch { }
            }

            onDialogueComplete = onComplete;

            dialoguePanel.SetActive(true);
            dialogueIsPlaying = true;
            canContinue = false;
            inputTimer = 0f;
            ContinueStory();

            ShowPortraits();
            ResetPortraits();
        }
    }

    public void BindAllExternalFunctions()
    {
        if (story == null) return;

        // ✅ 安全綁定：避免重複綁定時發生例外
        void SafeBind(string name, System.Action action)
        {
            try
            {
                story.BindExternalFunction(name, action);
            }
            catch (System.Exception e)
            {
                if (!e.Message.Contains("already been bound"))
                    Debug.LogWarning($"⚠️ 綁定 {name} 失敗：{e.Message}");
                else
                    Debug.Log($"🔁 函式 {name} 已存在，略過綁定。");
            }
        }

        void SafeBindString(string name, System.Action<string> action)
        {
            try
            {
                story.BindExternalFunction(name, action);
            }
            catch (System.Exception e)
            {
                if (!e.Message.Contains("already been bound"))
                    Debug.LogWarning($"⚠️ 綁定 {name} 失敗：{e.Message}");
                else
                    Debug.Log($"🔁 函式 {name} 已存在，略過綁定。");
            }
        }
    }


    public void ContinueStory()
    {
        if (isShowingChoices) return;

        if (story != null && story.canContinue)
        {
            SetPlayerCanMove(false);

            // 1️⃣ 先從 Ink 拿出下一句台詞
            string text = story.Continue().Trim();

            List<string> tags = new List<string>(story.currentTags);
            bool onlyHasControlTags = tags.Count > 0 && string.IsNullOrEmpty(text);

            HandleTags(tags);

            if (onlyHasControlTags && story.canContinue)
            {
                Debug.Log("⏭ 自動略過控制用 tag 輪（沒有文字）");
                ContinueStory();
                return;
            }

            if (string.IsNullOrEmpty(text))
            {
                // 🔹 特殊情況：如果這輪只有 tag，仍繼續下一句
                if (story.canContinue)
                {
                    Debug.Log("⏭ 跳過空白句（僅 tag 存在）");
                    ContinueStory(); // 遞迴繼續下一句
                    return;
                }
            }

            dialogueText.text = text;


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

            foreach (var tag in tags)
            {
                if (tag.StartsWith("play_music"))
                {
                    string[] parts = tag.Split(' ');
                    if (parts.Length > 1)
                    {
                        string musicName = parts[1];
                        Debug.Log($"🎵 播放音樂：{musicName}");
                        PlayMusic(musicName);
                    }
                }

            }


            // 4️⃣ 如果有 CG TAG，播放影片
            foreach (var tag in tags)
            {
                if (tag.StartsWith("play_cg"))
                {
                    string[] parts = tag.Split(' ');
                    string cgName = parts.Length > 1 ? parts[1] : "DefaultCG";
                    StartCoroutine(PlayCGThenContinue(cgName));
                    return; // 暫停 Ink，等 CG 播完再繼續
                }
            }


            // 5️⃣ 顯示選項
            HandleTags(story.currentTags);

            if (string.IsNullOrEmpty(text))
            {
                // 若有應自動略過的tag（例如memory結束、start_chase等）
                if (tags.Count > 0)
                {
                    ContinueStory(); // 自動繼續下一輪，不顯示對話框
                    return;
                }
            }

        }
        else
        {
            SetPlayerCanMove(true);
            // 對話結束後的處理 ↓↓↓
            string currentPath = story.state.currentPathString;

            dialoguePanel.SetActive(false);
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
        SceneManager.LoadScene(SceneName);
    }


    public void UpdatePortrait(string speakerName)
    {
        // 先重置
        mainPortraitImage.sprite = mainDefaultPortrait;
        guidePortraitImage.sprite = guideDefaultPortrait;
        himPortraitImage.sprite = himDefaultPortrait;

        foreach (var entry in portraits)
        {
            if (entry.speakerName == speakerName)
            {
                switch (entry.slot)
                {
                    case ENDPortraitSlot.Main:
                        mainPortraitImage.sprite = entry.sprite;
                        break;
                    case ENDPortraitSlot.Guide:
                        guidePortraitImage.sprite = entry.sprite;
                        break;
                    case ENDPortraitSlot.Him:
                        himPortraitImage.sprite = entry.sprite;
                        break;
                }
                return;
            }
        }
    }

    public void HidePortraits()
    {
        if (mainPortraitImage != null) mainPortraitImage.gameObject.SetActive(false);
        if (guidePortraitImage != null) guidePortraitImage.gameObject.SetActive(false);
        if (himPortraitImage != null) himPortraitImage.gameObject.SetActive(false);
    }

    public void ShowPortraits()
    {
        if (mainPortraitImage != null) mainPortraitImage.gameObject.SetActive(true);
        if (guidePortraitImage != null) guidePortraitImage.gameObject.SetActive(true);
        if (himPortraitImage != null) himPortraitImage.gameObject.SetActive(true);
    }


    public void ResetPortraits()
    {
        if (mainPortraitImage != null) mainPortraitImage.sprite = mainDefaultPortrait;
        if (guidePortraitImage != null) guidePortraitImage.sprite = guideDefaultPortrait;
        if (himPortraitImage != null) himPortraitImage.sprite = himDefaultPortrait;
    }



    private IEnumerator ContinueAfterChoice()
    {
        yield return new WaitForEndOfFrame(); // 稍微等一幀，讓 Ink 更新完成
        ContinueStory();
    }


    public void ForceEndDialogue()
    {
        dialoguePanel.SetActive(false);
        SetPlayerCanMove(true);
        dialogueIsPlaying = false;
        Debug.Log("🟢 對話強制結束");
    }

    public void SetPlayerCanMove(bool canMove)
    {
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            var pm = player.GetComponent<Player>();
            if (pm != null)
                pm.canMove = canMove;
        }
    }

    private IEnumerator PlayCGThenContinue(string cgName = "DefaultCG")
    {
        dialoguePanel.SetActive(false);
        SetPlayerCanMove(false);

        GameObject cgPanel = GameObject.Find("CGPanel");
        if (cgPanel == null)
        {
            Debug.LogWarning("⚠️ 找不到 CGPanel，無法播放影片");
            yield break;
        }

        // 取得所有 RawImage，準備控制顯示
        var raws = cgPanel.GetComponentsInChildren<UnityEngine.UI.RawImage>(true);
        foreach (var r in raws) r.gameObject.SetActive(false);

        // 根據名稱找要顯示的那個 RawImage
        UnityEngine.UI.RawImage targetRaw = null;
        foreach (var r in raws)
        {
            if (r.name.Equals(cgName, StringComparison.OrdinalIgnoreCase) ||
                r.name.Contains(cgName, StringComparison.OrdinalIgnoreCase))
            {
                targetRaw = r;
                break;
            }
        }

        // 找不到特定名稱時就用第一個 RawImage
        if (targetRaw == null && raws.Length > 0)
            targetRaw = raws[0];

        if (targetRaw == null)
        {
            Debug.LogWarning($"⚠️ 找不到對應的 RawImage 來播放 CG：{cgName}");
            yield break;
        }

        // 啟用目標畫面
        targetRaw.gameObject.SetActive(true);
        Canvas canvas = cgPanel.GetComponentInParent<Canvas>();
        if (canvas != null) canvas.sortingOrder = 999;

        // 找 VideoPlayer（可掛在父物件或 RawImage 上）
        var video = cgPanel.GetComponent<UnityEngine.Video.VideoPlayer>();
        if (video == null)
            video = targetRaw.GetComponent<UnityEngine.Video.VideoPlayer>();

        if (video == null)
        {
            Debug.LogWarning("⚠️ 找不到 VideoPlayer，無法播放影片");
            yield break;
        }

        // 載入影片
        var clip = Resources.Load<UnityEngine.Video.VideoClip>($"CG/{cgName}");
        Debug.Log($"🧩 嘗試載入影片：Resources/CG/{cgName}.mp4，結果：{clip}");

        if (clip != null)
        {
            video.source = UnityEngine.Video.VideoSource.VideoClip;
            video.clip = clip;
            Debug.Log($"🎞 已載入影片：{clip.name}");
        }
        else
        {
            Debug.LogWarning($"⚠️ 找不到影片：Resources/CG/{cgName}.mp4，改用原本 clip");
        }

        // 預備播放
        video.Prepare();
        int waitCount = 0;
        while (!video.isPrepared)
        {
            yield return null;
            waitCount++;
            if (waitCount > 300) // 約 5 秒
            {
                Debug.LogWarning("⚠️ 等待影片準備超時");
                yield break;
            }
        }

        Debug.Log("🎬 影片準備完成，開始播放");

        if (targetRaw != null)
        {
            targetRaw.texture = video.targetTexture;
            targetRaw.color = Color.white;
            targetRaw.enabled = true;
        }

        video.Play();
        Debug.Log($"▶️ 播放 CG：{cgName}");

        bool videoFinished = false;
        video.loopPointReached += (vp) => videoFinished = true;

        while (!videoFinished)
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                Debug.Log("⏭ 玩家跳過 CG");
                video.Stop();
                videoFinished = true;
            }
            yield return null;
        }

        Debug.Log("⏹ CG 播放結束");

        video.Stop();
        if (video.targetTexture != null)
            video.targetTexture.Release();

        // 關閉所有 CG 畫面
        foreach (var r in raws) r.gameObject.SetActive(false);

        // 還原對話
        if (story.canContinue)
        {
            Debug.Log($"📖 CG ({cgName}) 結束，繼續 Ink 劇情");
            ContinueStory();
        }
        else
        {
            Debug.LogWarning($"⚠️ CG ({cgName}) 結束後 Ink 無法繼續！");
        }

        dialoguePanel.SetActive(true);
        SetPlayerCanMove(true);
    }

    // -----------------------------
    // 解析 Ink 標籤觸發事件
    // -----------------------------
    private void HandleTags(List<string> currentTags)
    {
        bool shouldAutoContinue = false; // 🟩 檢查是否要自動略過空白輪

        foreach (string tag in currentTags)
        {
            switch (tag)
            {
                case "MainMeun":
                    Debug.Log("MainMenu");
                    SceneManager.LoadScene("MainMenu");
                    break;
                case "black_screen":
                    fullblackScreen.SetActive(true);
                    break;
                case "black_end":
                    endblackScreen.SetActive(true);
                    break;
                case "back_screen":
                    fullblackScreen.SetActive(false);
                    break;
                case "pause_music":
                    {
                        var bgm = FindObjectOfType<BGMManager>();
                        if (bgm != null)
                        {
                            bgm.PauseMusic(); // ← 新增這個方法（見下方）
                            Debug.Log("🎵 Ink 標籤：#pause_music → 暫停音樂");
                        }
                        else
                        {
                            Debug.LogWarning("⚠️ 找不到 BGMManager，無法暫停音樂");
                        }
                        break;
                    }
                // 🟩 繼續播放音樂
                case "keep_music":
                    {
                        var bgm = FindObjectOfType<BGMManager>();
                        if (bgm != null)
                        {
                            bgm.ResumeMusic(); // ← 新增這個方法（見下方）
                            Debug.Log("🎵 Ink 標籤：#keep_music → 繼續播放音樂");
                        }
                        else
                        {
                            Debug.LogWarning("⚠️ 找不到 BGMManager，無法繼續音樂");
                        }
                    }
                    break ;
                case "Hospital":
                    player.position = Hospital.position;
                    break;
                case "classmate":
                    classmate.SetActive(true);
                    break;
                case "turnleft":
                    {
                        Debug.Log("📸 Exposure 觸發 → 換立繪（Main）");
                        Sprite newSprite5 = Resources.Load<Sprite>("NPC/Turn_left");
                        if (spriteRenderer1 != null)
                            spriteRenderer1.sprite = newSprite5;
                        else
                            Debug.LogWarning("⚠ 找不到 Resources/NPC/exposure_pose.png");

                        break;
                    }
                case "School":
                    player.position = School.position;
                    break;
                case "BackMountain":
                    player.position = BackMountain.position;
                    break;
                case "Home":
                    player.position = Home.position;
                    break;
                case "Theater":
                    player.position = Theater.position;
                    break;
                case "crack_appear":
                    crack.SetActive(true);
                    break;
                case "crack_disappear":
                    crack.SetActive(false);
                    break;
                case "awake":
                    {
                        Debug.Log("📸 Exposure 觸發 → 換立繪（Main）");
                        Sprite newSprite4 = Resources.Load<Sprite>("NPC/Face_Guide");
                        if (spriteRenderer2 != null)
                            spriteRenderer2.sprite = newSprite4;
                        else
                            Debug.LogWarning("⚠ 找不到 Resources/NPC/exposure_pose.png");

                        break;
                    }

                case "lift_player":
                    {
                        Debug.Log("🔺 觸發：角色被拉起");

                        if (player_fake != null)
                        {
                            // 向上拉 2 單位
                            player_fake.DOMoveY(player_fake.position.y + 1f, 0.8f)
                                .SetEase(Ease.OutCubic);
                        }
                        break;
                    }

                case "push_enemy":
                    {
                        Debug.Log("💥 敵人被震退");

                        if (monster != null)
                        {
                            monster.DOMove(monster.position + new Vector3(0, 1.0f, 0), 0.3f)
                                   .SetEase(Ease.OutExpo);
                        }
                        break;
                    }

                case "flash_white":
                    {
                        Debug.Log("⚡ 觸發閃白特效");
                        StartCoroutine(FlashWhiteEffect());
                        break;
                    }

                case "enemy_disappear":
                    var enemy = GameObject.FindWithTag("Boss");
                    var sprite = enemy.GetComponent<SpriteRenderer>();
                    sprite.DOFade(0f, 0.5f).OnComplete(() => enemy.SetActive(false));
                    break;

                
            }
        }

    }

    IEnumerator DisableAfterSeconds(GameObject obj, float sec)
    {
        yield return new WaitForSeconds(sec);
        obj.SetActive(false);
    }


    private IEnumerator FlashWhiteEffect()
    {
        if (whiteScreenCanvasGroup == null)
        {
            Debug.LogWarning("⚠️ whiteScreenCanvasGroup 未指定");
            yield break;
        }

        // 0.15 秒快速亮到全白
        whiteScreenCanvasGroup.alpha = 0;
        whiteScreenCanvasGroup.gameObject.SetActive(true);

        float durationIn = 0.15f;
        float timer = 0;

        while (timer < durationIn)
        {
            timer += Time.deltaTime;
            whiteScreenCanvasGroup.alpha = Mathf.Lerp(0, 1, timer / durationIn);
            yield return null;
        }

        // 保持全白 0.05 秒
        yield return new WaitForSeconds(0.05f);

        // 0.3 秒淡回正常畫面
        float durationOut = 0.3f;
        timer = 0;

        while (timer < durationOut)
        {
            timer += Time.deltaTime;
            whiteScreenCanvasGroup.alpha = Mathf.Lerp(1, 0, timer / durationOut);
            yield return null;
        }

        whiteScreenCanvasGroup.alpha = 0;
        whiteScreenCanvasGroup.gameObject.SetActive(false);
    }

    private void TurnPlayerLeft()
    {
        if (player == null) return;

        player.GetComponent<Animator>().SetFloat("LastX", -1);

        Debug.Log("Player turned back (rotated 180 degrees)");
    }

    public void ReloadInkState(string jsonState)
    {
        Debug.Log("🔄 重新載入 Ink 劇情狀態...");

        if (inkJSON == null)
        {
            Debug.LogWarning("⚠️ inkJSON 未指定，無法重建 Ink Story");
            return;
        }

        if (story == null)
            story = new Ink.Runtime.Story(inkJSON.text);

        // 載入 Ink 狀態
        story.state.LoadJson(jsonState);

        BindAllExternalFunctions();

        // 僅恢復 UI 狀態，不主動顯示文字
        dialoguePanel.SetActive(false);
        dialogueIsPlaying = false;
        canContinue = false;
        justLoaded = false;

        Debug.Log("✅ Ink 劇情已完全恢復（等待玩家互動觸發對話）");
    }

    // -----------------------------
    // 黑幕淡入淡出控制
    // -----------------------------
    IEnumerator FadeScreen(bool fadeIn)
    {
        float duration = 1f;
        float elapsed = 0f;
        float start = fadeIn ? 0 : 1;
        float end = fadeIn ? 1 : 0;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            blackScreenCanvasGroup.alpha = Mathf.Lerp(start, end, elapsed / duration);
            yield return null;
        }
    }
    private void PlayMusic(string musicName)
    {
        var bgmManager = FindObjectOfType<BGMManager>();
        if (bgmManager != null)
        {
            bgmManager.PlayMusic(musicName);
            Debug.LogWarning("播放音樂：" + musicName);
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

    public ENDPortraitEntry[] portraits;
}

[System.Serializable]
public class ENDPortraitEntry
{
    public string speakerName;
    public Sprite sprite;
    public ENDPortraitSlot slot;
}


public enum ENDPortraitSlot
{
    Main,
    Guide,
    Him
}
