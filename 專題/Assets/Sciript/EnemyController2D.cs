using UnityEngine;

public class EnemyController2D : MonoBehaviour
{
    [Header("基本設定")]
    public Transform player;          // 玩家位置
    public Transform appearPoint;     // 出現位置
    public float moveSpeed = 12f;      // 追逐速度
    public bool isChasing = false;

    [Header("動畫控制")]
    public Animator animator;
    public Vector2 lastDirection = Vector2.down;
    public Vector2 movement;


    private Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (!isChasing || player == null)
            return;

        // 追逐玩家：使用方向向量
        Vector2 direction = (player.position - transform.position).normalized;
        rb.MovePosition(rb.position + direction * moveSpeed * Time.deltaTime);

        movement = (player.position - transform.position).normalized;
        animator.SetBool("IsMoving", movement.sqrMagnitude > 0.001f);

        if (movement != Vector2.zero)
        {
            lastDirection = movement.normalized;
            animator.SetFloat("MoveX", movement.x);
            animator.SetFloat("MoveY", movement.y);
            animator.SetBool("IsMoving", true);
        }
        else
        {
            animator.SetBool("IsMoving", false);
            rb.linearVelocity = Vector2.zero;
        }

        animator.SetFloat("LastX", lastDirection.x);
        animator.SetFloat("LastY", lastDirection.y);
    }

    // 🟥 在指定位置出現（不追）
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
        Debug.Log("👁️ 敵人出現於房間位置");
    }

    // 🟩 開始追逐玩家
    public void StartChase()
    {
        if (player == null)
        {
            Debug.LogWarning("⚠️ player 未設定");
            return;
        }

        isChasing = true;
        Debug.Log("🏃 敵人開始追逐玩家！");
    }

    // 🟦 停止追逐（例如切換房間時）
    public void StopChase()
    {
        isChasing = false;
        rb.linearVelocity = Vector2.zero;
    }

    public void TeleportTo(Transform newPoint)
    {
        if (newPoint == null) return;
        transform.position = newPoint.position;
        transform.rotation = newPoint.rotation;
        Debug.Log($"🚪 敵人重新定位到 {newPoint.name}");
    }
}
