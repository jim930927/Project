using UnityEngine;

public class MemoryTrigger : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [Header("Ink 對話管理器")]
    public InkDialogueManager inkManager;

    [Header("Ink Knot 名稱")]
    public string targetKnot = "storeroom";

    private bool hasTriggered = false; // 防止重複觸發

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasTriggered) return;

        if (other.CompareTag("Player"))
        {
            hasTriggered = true;

            if (inkManager == null)
                inkManager = FindObjectOfType<InkDialogueManager>();

            inkManager.SetPlayerCanMove(false);

            if (inkManager != null && inkManager.story != null)
            {
                Debug.Log($"⚔️ 觸發進入對話：{targetKnot}");
                inkManager.ShowPortraits();   // 🟩 新增
                inkManager.ResetPortraits();  // 🟩 新增
                inkManager.JumpToKnot(targetKnot);
            }
            else
            {
                Debug.LogWarning("⚠️ 找不到 InkDialogueManager 或 Ink 故事尚未建立");
            }
        }
    }
}
