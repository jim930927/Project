using DG.Tweening;
using Ink.Runtime;
using NUnit.Framework.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static ClueData;
using static UnityEditor.Experimental.GraphView.GraphView;
using static UnityEngine.EventSystems.EventTrigger;

public class InkDialogueManager : MonoBehaviour
{
    public static InkDialogueManager Instance;

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
    public float dialogueEndCooldown = 0.3f;
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
    public BookUIManager bookUIManager;
    public bool doorUnlocked = false;

    [Header("記憶碎片資料庫")]
    public MemoryFragmentData memoryFragmentDatabase;


    [Header("回憶場景")]
    public CanvasGroup blackScreenCanvasGroup; // 黑幕
    public GameObject littleblackScreen;
    public GameObject fullblackScreen;
    public Transform fatherSpawnPoint;          // 爸爸出現位置
    public Transform motherSpawnPoint;          // 媽媽出現位置
    public Transform guideSpawnPoint;
    public Transform enemySpawnPoint;
    public Transform StorySpawnPoint;
    public Transform GateSpawnPoint;
    public GameObject FightEnemy;

    public GameObject PlayerLaySprite;

    [Header("Memory System")]
    public Transform player;
    public Transform memoryPoint1;
    public Transform memoryPoint2;
    public Transform memoryPoint3;
    public Transform memoryPoint4;
    public Transform goOutPoint;
    private Vector3 originalPlayerPos;


    public EnemyController2D enemy2D;
    public GameObject enemyPrefab;
    public Transform enemyAppearPoint;
    private EnemyController2D activeEnemy;
    public Transform playerTransform;

    public GameObject incense;
    public GameObject forcer;
    public GameObject Exam;

    public GameObject AiNpc;

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

