using UnityEngine;
using static NPCManager;

public class EnemyTrigger : MonoBehaviour
{
    [Header("Ink 對話管理器")]
    public InkDialogueManager inkManager;

    [Header("Ink Knot 名稱")]
    public string targetKnot = "enemy";

    private bool hasTriggered = false; // 防止重複觸發

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasTriggered) return;

        if (other.CompareTag("Player"))
        {
            hasTriggered = true;

            // ⭐ 更改敵人的 Tag → EnemyHide
            if (gameObject.CompareTag("Enemy"))
            {
                gameObject.tag = "EnemyHide";
                Debug.Log("🔄 敵人 Tag 已更改為 EnemyHide");
            }

            if (inkManager == null)
                inkManager = FindObjectOfType<InkDialogueManager>();

            inkManager.SetPlayerCanMove(false);

            if (inkManager != null && inkManager.story != null)
            {
                Debug.Log($"⚔️ 觸發進入對話：{targetKnot}");
                inkManager.ShowPortraits();
                inkManager.ResetPortraits();
                inkManager.JumpToKnot(targetKnot);
            }
            else
            {
                Debug.LogWarning("⚠️ 找不到 InkDialogueManager 或 Ink 故事尚未建立");
            }
        }
    }
}
