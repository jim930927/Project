using DG.Tweening;
using Ink.Runtime;
using NUnit.Framework.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static ClueData;
using static TMPro.SpriteAssetUtilities.TexturePacker_JsonArray;
using static UnityEngine.EventSystems.EventTrigger;

public class FinalInkDialogue : MonoBehaviour
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

    [Header("角色立繪區域（Main / Guide / Him）")]
    public Image mainPortraitImage;
    public Image guidePortraitImage;
    public Image himPortraitImage;

    public Sprite mainDefaultPortrait;
    public Sprite guideDefaultPortrait;
    public Sprite himDefaultPortrait;

    [Header("對話緩衝")]
    public float dialogueEndCooldown = 0.3f;
    private float dialogueEndTimer = 0f;

    [Header("布幕設定（只關閉時使用）")]
    public RectTransform leftCurtain;
    public RectTransform rightCurtain;
    public Vector2 leftClosePos = new Vector2(0, 0);
    public Vector2 rightClosePos = new Vector2(0, 0);
    public float curtainCloseDuration = 1.2f;
    public string battleSceneName = "BattleScene";
    public GameObject fullblackScreen;
    public CanvasGroup blackScreenCanvasGroup; // 黑幕

    [Header("對應線索 ID")]
    public string[] tagClueIDs;

    [Header("線索/道具")]
    public ClueData clueDatabase;
    public ItemData itemDatabase;
    public FinalBookUIManager bookUIManager;
    public bool doorUnlocked = false;

    [Header("記憶碎片資料庫")]
    public MemoryFragmentData memoryFragmentDatabase;


    [System.Serializable]
    public class EndingLabel
    {
        public string tagName;   // 例如 "GameOver1"
        public string text;      // 顯示在黑幕上的字，例如 "True End"
    }

    public EndingLabel[] endings;          // 可在 Inspector 填多個
    public TextMeshProUGUI endingTextUI;   // 指到UI(TextMeshProUGUI)

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

    private bool isShowingChoices = false;
    private bool skipLocked = false;
    private bool choiceCooldown = false; // 🔥 新增：防止剛出現選項時空白鍵誤觸

    public SpriteRenderer spriteRenderer;
    public GameObject Him;

    public bool dialogueIsPlaying { get; private set; }
    public bool IsInCooldown => dialogueEndTimer > 0f;

    private Action onDialogueComplete;

    private bool firstTagCheck = true; // 新增這個在 class 層級

    public static bool shouldAutoStartInk = true;  // 控制 Start() 是否自動啟動


    private void Start()
    {
        if (LoadUIManager.pendingLoadData != null)
        {
            Debug.Log("🟡 檢測到待載入存檔，暫停自動初始化 Ink 劇情");
            justLoaded = true;
            shouldAutoStartInk = false;

            // 關閉所有對話 UI，避免擋住互動
            dialoguePanel?.SetActive(false);
            choiceContainer?.SetActive(false);
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

            if (v == null)
            {
                Debug.LogWarning("⚠️ Ink 變數 'hp' 不存在，使用現有 HP");
                story.variablesState["hp"] = hpRef.hp;
                return;
            }

            try
            {
                int parsed = 0;

                // 🧩 根據型態轉換
                if (v is int)
                {
                    parsed = (int)v;
                }
                else if (v is float)
                {
                    parsed = Mathf.RoundToInt((float)v);
                }
                else if (v is double)
                {
                    parsed = Mathf.RoundToInt((float)(double)v);
                }
                else if (v is string)
                {
                    int.TryParse((string)v, out parsed);
                }

                hpRef.hp = Mathf.Max(0, parsed);
                Debug.Log($"❤️ 從 Ink 同步 HP：{hpRef.hp}");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"⚠️ 無法解析 Ink 變數 'hp'：{v} ({e.Message})，保持原本 HP");
            }
        }
    }

    public void EnterDialogueMode(TextAsset newInkJSON, string knotName = "", Action onComplete = null)
    {
        SetPlayerCanMove(false);
        if (justLoaded)
        {
            Debug.Log("🟡 已從存檔載入，跳過開場 CG，但仍恢復對話介面");
            dialoguePanel.SetActive(true);
            choiceContainer.SetActive(false);
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

            BindExternalBookFunctions(); // 🔹 綁定 Ink 外部函式
            story.ObserveVariable("hp", (string name, object value) =>
            {
                if (hpRef == null) hpRef = FindFirstObjectByType<HP>(); // 備援抓場上第一個 HP
                if (hpRef == null) return;
                hpRef.hp = Mathf.Max(0, System.Convert.ToInt32(value)); // 無上限，保底 0
            });


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
                    story.variablesState["hp"] = 3;
                    Debug.Log("🩸 為新 Story 建立 hp（預設為 3）");
                }
            }

            story.BindExternalFunction("UnlockDoor", (string doorID) =>
            {
                if (!string.IsNullOrEmpty(doorID))
                {
                    DoorManager.Instance?.UnlockDoor(doorID);
                    Debug.Log($"🗝️ Ink 呼叫 UnlockDoor：{doorID}");
                }
            });

            story.BindExternalFunction("OpenChestUI", () =>
            {
                var chest = GameObject.FindObjectOfType<FinalChestController>();
                if (chest != null)
                {
                    chest.Interact();
                }
            });

            story.BindExternalFunction(
                "GetHeldItem", () =>
                {
                    if (itemDatabase != null && itemDatabase.HasItem("recorder"))
                        return "recorder";
                    return "";
                });

            // 讓 Ink 設定門已開
            story.BindExternalFunction("SetDoorUnlocked", () => {
                doorUnlocked = true;
            });

           

            if (itemDatabase != null)
            {
                string have = "";
                if (itemDatabase.HasItem("recorder"))
                    have = "recorder";

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
    public void BindExternalBookFunctions()
    {

        story.BindExternalFunction("Get_Item", (string itemID) =>
        {
            var clueIDB = itemDatabase;
            var bookUI = bookUIManager;

            clueIDB.AddItem(itemID);

            bookUI.OpenItemOverlay(itemID);

            // ✅ 嘗試從 Resources/Clues/ 載入對應圖片
            var image = Resources.Load<Sprite>($"Clues/{itemID}");
            if (image != null && PreviewImageManager.Instance != null)
            {
                Debug.Log($"🖼️ 顯示線索圖片：{itemID}");
                PreviewImageManager.Instance.ShowImage(image);
            }
            else
            {
                Debug.LogWarning($"⚠️ 找不到圖片：Resources/Clues/{itemID}.png 或 PreviewImageManager 未初始化");
            }

            Debug.Log($"📘 Ink 觸發撿取道具：{itemID}");
        });


        // 🟩 Ink 呼叫：~ Get_Clue("Journal3")（撿取線索並顯示）
        story.BindExternalFunction("Get_Clue", (string clueID) =>
        {
            Debug.Log($"🧩 Ink 呼叫 Get_Clue：{clueID}");
            var clueIDB = clueDatabase;
            var bookUI = bookUIManager;

            // ✅ 加入線索到資料庫
            clueIDB.AddClue(clueID);

            // ✅ 顯示線索內容（不開整本書）
            bookUI.OpenClueOverlay(clueID);

            // ✅ 嘗試從 Resources/Clues/ 載入對應圖片
            var image = Resources.Load<Sprite>($"Clues/{clueID}");
            if (image != null && PreviewImageManager.Instance != null)
            {
                Debug.Log($"🖼️ 顯示線索圖片：{clueID}");
                PreviewImageManager.Instance.ShowImage(image);
            }
            else
            {
                Debug.LogWarning($"⚠️ 找不到圖片：Resources/Clues/{clueID}.png 或 PreviewImageManager 未初始化");
            }

            Debug.Log($"📘 Ink 觸發撿取線索：{clueID}");
        });



        story.BindExternalFunction("Get_fragments", (string fragID) =>
        {
            var fragDB = memoryFragmentDatabase; // 你等下要在 Inspector 綁上 ScriptableObject
            fragDB.AddFragment(fragID);

            int count = fragDB.GetCollectedCount();
            Debug.Log($"🧩 當前記憶碎片數量：{count}/8");

            if (count == 4)
            {
                HP hpSystem = FindObjectOfType<HP>();
                if (hpSystem != null)
                {
                    hpSystem.hp += 1;
                    Debug.Log("💖 記憶碎片達到4個，血量 +1！");
                }
            }

            if (count == 8)
            {
                HP hpSystem = FindObjectOfType<HP>();
                if (hpSystem != null)
                {
                    hpSystem.hp += 1;
                    Debug.Log("💖 記憶碎片達到8個，血量 +1！");
                }
            }
        });



        story.BindExternalFunction("ReplaceItem", (string oldItemID, string newClueID) =>
        {
            if (itemDatabase == null || bookUIManager == null)
            {
                Debug.LogWarning("⚠️ ReplaceItem: 缺少 itemData 或 bookUIManager 引用");
                return;
            }

            // 移除舊道具
            itemDatabase.RemoveItem(oldItemID);

            // 新增新道具
            clueDatabase.AddClue(newClueID);

            // 立即顯示新道具的內容（不開整本書）
            var image = Resources.Load<Sprite>($"Clues/{newClueID}");
            bookUIManager.OpenClueOverlay(newClueID);
            PreviewImageManager.Instance.ShowImage(image);

            Debug.Log($"🔄 道具已替換：{oldItemID} → {newClueID}");
        });

        story.BindExternalFunction("Use_Item", (string itemID) =>
        {
            Debug.Log($"🧭 Ink 呼叫 Use_Item：{itemID}");

            if (itemDatabase == null)
            {
                Debug.LogWarning("⚠️ Use_Item: 缺少 itemDatabase 參考");
                return;
            }

            var item = itemDatabase.items.Find(i => i.id == itemID);
            if (item != null)
            {
                item.collected = false;
                Debug.Log($"🪞 道具 {itemID} 已標記為未收集（使用掉）");
            }
            else
            {
                Debug.LogWarning($"⚠️ 找不到道具：{itemID}");
            }

        });


        if (story == null) return;

        var bookUI = FindObjectOfType<BookUIManager>();
        if (bookUI == null)
        {
            Debug.LogWarning("⚠️ 找不到 BookUIManager，無法綁定 Ink 外部函式");
            return;
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


        // === Ink 外部函式綁定 ===

        SafeBind("SaveGame", () => {
            saveUI.OpenSaveMenu(story.state.ToJson());
        });


        SafeBindString("UnlockDoor", (string doorID) =>
        {
            if (!string.IsNullOrEmpty(doorID))
            {
                DoorManager.Instance?.UnlockDoor(doorID);
                Debug.Log($"🗝️ Ink 呼叫 UnlockDoor：{doorID}");
            }
        });

        SafeBind("OpenChestUI", () =>
        {
            var chest = GameObject.FindObjectOfType<FinalChestController>();
            if (chest != null)
                chest.Interact();
        });


        // === 重新綁定書籍、物品、血量函式 ===
        BindExternalBookFunctions();
        SyncHpFromInk();
        SyncHaveItemsToInk();

        Debug.Log("🔗 所有 Ink 外部函式已安全綁定完成");
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
            DisplayChoices();
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

            // 🔒 避免按鍵還在按著 → 自動 continue / 自動選項 / 誤觸 UI
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(null);

            Input.ResetInputAxes();
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

    private System.Collections.IEnumerator BadEnding()
    {
        if (!curtainInitialized) InitCurtain();

        Sequence seq = DOTween.Sequence();
        seq.Append(leftCurtain.DOAnchorPos(leftClosePos, curtainCloseDuration));
        seq.Join(rightCurtain.DOAnchorPos(rightClosePos, curtainCloseDuration));
        yield return seq.WaitForCompletion();

        yield return new WaitForSeconds(0.3f);
        SceneManager.LoadScene("BadEnd");
    }


    void DisplayChoices()
    {
        List<Choice> choices = story.currentChoices;
        isShowingChoices = choices.Count > 0;

        choiceContainer.SetActive(isShowingChoices);

        // 🔒 鎖定輸入，防止剛出選項時空白被誤觸
        StartCoroutine(LockInputTemporarily(0.2f));

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

        Debug.Log($"🟢 DisplayChoices(): choices={choices.Count}, isShowingChoices={isShowingChoices}");
    }

    void OnChoiceSelected(int choiceIndex)
    {
        // 防止重複點選
        if (!isShowingChoices) return;

        isShowingChoices = false;
        choiceContainer.SetActive(false);

        // 🧩 Ink 本身會自動前進到下一段，因此不需要馬上 ContinueStory()
        story.ChooseChoiceIndex(choiceIndex);

        // 🔒 重置輸入狀態，避免馬上又觸發空白鍵
        StartCoroutine(LockInputTemporarily(0.2f));

        // ✅ 直接重新顯示新的對話（Ink 已經自動前進）
        ContinueStory();
    }


    private IEnumerator ContinueAfterChoice()
    {
        yield return new WaitForEndOfFrame(); // 稍微等一幀，讓 Ink 更新完成
        ContinueStory();
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
                    case PortraitSlot.Main:
                        mainPortraitImage.sprite = entry.sprite;
                        break;
                    case PortraitSlot.Guide:
                        guidePortraitImage.sprite = entry.sprite;
                        break;
                    case PortraitSlot.Him:
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

    public bool AllCluesCollected()
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
                case "black_screen":
                    fullblackScreen.SetActive(true);
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
                        break;
                    }
                case "detection":
                    {
                        Debug.Log("🧩 Ink 標籤：#detection → 進行血量檢測");

                        // 找到 HP 物件
                        HP hpSystem = FindObjectOfType<HP>();
                        if (hpSystem != null)
                        {
                            int curHP = hpSystem.hp;
                            Debug.Log($"🩸 目前 HP = {curHP}");

                            if (curHP <= 3)
                            {
                                Debug.Log("⚠️ HP 過低，進入壞結局");
                                JumpToKnot("bad_end"); // ← 改成你壞結局的節點名
                            }
                            else
                            {
                                Debug.Log("✅ HP 足夠，前往下一段劇情");
                                JumpToKnot("your_choice"); // ← 改成你要的後續節點名
                            }
                        }
                        else
                        {
                            Debug.LogWarning("⚠️ 找不到 HP 物件，無法進行檢測");
                        }
                        break;
                    }
                case "Exposure":
                    {
                        Debug.Log("📸 Exposure 觸發 → 換立繪（Main）");
                        Sprite newSprite1 = Resources.Load<Sprite>("NPC/Face_Guide");
                        Sprite newSprite2 = Resources.Load<Sprite>("NPC/NEW_Guide");
                        if (newSprite2 != null)
                            guidePortraitImage.sprite = newSprite2;
                        else
                            Debug.LogWarning("⚠ 找不到 Resources/NPC/exposure_pose.png");

                        if (spriteRenderer != null)
                            spriteRenderer.sprite = newSprite1;
                        else
                            Debug.LogWarning("⚠ 找不到 Resources/NPC/exposure_pose.png");

                        break;
                    }
                case "Lay_down":
                    Debug.Log("📸 Exposure 觸發 → 換立繪（Main）");
                    Sprite newSprite3 = Resources.Load<Sprite>("NPC/Lay_Guide");
                    if (spriteRenderer != null)
                        spriteRenderer.sprite = newSprite3;
                    else
                        Debug.LogWarning("⚠ 找不到 Resources/NPC/exposure_pose.png");

                    break;
                case "Him_appear":
                    Him.SetActive(true);
                    break;
                case "BadEnd":
                    StartCoroutine(BadEnding());
                    break;



            }
            // ====== GameOver 支援 ======
            if (tag.StartsWith("GameOver"))
            {
                string endingName = "END"; // fallback
                foreach (var e in endings)
                {
                    if (e.tagName == tag)
                    {
                        endingName = e.text;
                        break;
                    }
                }
                StartCoroutine(ShowEndingThenReturnMenu(endingName));
                return; // 停止繼續處理其他 tag
            }

        }
    }
    private IEnumerator ShowEndingThenReturnMenu(string endingName)
    {
        // 黑幕顯示
        fullblackScreen.SetActive(true);

        // 顯示結局文字
        endingTextUI.text = endingName;
        endingTextUI.gameObject.SetActive(true);

        // 停 3 秒
        yield return new WaitForSeconds(3f);

        // 回主選單
        SceneManager.LoadScene("MainMenu");
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

        // 綁定所有外部函式
        story.BindExternalFunction("SaveGame", () => {
            saveUI.OpenSaveMenu(story.state.ToJson());
        });

        BindAllExternalFunctions();
        SyncHpFromInk();
        SyncHaveItemsToInk();

        // 僅恢復 UI 狀態，不主動顯示文字
        dialoguePanel.SetActive(false);
        choiceContainer.SetActive(false);
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
                if (itemDatabase.HasItem("recorder"))
                    have = "recorder";

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

    public PortraitEntry[] portraits;

    [System.Serializable]
    public class PortraitEntry
    {
        public string speakerName;
        public Sprite sprite;
        public PortraitSlot slot;
    }

    public enum PortraitSlot
    {
        Main,
        Guide,
        Him
    }

}


