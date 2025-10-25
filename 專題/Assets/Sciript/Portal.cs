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
    public float cooldown = 0.8f;

    [Header("敵人設定")]
    public Transform enemySpawnPoint; // ✅ 敵人在這個傳送後會出現的位置

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
        // ✅ 若正在對話 → 禁止任何傳送動作
        if (dialogueManager != null && dialogueManager.dialogueIsPlaying)
            return;

        if (isTeleporting) return;
        if (!isPlayerInside || player == null) return;

        if (Input.GetKeyDown(KeyCode.Space) && Time.time - lastTeleportTime > cooldown)
        {
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

            // ✅ 未解鎖 → 啟動 Ink 對話（由 Ink 控制是否能開門）
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
        string[] allKeys = { "key_room", "key_parent" , "key_unknow" , "key_gold"};
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

        yield return new WaitForSeconds(0.3f);
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
        dialogueManager.itemDatabase.RemoveItem(requiredKeyID);
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

                if (inkUnlocked)
                {
                    DoorManager.Instance.UnlockDoor(doorGroupID);
                    unlocked = true;
                    Debug.Log($"🗝️ 自動補登解鎖：{doorGroupID}");

                    // ===== 新增：如果門有設定 requiredKeyID，且玩家持有該鑰匙，就消耗它 =====
                    if (!string.IsNullOrEmpty(requiredKeyID) && dialogueManager != null && dialogueManager.itemDatabase != null)
                    {
                        try
                        {
                            if (dialogueManager.itemDatabase.HasItem(requiredKeyID))
                            {
                                dialogueManager.itemDatabase.RemoveItem(requiredKeyID);
                                Debug.Log($"🗝️ 已消耗鑰匙：{requiredKeyID}");
                            }
                        }
                        catch (System.Exception ex)
                        {
                            Debug.LogWarning($"⚠️ 無法消耗鑰匙 {requiredKeyID}：{ex.Message}");
                        }
                    }
                    // ===================================================================
                }
            }
        }

        // ✅ 若門已解鎖 → 稍等 0.5 秒再傳送
        if (!unlocked)
        {
            Debug.Log($"🚪 門 {doorGroupID} 仍然鎖著。");
        }

        // ✅ 避免對話剛結束又立刻傳送
        yield return new WaitForSeconds(1.5f);
        isTeleporting = false;

    }

}
