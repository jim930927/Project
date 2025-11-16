using UnityEngine;
using UnityEngine.UI;

public class FinalCluePickup : MonoBehaviour
{

    [Header("線索設定")]
    public string clueID;          // 對應 ClueData 裡的 id
    public string clueName;        // 顯示名稱（可選）
    public ClueData clueData;      // 指向 ClueDatabase（ScriptableObject）

    [Header("互動設定")]
    public bool destroyOnPickup = true; // 撿起後是否刪除物件

    [Header("Ink 劇情設定")]
    public FinalInkDialogue inkManager;
    public TextAsset inkStoryAsset;
    public string startKnotName = "";    // 撿取時要播放的開場
    public string returnKnotName = "";   // 看完線索後要接續的 Knot

    [Header("圖片設定")]
    public Sprite clueImage; // 顯示線索圖片


    private bool playerInRange = false;
    public bool collected = false;


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

    public void CollectClue()
    {
        if (collected) return;

        if (clueData == null)
        {
            Debug.LogWarning($"⚠️ CluePickup：{name} 沒有設定 ClueData！");
            return;
        }

        clueData.AddClue(clueID, clueName);
        collected = true;

        if (inkManager != null && inkStoryAsset != null)
        {
            // ✅ 顯示圖片    
            inkManager.SetPlayerCanMove(false);

            inkManager.EnterDialogueMode(inkStoryAsset, startKnotName, () =>
            {
                if (clueImage != null && PreviewImageManager.Instance != null)
                {
                    Debug.Log("👉 [CluePickup] 呼叫 ShowImage：" + clueImage.name);
                    PreviewImageManager.Instance.ShowImage(clueImage);
                }

                // 顯示線索 UI
                var bookUI = FindObjectOfType<FinalBookUIManager>();
                if (bookUI != null)
                    bookUI.OpenClueOverlay(clueID, returnKnotName);
            });
        }
        else
        {
            // 沒有 Ink，直接開啟線索
            var bookUI = FindObjectOfType<FinalBookUIManager>();
            if (bookUI != null)
                bookUI.OpenClueOverlay(clueID);
        }

        if (destroyOnPickup)
            gameObject.SetActive(false);
    }

}
