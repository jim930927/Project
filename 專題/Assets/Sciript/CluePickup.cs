using UnityEngine;
using UnityEngine.UI;

public class CluePickup : MonoBehaviour
{
    [Header("線索設定")]
    public string clueID;          // 對應 ClueData 裡的 id
    public string clueName;        // 顯示名稱（可選）
    public ClueData clueData;      // 指向 ClueDatabase（ScriptableObject）

    [Header("互動設定")]
    public bool destroyOnPickup = true; // 撿起後是否刪除物件

    [Header("Ink 劇情設定")]
    public InkDialogueManager inkManager;
    public TextAsset inkStoryAsset;
    public string startKnotName = "";    // 撿取時要播放的開場
    public string returnKnotName = "";   // 看完線索後要接續的 Knot

    [Header("圖片設定")]
    public Sprite clueImage; // 顯示線索圖片


    private bool playerInRange = false;
    private bool collected = false;


    void Update()
    {
        if (playerInRange && !collected && Input.GetKeyDown(KeyCode.Space))
        {
            CollectClue();
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !collected)
        {
            playerInRange = true;
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
        }
    }

    void CollectClue()
    {
        if (collected) return;

        if (clueData == null)
        {
            Debug.LogWarning($"⚠️ CluePickup：{name} 沒有設定 ClueData！");
            return;
        }

        clueData.AddClue(clueID, clueName);
        collected = true;

        // ✅ 播放 Ink 對話
        if (inkManager != null && inkStoryAsset != null)
        {
            // 顯示圖片
            if (clueImage != null && PreviewImageManager.Instance != null)
                PreviewImageManager.Instance.ShowImage(clueImage);

            inkManager.EnterDialogueMode(inkStoryAsset, startKnotName, () =>
            {
                // 對話結束 → 關閉圖片
                if (PreviewImageManager.Instance != null)
                    PreviewImageManager.Instance.HideImage();

                // 顯示線索 UI
                var bookUI = FindObjectOfType<BookUIManager>();
                if (bookUI != null)
                    bookUI.OpenClueOverlay(clueID, returnKnotName);
            });
        }


        // ✅ 撿起線索，更新 ClueData
        clueData.AddClue(clueID, clueName);
        Debug.Log($"📜 撿取線索：{clueID}");

        // ✅ 播放 Ink 對話
        if (inkManager != null && inkStoryAsset != null)
        {
            inkManager.EnterDialogueMode(inkStoryAsset, startKnotName, () =>
            {
                // Ink 結束 → 顯示線索 UI
                var bookUI = FindObjectOfType<BookUIManager>();
                if (bookUI != null)
                    bookUI.OpenClueOverlay(clueID, returnKnotName); // 傳入要返回的 knot
            });

        }
        else
        {
            Debug.LogWarning("⚠️ Ink Manager 或 Ink Story Asset 未設定，直接顯示線索內容");
            var bookUI = FindObjectOfType<BookUIManager>();
            if (bookUI != null)
                bookUI.OpenClueOverlay(clueID);
        }


        if (destroyOnPickup)
            Destroy(gameObject);
    }
}
