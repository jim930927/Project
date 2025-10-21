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

    [Header("存檔UI控制器")]
    public SaveUIManager saveUI;

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

    [Header("線索/道具")]
    public ClueData clueDatabase;
    public ItemData itemDatabase;
    public bool doorUnlocked = false;

    public bool justLoaded = false;  // ← 新增：判斷是否剛載入存檔
    private bool canAutoContinue = true; // ← 控制是否自動Continue


    public Story GetStory() => story;

    private Vector2 leftOriginPos;
    private Vector2 rightOriginPos;
    private bool curtainInitialized = false;

    public Story story;
    private bool canContinue = false;
    private float inputDelay = 0.5f;
    private float inputTimer = 0f;

    public bool dialogueIsPlaying { get; private set; }
    public bool IsInCooldown => dialogueEndTimer > 0f;

    private Action onDialogueComplete;

    private bool firstTagCheck = true; // 新增這個在 class 層級

    public static bool shouldAutoStartInk = true;  // 控制 Start() 是否自動啟動


    void Start()
    {
        dialoguePanel.SetActive(false);
        choiceContainer.SetActive(false);
        dialogueIsPlaying = false;

        HidePortraits();
        InitCurtain();

        // 自動啟動 Ink 劇本
        if (inkJSON != null && shouldAutoStartInk && !justLoaded)
        {
            Debug.Log("🎬 自動啟動 Ink 劇本，從 === CG === 開始");
            EnterDialogueMode(inkJSON, "CG");
        }
        else
        {
            Debug.Log("🟡 跳過自動啟動 Ink 劇本（因為是從存檔載入）");
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
        SetPlayerCanMove(false);
        if (justLoaded)
        {
            // 代表是從存檔載入的，不要重播開場 CG 或重新初始化 Ink
            Debug.Log("🟡 已從存檔載入，跳過自動對話初始化");
            justLoaded = false;
            return;
        }

        if (newInkJSON == null) return;

        if (story == null || inkJSON != newInkJSON)
        {
            inkJSON = newInkJSON;
            story = new Story(inkJSON.text);

            story.BindExternalFunction("SaveGame", () => {
                saveUI.OpenSaveMenu(story.state.ToJson());
            });

            // 建立 Story 後，同步 / 建立 Ink 內的 hp 變數（若不存在就設一個）
            if (hpRef == null) hpRef = FindFirstObjectByType<HP>();
            try
            {
                // 嘗試讀取，若讀不到會丟例外
                var maybe = story.variablesState["hp"];
            }
            catch
            {
                // hp 尚未宣告於 Ink，幫它建立（用目前 Unity 的 hp 值）
                if (hpRef != null)
                {
                    story.variablesState["hp"] = hpRef.hp;
                    Debug.Log($"🩸 為新 Story 建立 hp（由 Unity 同步）：{hpRef.hp}");
                }
                else
                {
                    // 若 Unity 也沒有 hp，給一個合理的預設（例如 3）
                    story.variablesState["hp"] = 4;
                    Debug.Log("🩸 為新 Story 建立 hp（預設為 3）");
                }
            }


            story.BindExternalFunction("ChangeBedImage", (string state) =>
            {
                var bed = GameObject.FindObjectOfType<BedController>();
                if (bed != null)
                    bed.ChangeImage(state);
            });

            story.BindExternalFunction("ChangeToiletImage", (string state) =>
            {
                var toilet = GameObject.FindObjectOfType<toiletController>();
                if (toilet != null)
                    toilet.ChangeImage(state);
            });

            story.BindExternalFunction("OpenChestUI", () =>
            {
                var chest = GameObject.FindObjectOfType<ChestController>();
                if (chest != null)
                {
                    chest.Interact();
                }
            });


            story.BindExternalFunction("MovePlayer", (string target) =>
            {
                var player = GameObject.FindWithTag("Player");
                if (player != null)
                {
                    var moveTarget = GameObject.Find(target);
                    if (moveTarget != null)
                    {
                        player.transform.position = moveTarget.transform.position;
                    }
                }
            });


            story.BindExternalFunction("SpawnObject", (string objName) =>
            {
                var bed = GameObject.FindObjectOfType<BedController>();
                if (bed != null)
                    bed.SpawnObject(objName);
            });


            story.BindExternalFunction("canStartBattle", () =>
            {
                // ✅ 改成只檢查特定線索
                return clueDatabase.HasCollectedClues("Letter", "Journal", "NPC_talk");
            });

            story.BindExternalFunction("CheckHasItem", (string itemId) => {
                return itemDatabase != null && itemDatabase.HasItem(itemId);
            });



            // ✅ 解鎖門
            story.BindExternalFunction("UnlockDoor", (string doorID) =>
            {
                if (!string.IsNullOrEmpty(doorID))
                {
                    DoorManager.Instance?.UnlockDoor(doorID);
                    Debug.Log($"🗝️ Ink 呼叫 UnlockDoor：{doorID}");
                }
            });

            // === 生成 NPC 外部函式 ===
            story.BindExternalFunction("SpawnNPC", (string npcName) =>
            {
                // 尋找場景中的出生點
                var spawnPoint = GameObject.Find($"{npcName}SpawnPoint");
                if (spawnPoint == null)
                {
                    Debug.LogWarning($"⚠️ 找不到 {npcName}SpawnPoint，NPC 將生成在 (0,0,0)");
                }

                // 從 Resources 載入 NPC prefab
                GameObject npcPrefab = Resources.Load<GameObject>($"NPCs/{npcName}");
                if (npcPrefab != null)
                {
                    Vector3 spawnPos = spawnPoint != null ? spawnPoint.transform.position : Vector3.zero;
                    GameObject npc = GameObject.Instantiate(npcPrefab, spawnPos, Quaternion.identity);
                    Debug.Log($"👤 已生成 NPC：{npcName}，位置：{spawnPos}");
                }
                else
                {
                    Debug.LogWarning($"⚠️ 無法找到 NPC Prefab：{npcName}（請放在 Resources/NPCs 下）");
                }
            });


            // ✅ 讓 Ink 能檢查目前持有的物品
            story.BindExternalFunction(
                "GetHeldItem", () =>
            {
                if (itemDatabase != null && itemDatabase.HasItem("key_room"))
                   return "key_room";
                if (itemDatabase != null && itemDatabase.HasItem("key_parent"))
                    return "key_parent";
                return "";
            });

            // 讓 Ink 設定門已開
            story.BindExternalFunction("SetDoorUnlocked", () => {
                doorUnlocked = true;
            });

            BindExternalBookFunctions(); // 🔹 綁定 Ink 外部函式
            story.ObserveVariable("hp", (string name, object value) =>
            {
                if (hpRef == null) hpRef = FindFirstObjectByType<HP>(); // 備援抓場上第一個 HP
                if (hpRef == null) return;
                hpRef.hp = Mathf.Max(0, System.Convert.ToInt32(value)); // 無上限，保底 0
            });

            // ✅ 改成一次性同步兩把鑰匙
            if (itemDatabase != null)
            {
                string have = "";
                if (itemDatabase.HasItem("key_parent"))
                    have = "key_parent";
                else if (itemDatabase.HasItem("key_room"))
                    have = "key_room";

                story.variablesState["have_items"] = have;
                Debug.Log($"🧩 已同步 have_items：{have}");
            }




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
            foreach (var tag in story.currentTags)
            {
                if (tag.StartsWith("play_cg"))
                {
                    string[] parts = tag.Split(' ');
                    string cgName = parts.Length > 1 ? parts[1] : "DefaultCG";
                    Debug.Log($"🎬 偵測到 #play_cg，播放影片：{cgName}");
                    StartCoroutine(PlayCGThenContinue(cgName));
                    return;
                }
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

            // 讀取 Ink 變數 UnlockDoor（安全寫法）
            object unlockObj = null;
            try
            {
                unlockObj = story.variablesState["Unlock_door"];
            }
            catch
            {
                unlockObj = null;
            }

            bool inkSaysUnlocked = false;
            if (unlockObj != null)
            {
                // Ink 可能回傳 bool、int、string 等，先嘗試轉 bool
                if (unlockObj is bool)
                {
                    inkSaysUnlocked = (bool)unlockObj;
                }
                else
                {
                    bool parsed;
                    if (bool.TryParse(unlockObj.ToString(), out parsed))
                        inkSaysUnlocked = parsed;
                    else
                    {
                        // 若 Ink 用 0/1 表示，也可嘗試轉 int
                        int intVal;
                        if (int.TryParse(unlockObj.ToString(), out intVal))
                            inkSaysUnlocked = (intVal != 0);
                    }
                }
            }

            if (inkSaysUnlocked)
            {
                doorUnlocked = true; // 你的 InkDialogueManager 層級旗標
                Debug.Log("🚪 Ink 變數 UnlockDoor 為 true，門已解鎖");
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

    public void UpdatePortrait(string speakerName)
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

    public void HidePortraits()
    {
        if (leftPortraitImage != null) leftPortraitImage.gameObject.SetActive(false);
        if (rightPortraitImage != null) rightPortraitImage.gameObject.SetActive(false);
    }

    public void ShowPortraits()
    {
        if (leftPortraitImage != null) leftPortraitImage.gameObject.SetActive(true);
        if (rightPortraitImage != null) rightPortraitImage.gameObject.SetActive(true);
    }

    public void ResetPortraits()
    {
        if (leftPortraitImage != null) leftPortraitImage.sprite = leftDefaultPortrait;
        if (rightPortraitImage != null) rightPortraitImage.sprite = rightDefaultPortrait;
    }

    public void ForceEndDialogue()
    {
        dialoguePanel.SetActive(false);
        choiceContainer.SetActive(false);
        HidePortraits();
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
            if (Input.anyKeyDown)
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

    public void SyncHaveItemsToInk()
    {
        if (story == null)
        {
            Debug.LogWarning("⚠️ SyncHaveItemsToInk: story 尚未建立");
            return;
        }

        if (itemDatabase == null)
        {
            Debug.LogWarning("⚠️ SyncHaveItemsToInk: itemDatabase 未指派");
            return;
        }

        try
        {
            // ✅ 改成一次性同步兩把鑰匙
            if (itemDatabase != null)
            {
                string have = "";
                if (itemDatabase.HasItem("key_parent"))
                    have = "key_parent";
                else if (itemDatabase.HasItem("key_room"))
                    have = "key_room";

                story.variablesState["have_items"] = have;
                Debug.Log($"🧩 已同步 have_items：{have}");
            }

            Debug.Log($"🧩 已同步 have_items: {story.variablesState["have_items"]}");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("⚠️ 更新 Ink 變數 have_items 發生錯誤: " + e.Message);
        }
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
