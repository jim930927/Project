using System.Collections;
using UnityEngine;

public class FinalPortal : MonoBehaviour
{
    [Header("Portal 設定")]
    public string portalID;          // 傳送點自己的 ID
    public string targetPortalID;    // 傳送目的地 ID
    public bool rightway = false;    // 是否為正確的路

    [Header("控制設定")]
    public bool oneWay = false;      // 單向傳送
    public bool canTP = true;        // 是否可傳送
    public float cooldown = 0.3f;    // 傳送冷卻時間

    [Header("迷宮計數")]
    public static float wrongtime = 0;

    [Header("敵人設定")]
    public Transform enemySpawnPoint;

    private static bool isTeleporting = false;
    private static float lastTeleportTime = -999f;
    private bool isPlayerInside = false;
    private GameObject player;

    private ScreenFader fader;
    private FinalInkDialogue dialogue; // ✅ 正確類型

    void Start()
    {
        fader = FindFirstObjectByType<ScreenFader>();
        dialogue = FindFirstObjectByType<FinalInkDialogue>(); // ✅ 確保能抓到正確的對話控制器
    }

    void Update()
    {
        // 🔒 若正在對話中或剛結束（冷卻中）→ 禁止傳送
        if (dialogue != null && (dialogue.dialogueIsPlaying || dialogue.IsInCooldown))
            return;

        if (isTeleporting) return;
        if (!isPlayerInside || player == null) return;

        if (Input.GetKeyDown(KeyCode.Space) && Time.time - lastTeleportTime > cooldown)
        {
            if (!canTP) return;
            StartCoroutine(Teleport());
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

            if (dialogue != null)
            {
                yield return new WaitForSeconds(1f);
                if (wrongtime == 1)
                    dialogue.EnterDialogueMode(dialogue.inkJSON, "wrong_1");
                else if (wrongtime == 5)
                    dialogue.EnterDialogueMode(dialogue.inkJSON, "wrong_5");
                else if (wrongtime == 10)
                    dialogue.EnterDialogueMode(dialogue.inkJSON, "wrong_10");
            }
        }
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
}
