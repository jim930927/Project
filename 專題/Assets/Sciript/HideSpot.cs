using UnityEngine;

public class HideSpot : MonoBehaviour
{
    [Tooltip("玩家可以按下這個鍵來躲藏或離開")]
    public KeyCode hideKey = KeyCode.E;

    private Player playerHide;
    private bool playerInRange = false;

    void Update()
    {
        if (playerHide == null) return;

        // 進入躲藏
        if (playerInRange && !playerHide.isHiding && Input.GetKeyDown(hideKey))
        {
            playerHide.EnterHide(transform);
        }

        // 離開躲藏（不再需要在範圍內）
        else if (playerHide.isHiding && Input.GetKeyDown(hideKey))
        {
            playerHide.ExitHide();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            playerHide = other.GetComponent<Player>();
            Debug.Log("玩家進入可躲藏範圍");
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player") && (playerHide == null || !playerHide.isHiding))
        {
            playerInRange = false;
            playerHide = null;
            Debug.Log("玩家離開躲藏範圍");
        }
    }
}
