using DG.Tweening;
using System.Collections;
using System.Linq;
using UnityEngine;

public class FinalPortal : MonoBehaviour
{
    [Header("Portal 設定")]
    public string portalID;          // 傳送點自己的 ID
    public string targetPortalID;    // 傳送目的地 ID
    public string doorGroupID;       // 同一扇門共用的 ID（如 room_door）

    public bool rightway = false;    // 是否為正確的路

    [Header("控制設定")]
    public bool oneWay = false;      // 單向傳送
    public bool canTP = true;        // 是否可傳送
    public float cooldown = 0.3f;    // 傳送冷卻時間

    [Header("迷宮計數")]
    public static float wrongtime = 0;

    [Header("敵人設定")]
    public Transform enemySpawnPoint;

    [Header("門屬性設定")]
    public bool isLockedDoor = true;     // 是否需要鎖
    public string requiredKeyID = "";    // ✅ 對應的鑰匙 ID（例：key_room）

    private static bool isTeleporting = false;
    private static float lastTeleportTime = -999f;
    private bool isPlayerInside = false;
    private GameObject player;

    private ScreenFader fader;
    private FinalInkDialogue dialogueManager; // ✅ 正確類型

    public GameObject GuideNPC;

    void Start()
    {
        fader = FindFirstObjectByType<ScreenFader>();
        dialogueManager = FindFirstObjectByType<FinalInkDialogue>(); // ✅ 確保能抓到正確的對話控制器
    }

    void Update()
    {
        // 🔒 若正在對話中或剛結束（冷卻中）→ 禁止傳送
        if (dialogueManager != null && (dialogueManager.dialogueIsPlaying || dialogueManager.IsInCooldown))
            return;

        if (isTeleporting) return;
        if (!isPlayerInside || player == null) return;

        if (Input.GetKeyDown(KeyCode.Space) && Time.time - lastTeleportTime > cooldown)
        {
            if (!canTP)
            {
                return;
            }

            // ✅ 不鎖的門：直接傳送
            if (!isLockedDoor)
            {
                StartCoroutine(Teleport());
                return;
            }

            bool unlocked = DoorManager.Instance?.IsUnlocked(doorGroupID) ?? false;
            if (unlocked)
            {
                StartCoroutine(Teleport());
                return;
            }

            // ✅ 未解鎖 → 啟動 Ink 對話
            if (!dialogueManager.dialogueIsPlaying)
            {
                try
                {
                    var story = dialogueManager.GetStory();
                    if (story != null && dialogueManager.clueDatabase != null)
                    {
                        story.variablesState["have_clues"] = GetHeldKey();
                    }
                }
                catch { }

                dialogueManager.EnterDialogueMode(dialogueManager.inkJSON, $"{doorGroupID}", OnDoorDialogueEnd);
            }
        }

        
    }

    private IEnumerator Teleport()
    {
        isTeleporting = true;

        // 🔍 尋找目標 FinalPortal
        FinalPortal target = FindTargetPortal();
        if (target == null)
        {
            Debug.LogWarning($"⚠️ 找不到目標傳送點：{targetPortalID}");
            isTeleporting = false;
            yield break;
        }


        // 🕶️ 淡出畫面
        if (fader != null)
            yield return StartCoroutine(fader.FadeOut());

        // ✅ 傳送玩家
        if (player == null)
            player = GameObject.FindWithTag("Player");

        if (player != null)
        {
            player.transform.position = target.transform.position;
            lastTeleportTime = Time.time;
        }


        // 🔁 單向傳送設定
        if (oneWay && target != null)
        {
            target.canTP = false;
            Debug.Log($"➡️ 單向傳送：{portalID} -> {target.portalID}");
        }


        // 🕶️ 淡入畫面
        if (fader != null)
            yield return StartCoroutine(fader.FadeIn());

        yield return new WaitForSeconds(0.2f);
        isTeleporting = false;

        // 🧭 錯路檢查與對話
        if (!rightway)
        {
            wrongtime += 1;
            Debug.Log($"🚶‍♂️ 玩家走錯路 {wrongtime} 次");

            if (dialogueManager != null)
            {
                yield return new WaitForSeconds(1f);
                if (wrongtime == 1)
                    dialogueManager.EnterDialogueMode(dialogueManager.inkJSON, "wrong_1");
                else if (wrongtime == 5)
                    dialogueManager.EnterDialogueMode(dialogueManager.inkJSON, "wrong_5");
                else if (wrongtime == 10)
                    dialogueManager.EnterDialogueMode(dialogueManager.inkJSON, "wrong_10");
            }
        }

        // ✅ 木屋出口特殊事件（全線索收集後觸發）
        if (portalID == "b1") // 👈 改成你的木屋出口的 portalID
        {
            var dialogue = FindFirstObjectByType<FinalInkDialogue>();
            if (dialogue != null && HasCollectedAllClues(dialogue))
            {
                Debug.Log("🌟 所有線索已收集，觸發引路人被抓走事件");

                GuideNPC.SetActive(true);
                // 停止玩家移動
                dialogue.SetPlayerCanMove(false);

                // 可加特效（引路人被拉走）
                GameObject guide = GuideNPC;
                if (guide != null)
                {
                    guide.transform.DOScale(0, 1f).SetEase(Ease.InBack);
                    guide.transform.DOLocalMoveX(guide.transform.localPosition.x + 3f, 1f);
                    yield return new WaitForSeconds(1f);
                }

                // 進入 Ink 劇情
                dialogue.EnterDialogueMode(dialogue.inkJSON, "guide_captured");
                yield break;
            }
        }

    }

