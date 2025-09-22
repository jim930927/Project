using UnityEngine;

public class Player : MonoBehaviour
{
    public float moveSpeed = 5f;
    private Rigidbody2D rb;
    private Vector2 movement;

    [Header("動畫控制")]
    public Animator animator;
    private Vector2 lastDirection = Vector2.down;

    [Header("移動控制")]
    public bool canMove = true; // 可由外部如對話系統控制

    [Header("動畫延遲")]
    //private float idleBuffer = 0.1f; // 緩衝 0.1 秒
    private float lastMoveTime;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        if (!canMove)
        {
            movement = Vector2.zero;
            animator.SetBool("IsMoving", false);
            return;
        }

        // 接收輸入
        movement.x = Input.GetAxisRaw("Horizontal");
        movement.y = Input.GetAxisRaw("Vertical");

        // 限制斜向
        if (movement.x != 0) movement.y = 0;
        else if (movement.y != 0) movement.x = 0;

        // 動畫控制
        if (movement != Vector2.zero)
        {
            lastDirection = movement.normalized;
            animator.SetFloat("MoveX", lastDirection.x);
            animator.SetFloat("MoveY", lastDirection.y);
            animator.SetBool("IsMoving", true);
        }
        else
        {
            animator.SetBool("IsMoving", false);
        }

        animator.SetFloat("LastX", lastDirection.x);
        animator.SetFloat("LastY", lastDirection.y);
    }

    void FixedUpdate()
    {
        if (!canMove) return;
        rb.MovePosition(rb.position + movement * moveSpeed * Time.fixedDeltaTime);
    }

    // 可從 Ink 或其他控制器呼叫
    public void SetCanMove(bool value)
    {
        canMove = value;
    }
}