            story.BindExternalFunction("OpenSafeUI", () =>
            {
                var safe = GameObject.FindObjectOfType<SafeController>();
                if (safe != null)
                {
                    safe.Interact();
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
                AiNpc.SetActive(true);

                var saveMgr = FindObjectOfType<SaveUIManager>();
                if (saveMgr != null)
                {
                    string savePath = Application.persistentDataPath + "/autosave_spawn.txt";
                }

                /*
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
                */
            });


            // ✅ 讓 Ink 能檢查目前持有的物品
            story.BindExternalFunction(
                "GetHeldItem", () =>
            {
                if (itemDatabase != null && itemDatabase.HasItem("key_room"))
                   return "key_room";
                if (itemDatabase != null && itemDatabase.HasItem("key_parent"))
                   return "key_parent";
                if (itemDatabase != null && itemDatabase.HasItem("key_unknow"))
                   return "key_unknow";
                if (itemDatabase != null && itemDatabase.HasItem("key_gold"))
                    return "key_gold";
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

            if (itemDatabase != null)
            {
                string have = "";
                if (itemDatabase.HasItem("key_parent"))
                    have = "key_parent";
                else if (itemDatabase.HasItem("key_room"))
                    have = "key_room";
                else if (itemDatabase.HasItem("key_unknow"))
                    have = "key_unknow";
                else if (itemDatabase.HasItem("key_gold"))
                    have = "key_gold";

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

        SafeBindString("ChangeBedImage", (string state) =>
        {
            var bed = GameObject.FindObjectOfType<BedController>();
            if (bed != null)
                bed.ChangeImage(state);
        });

        SafeBindString("ChangeToiletImage", (string state) =>
        {
            var toilet = GameObject.FindObjectOfType<toiletController>();
            if (toilet != null)
                toilet.ChangeImage(state);
        });

        SafeBind("OpenChestUI", () =>
        {
            var chest = GameObject.FindObjectOfType<ChestController>();
            if (chest != null)
                chest.Interact();
        });

        SafeBind("OpenSafeUI", () =>
        {
            var safe = GameObject.FindObjectOfType<SafeController>();
            if (safe != null)
                safe.Interact();
        });

        SafeBindString("MovePlayer", (string target) =>
        {
            var player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                var moveTarget = GameObject.Find(target);
                if (moveTarget != null)
                    player.transform.position = moveTarget.transform.position;
            }
        });

        SafeBindString("SpawnObject", (string objName) =>
        {
            var bed = GameObject.FindObjectOfType<BedController>();
            if (bed != null)
                bed.SpawnObject(objName);
        });

        SafeBindString("UnlockDoor", (string doorID) =>
        {
            if (!string.IsNullOrEmpty(doorID))
            {
                DoorManager.Instance?.UnlockDoor(doorID);
                Debug.Log($"🗝️ Ink 呼叫 UnlockDoor：{doorID}");
            }
        });

        SafeBindString("SpawnNPC", (string npcName) =>
        {
            var npcManager = GameObject.FindObjectOfType<NPCManager>();
            if (npcManager != null)
            {
                npcManager.SpawnNPC(npcName);
                Debug.Log($"🧍 Ink 呼叫 SpawnNPC：{npcName}");
            }
            else
            {
                Debug.LogWarning($"⚠️ 沒有找到 NPCManager，無法生成 NPC：{npcName}");
            }
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

            // 🔹 檢查是否有 #Enemy_disappear 標籤
            foreach (var tag in story.currentTags)
            {
                if (tag == "Enemy_disappear")
                {
                    Debug.Log("💨 偵測到 #Enemy_disappear，開始讓敵人消失");
                    HideEnemy();
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
                case "memory1":
                    originalPlayerPos = player.position;
                    StartCoroutine(EnterMemoryScene("memory1"));
                    break;
                case "memory2":
                    originalPlayerPos = player.position;
                    StartCoroutine(EnterMemoryScene("memory2"));
                    break;
                case "memory3":
                    originalPlayerPos = player.position;
                    StartCoroutine(EnterMemoryScene("memory3"));
                    break;
                case "memory4":
                    originalPlayerPos = player.position;
                    StartCoroutine(EnterMemoryScene("memory4"));
                    break;
                case "go_out":
                    originalPlayerPos = player.position;
                    StartCoroutine(EnterMemoryScene("go_out"));
                    break;
                case "father_appear":
                    SpawnNPC("Father");
                    break;
                case "mother_appear":
                    SpawnNPC("Mother");
                    break;
                case "guide_appear":
                    Debug.Log($"生成NPC");
                    SpawnMemory("GuideNPC");
                    break;
                case "enemy_appear":
                    Debug.Log($"生成NPC");
                    SpawnMemory("EnemyNPC");
                    break;
                case "StoryNPC":
                    SpawnStory("StoryNPC");
                    break;
                case "gate_NPC":
                    SpawnStory("GateNPC");
                    break;
                case "FightEnemy":
                    Debug.Log("⚔️ Ink 觸發 #FightEnemy，讓敵人出現！");
                    if (FightEnemy != null)
                    {
                        FightEnemy.gameObject.SetActive(true);
                    }
                    break;

                case "lay_down":
                    FadePlayer();
                    PlayerLaySprite.SetActive(true);
                    break;
                case "wake":
                    ShowPlayer();
                    PlayerLaySprite.SetActive(false);
                    break;
                case "Enemy_disappear":
                    HideEnemy();
                    break;
                case "EnemyNPC_disspear":
                    DestroyAllEnemyNPCs();
                    break;
                case "GuideNPC_disspear":
                    DestroyAllGuideNPCs();
                    break;
                case "StoryNPC_disspear":
                    DestroyAllStoryNPCs();
                    break;
                case "sink_memory_end":
                    StartCoroutine(ExitMemoryScene());
                    shouldAutoContinue = true; // 🟩 這類 tag 通常沒有對話，繼續下一段
                    break;
                case "refrigerator_memory_end":
                    StartCoroutine(ExitMemoryScene());
                    shouldAutoContinue = true;
                    break;
                case "amulet_memory_end":
                    StartCoroutine(ExitMemoryScene());
                    shouldAutoContinue = true;
                    break;
                case "store_memory_end":
                    StartCoroutine(ExitMemoryScene());
                    shouldAutoContinue = true; 
                    break;
                case "turn_back":
                    TurnPlayerBack();
                    break;
                case "turn_left":
                    TurnPlayerLeft();
                    break;
                case "turn_up":
                    TurnPlayerUP();
                    break;
                case "Enemy_appear":
                    if (activeEnemy == null)
                    {
                        GameObject enemyObj = Instantiate(enemyPrefab, enemyAppearPoint.position, Quaternion.identity);
                        activeEnemy = enemyObj.GetComponent<EnemyController2D>();
                        activeEnemy.player = playerTransform; // 指派玩家
                    }
                    else
                    {
                        activeEnemy.AppearAtPoint();
                    }
                    break;
                case "start_chase":
                    shouldAutoContinue = true; // 🟩 同上
                    activeEnemy?.StartChase();
                    break;
                case "burn":
                    incense.SetActive(true);
                    break;
                case "open_forcer":
                    if (forcer != null) forcer.SetActive(true);
                    break;
                case "black_screen":
                    fullblackScreen.SetActive(true);
                    break;
                case "back_screen":
                    fullblackScreen.SetActive(false);
                    break;
                case "Exam_appear":
                    ExamAppear();
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


    // -----------------------------
    // 記憶場景切換、NPC出現
    // -----------------------------
    IEnumerator EnterMemoryScene(string memoryName)
    {
        yield return StartCoroutine(FadeScreen(true)); // 黑屏
        player.GetComponent<Animator>().SetFloat("LastY", 1);
        littleblackScreen.SetActive(true);

        // 記錄原始位置
        Debug.Log($"紀錄進入記憶前的位置：{originalPlayerPos}");

        // 傳送到記憶座標
        if (memoryName == "memory1" && memoryPoint1 != null)
            player.position = memoryPoint1.position;
        else if (memoryName == "memory2" && memoryPoint2 != null)
            player.position = memoryPoint2.position;
        else if (memoryName == "memory3" && memoryPoint3 != null)
            player.position = memoryPoint3.position;
        else if (memoryName == "memory4" && memoryPoint4 != null)
            player.position = memoryPoint4.position;
        else if (memoryName == "go_out" && goOutPoint != null)
            player.position = goOutPoint.position;

        yield return StartCoroutine(FadeScreen(false)); // 淡出黑幕
    }

    IEnumerator ExitMemoryScene()
    {
        littleblackScreen.SetActive(false);
        yield return StartCoroutine(FadeScreen(true)); // 黑屏
        player.GetComponent<Animator>().SetFloat("LastY", 1);

        // 刪除 NPC（如果你有 Spawn 過）
        DestroyAllNPCs();

        // 傳回原位置
        player.position = originalPlayerPos;
        Debug.Log("回到原座標");

        yield return StartCoroutine(FadeScreen(false)); // 淡出黑幕
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

    void SpawnNPC(string npcName)
    {
        GameObject prefab = Resources.Load<GameObject>($"NPCs/{npcName}");
        if (prefab == null) return;

        Vector3 spawnPos = npcName == "Father" ? fatherSpawnPoint.position : motherSpawnPoint.position;
        Instantiate(prefab, spawnPos, Quaternion.identity);
    }

    void SpawnMemory(string npcName)
    {
        GameObject prefab = Resources.Load<GameObject>($"NPCs/{npcName}");
        if (prefab == null) return;

        Vector3 spawnPos = npcName == "EnemyNPC" ? enemySpawnPoint.position : guideSpawnPoint.position;
        Instantiate(prefab, spawnPos, Quaternion.identity);
    }

    void SpawnStory(string npcName)
    {
        GameObject prefab = Resources.Load<GameObject>($"NPCs/{npcName}");
        if (prefab == null) return;

        Vector3 spawnPos = npcName == "StoryNPC" ? StorySpawnPoint.position : GateSpawnPoint.position;
        Instantiate(prefab, spawnPos, Quaternion.identity);
    }

    private void TurnPlayerBack()
    {
        if (player == null) return;

        player.GetComponent<Animator>().SetFloat("LastY", -1);

        Debug.Log("Player turned back (rotated 180 degrees)");
    }

    private void TurnPlayerLeft()
    {
        if (player == null) return;

        player.GetComponent<Animator>().SetFloat("LastX", -1);

        Debug.Log("Player turned back (rotated 180 degrees)");
    }

    private void TurnPlayerUP()
    {
        if (player == null) return;

        player.GetComponent<Animator>().SetFloat("LastY", 1);

        Debug.Log("Player turned back (rotated 180 degrees)");
    }

    void DestroyAllNPCs()
    {
        foreach (var npc in GameObject.FindGameObjectsWithTag("NPC"))
        {
            Destroy(npc);
        }
    }

    void DestroyAllEnemyNPCs()
    {
        foreach (var npcE in GameObject.FindGameObjectsWithTag("EnemyNPC"))
        {
            Destroy(npcE);
        }
    }

    void DestroyAllGuideNPCs()
    {
        foreach (var npcG in GameObject.FindGameObjectsWithTag("GuideNPC"))
        {
            Destroy(npcG);
        }
    }

    void DestroyAllStoryNPCs()
    {
        foreach (var npcS in GameObject.FindGameObjectsWithTag("StoryNPC"))
        {
            Destroy(npcS);
        }
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

    private void FadePlayer()
    {
        var player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            // 你可以選擇用 Destroy、SetActive(false)，或播放動畫
            // 以下範例用淡出動畫（若你有 DOTween）
            var sprite = player.GetComponent<SpriteRenderer>();
            if (sprite != null)
            {
                sprite.DOFade(0f, 0.1f);
            }
            Debug.Log("🧩 玩家已消失");
        }
        else
        {
            Debug.LogWarning("⚠️ 場景中找不到 tag 為 'Player' 的物件");
        }
    }
    private void ShowPlayer()
    {
        var player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            // 你可以選擇用 Destroy、SetActive(false)，或播放動畫
            // 以下範例用淡出動畫（若你有 DOTween）
            var sprite = player.GetComponent<SpriteRenderer>();
            if (sprite != null)
            {
                sprite.DOFade(1f, 0.1f);
            }
            Debug.Log("🧩 玩家已回復");
        }
        else
        {
            Debug.LogWarning("⚠️ 場景中找不到 tag 為 'Player' 的物件");
        }
    }
    private void HideEnemy()
    {
        var enemy = GameObject.FindWithTag("Enemy");
        if (enemy != null)
        {
            // 你可以選擇用 Destroy、SetActive(false)，或播放動畫
            // 以下範例用淡出動畫（若你有 DOTween）
            var sprite = enemy.GetComponent<SpriteRenderer>();
            if (sprite != null)
            {
                sprite.DOFade(0f, 0.5f).OnComplete(() => enemy.SetActive(false));
            }
            else
            {
                enemy.SetActive(false);
            }
            Debug.Log("🧩 敵人已消失");
        }
        else
        {
            Debug.LogWarning("⚠️ 場景中找不到 tag 為 'Enemy' 的物件");
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
                else if (itemDatabase.HasItem("key_store"))
                    have = "key_store";

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
        
    public void ExamAppear()
    {
        Exam.SetActive(true);
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
