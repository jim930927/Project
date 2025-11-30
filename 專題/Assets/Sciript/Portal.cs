using System.Collections;
using System.Linq;
using UnityEngine;

public class Portal : MonoBehaviour
{
    [Header("Portal 設定")]
    public string portalID;          // 傳送點自己的 ID
    public string targetPortalID;    // 傳送目的地 ID
    public string doorGroupID;       // 同一扇門共用的 ID（如 room_door）

    [Header("門屬性設定")]
    public bool isLockedDoor = true;     // 是否需要鎖
    public string requiredKeyID = "";    // ✅ 對應的鑰匙 ID（例：key_room）
    public float cooldown = 0.3f;


    [Header("敵人設定")]
    public Transform enemySpawnPoint; // ✅ 敵人在這個傳送後會出現的位置

    [Header("單向傳送設定")]
    [Tooltip("打勾 = 這個 Portal 是單向的（從此 Portal 傳到 target，之後無法從 target 回到此 Portal）")]
    public bool oneWay = false;
    public bool canTP = true;

    // 若為 true，就能從此 Portal 進行「往外」的傳送（OnTriggerEnter 會接受 Space）
    [HideInInspector] public bool canTeleportOut = true;

    private static float lastTeleportTime = -999f;
    private static bool isTeleporting = false;
    private bool isPlayerInside = false;

    private GameObject player;
    private ScreenFader fader;
    private InkDialogueManager dialogueManager;

    void Start()
    {
        fader = Object.FindFirstObjectByType<ScreenFader>();
        dialogueManager = Object.FindFirstObjectByType<InkDialogueManager>();
    }

    void Update()
    {
        // ✅ 若正在對話或剛結束（冷卻中）→ 禁止任何傳送動作
        if (dialogueManager != null)
        {
            if (dialogueManager.dialogueIsPlaying || dialogueManager.IsInCooldown)
                return;
        }

        if (isTeleporting) return;
        if (!isPlayerInside || player == null) return;

        // 如果這個 Portal 被設為不能往外傳送（例如被另一個單向 Portal 禁用），直接 return
        if (!canTeleportOut) return;

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

            // ✅ 若門已解鎖 → 直接傳送
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
                    if (story != null && dialogueManager.itemDatabase != null)
                    {
                        story.variablesState["have_items"] = GetHeldKey();
                    }
                }
                catch { }

                dialogueManager.EnterDialogueMode(dialogueManager.inkJSON, $"{doorGroupID}", OnDoorDialogueEnd);
            }

        }
    }



    private string GetHeldKey()
    {
        if (dialogueManager == null || dialogueManager.itemDatabase == null)
            return "";

        // ✅ 可擴充支援多把鑰匙
        string[] allKeys = { "key_room", "key_parent", "key_unknow", "key_gold" };
        foreach (var key in allKeys)
        {
            if (dialogueManager.itemDatabase.HasItem(key))
                return key;
        }
        return "";
    }

    private IEnumerator Teleport()
    {
        isTeleporting = true;

        Portal targetPortal = FindTargetPortal();
        if (targetPortal == null)
        {
            Debug.LogWarning($"⚠️ 找不到目標傳送點：{targetPortalID}");
            isTeleporting = false;
            yield break;
        }

        if (player == null)
        {
            player = GameObject.FindWithTag("Player");
            if (player == null)
            {
                Debug.LogWarning("⚠️ 找不到 Player，取消傳送");
                isTeleporting = false;
                yield break;
            }
        }



        if (fader != null)
        {
            yield return StartCoroutine(fader.FadeOut());
        }

        if (player != null && targetPortal != null)
        {
            player.transform.position = targetPortal.transform.position;
            lastTeleportTime = Time.time;
        }
        else
        {
            Debug.LogWarning("⚠️ 傳送時 Player 或 Portal 已被銷毀");
        }

        // 如果這個 portal 被設定為單向，則在傳送後禁用目標 portal 的「往外傳送」能力，
        // 使得玩家無法從目標 portal 再傳回來（實現永久的單向傳送）。
        if (oneWay && targetPortal != null)
        {
            targetPortal.canTeleportOut = false;
            Debug.Log($"➡️ 單向傳送：{portalID} -> {targetPortal.portalID}（已禁用 {targetPortal.portalID} 的往外傳送）");
        } 

        // 🟥 敵人延遲傳送機制 ===========================
        EnemyController2D enemy = FindAnyObjectByType<EnemyController2D>();
        if (enemy != null)
        {
            // 取得目標傳送門對應的敵人生成點
            Transform enemySpawn = targetPortal.enemySpawnPoint != null
                ? targetPortal.enemySpawnPoint
                : targetPortal.transform; // 若沒設定則跟玩家傳送點相同

            float delayBeforeTeleport = 1.5f; // 🔸延遲秒數，可調整
            StartCoroutine(DelayedEnemyTeleport(enemy, enemySpawn, delayBeforeTeleport));
        }
        // =================================================

        // =====================================


        if (fader != null)
        {
            yield return StartCoroutine(fader.FadeIn());
        }

        yield return new WaitForSeconds(0.2f);
        isTeleporting = false;
    }

    private Portal FindTargetPortal()
    {
        Portal[] portals = FindObjectsOfType<Portal>();
        foreach (Portal p in portals)
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

    private IEnumerator DelayedEnemyTeleport(EnemyController2D enemy, Transform spawnPoint, float delay)
    {
        // 🔹 先暫停敵人追逐
        enemy.StopChase();

        // 🔹 延遲一段時間再傳送
        yield return new WaitForSeconds(delay);

        if (enemy == null || spawnPoint == null)
            yield break;

        // 🔹 傳送敵人
        enemy.TeleportTo(spawnPoint);

        // 🔹 傳送後立刻開始追逐
        enemy.StartChase();

        Debug.Log($"👁️ 敵人在延遲 {delay:F1} 秒後傳送並開始追逐");
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
        bool inkUnlocked = false;

        var story = dialogueManager?.GetStory();
        if (story != null && story.variablesState.Contains("Unlock_door"))
        {
            try
            {
                var val = story.variablesState["Unlock_door"];
                inkUnlocked = val is bool b ? b : (val.ToString() == "true");
            }
            catch { }
        }

        if (unlocked && inkUnlocked)
        {
            if (!string.IsNullOrEmpty(requiredKeyID) && dialogueManager.itemDatabase.HasItem(requiredKeyID))
            {
                dialogueManager.itemDatabase.RemoveItem(requiredKeyID);
                SaveItem.RemoveItem(requiredKeyID);   // ← ★★★最關鍵
                Debug.Log($"🗝️ 已使用鑰匙：{requiredKeyID}");
            }
        }
        else
        {
            Debug.Log("❗ 玩家選了『等等』或門沒開，不消耗鑰匙");
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

}