    private string GetHeldKey()
    {
        if (dialogueManager == null || dialogueManager.clueDatabase == null)
            return "";

        // ✅ 可擴充支援多把鑰匙
        string[] allKeys = { "recorder" };
        foreach (var key in allKeys)
        {
            if (dialogueManager.clueDatabase.HasClue(key))
                return key;
        }
        return "";
    }


    private FinalPortal FindTargetPortal()
    {
        FinalPortal[] portals = FindObjectsOfType<FinalPortal>();
        foreach (FinalPortal p in portals)
        {
            if (p.portalID == targetPortalID)
                return p;
        }
        return null;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInside = true;
            player = other.gameObject;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInside = false;
            player = null;
        }
    }

    private void OnDoorDialogueEnd()
    {
        // 防止對話剛結束立即重複觸發
        StartCoroutine(HandleDoorDialogueEnd());
    }

    private IEnumerator HandleDoorDialogueEnd()
    {
        // ✅ 對話剛結束時先暫時禁止傳送
        isTeleporting = true;

        bool unlocked = DoorManager.Instance != null && DoorManager.Instance.IsUnlocked(doorGroupID);
        // 若門在 Ink 中被設為 Unlock_door = true，但 DoorManager 尚未同步 → 嘗試補登
        if (!unlocked)
        {
            var story = dialogueManager?.GetStory();
            if (story != null && story.variablesState.Contains("Unlock_door"))
            {
                bool inkUnlocked = false;
                try
                {
                    var val = story.variablesState["Unlock_door"];
                    inkUnlocked = val is bool b ? b : (val.ToString() == "true");
                    Debug.Log($"🗝️ 解鎖：{doorGroupID}");
                }
                catch { }

            }
        }

        // ✅ 若門已解鎖 → 稍等 0.5 秒再傳送
        if (!unlocked)
        {
            Debug.Log($"🚪 門 {doorGroupID} 仍然鎖著。");
        }

        // ✅ 避免對話剛結束又立刻傳送
        yield return new WaitForSeconds(1f);
        isTeleporting = false;

    }

    // 🧩 用來檢查是否收集完所有線索（等同 AllCluesCollected）
    private bool HasCollectedAllClues(FinalInkDialogue dialogue)
    {
        var clueData = dialogue.clueDatabase;
        if (clueData == null)
        {
            Debug.LogWarning("⚠️ HasCollectedAllClues(): 沒找到 ClueData");
            return false;
        }

        var requiredClues = dialogue.tagClueIDs;
        if (requiredClues == null || requiredClues.Length == 0)
        {
            Debug.Log("⚠️ 未設定 tagClueIDs，預設視為收集完成");
            return true;
        }

        foreach (var clueID in requiredClues)
        {
            if (!clueData.HasClue(clueID))
            {
                Debug.Log($"❌ 缺少線索：{clueID}");
                return false;
            }
        }

        Debug.Log("✅ 全部線索已收集");
        return true;
    }

}
