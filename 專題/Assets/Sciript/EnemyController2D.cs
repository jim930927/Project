using UnityEngine;
using System.Collections;

public class EnemyController2D : MonoBehaviour
{
    [Header("基本設定")]
    public Transform player;
    public Transform appearPoint;
    public float moveSpeed = 3f;
    public bool isChasing = false;

    [Header("追丟設定")]
    public float losePlayerTime = 3f; // 幾秒後消失
    public float loseDistance = 8f;   // 超出距離才判定追丟
    private float loseTimer = 0f;
    private bool isFadingOut = false;

    [Header("動畫控制")]
    public Animator animator;
    private Rigidbody2D rb;
    private InkDialogueManager dialogueManager;

    void Start()
    {
        dialogueManager = FindFirstObjectByType<InkDialogueManager>();
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (!isChasing || player == null)
        {
            animator.SetBool("IsMoving", false);
            return;
        }

        Player hide = player.GetComponent<Player>();
        bool playerHiding = (hide != null && hide.isHiding);

        float distance = Vector2.Distance(transform.position, player.position);

        // 🔹 Debug 資訊印出來看看
        Debug.Log($"[Enemy] 距離: {distance:F2}, 躲藏中: {playerHiding}, loseTimer: {loseTimer:F2}");

        // 🟦 當玩家太遠或躲藏時 → 開始倒數
        if (playerHiding)
        {
            rb.linearVelocity = Vector2.zero;
            animator.SetBool("IsMoving", false);
            loseTimer += Time.deltaTime;

            if (loseTimer >= losePlayerTime)
            {
                StartCoroutine(DisappearNow());
            }
            return;
        }

        // 🟥 追逐移動
        Vector2 dir = (player.position - transform.position).normalized;
        rb.MovePosition(rb.position + dir * moveSpeed * Time.deltaTime);

        animator.SetBool("IsMoving", true);
        animator.SetFloat("MoveX", dir.x);
        animator.SetFloat("MoveY", dir.y);
    }

    public void StopChase()
    {
        isChasing = false;
        rb.linearVelocity = Vector2.zero;
        animator.SetBool("IsMoving", false);
        loseTimer = 0f;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (dialogueManager != null)
            {
                dialogueManager.EnterDialogueMode(dialogueManager.inkJSON, "get_catch");
            }
        }
    }
    private IEnumerator DisappearNow()
    {
        if (isFadingOut) yield break;
        isFadingOut = true;
        animator.SetBool("IsMoving", false);
        yield return new WaitForSeconds(0.5f);
        gameObject.SetActive(false);
        Debug.Log("✅ 敵人已消失");
        if (dialogueManager != null)
        {
            dialogueManager.EnterDialogueMode(dialogueManager.inkJSON, "gone");
        }
    }

    public void StartChase()
    {
        if (isFadingOut) return;
        isChasing = true;
        loseTimer = 0f;
        gameObject.SetActive(true);
        animator.SetBool("IsMoving", true);
        Debug.Log("🏃 敵人開始追逐玩家！");
    }
    public void AppearAtPoint()
    {
        if (appearPoint == null)
        {
            Debug.LogWarning("⚠️ appearPoint 未設定");
            return;
        }

        transform.position = appearPoint.position;
        gameObject.SetActive(true);
        isChasing = false;
        loseTimer = 0f;
        isFadingOut = false;
        animator.SetBool("IsMoving", false);
        Debug.Log("👁️ 敵人出現於房間位置");
    }

    public void TeleportTo(Transform newPoint)
    {
        if (newPoint == null) return;
        transform.position = newPoint.position;
        transform.rotation = newPoint.rotation;
        Debug.Log($"🚪 敵人重新定位到 {newPoint.name}");
    }
}





