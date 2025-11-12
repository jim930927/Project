using UnityEngine;
using UnityEngine.UI;
using static ClueData;

public class FinalItemPickup : MonoBehaviour
{
    [Header("道具設定")]
    [Tooltip("對應 ItemData 裡的 ID")]
    public string itemID;

    [Tooltip("顯示名稱（可選）")]
    public string itemName;

    [Tooltip("指向 ItemData ScriptableObject 資料庫")]
    public ItemData itemData;

    [Header("互動設定")]
    [Tooltip("撿起後是否刪除物件")]
    public bool destroyOnPickup = true;

    [Header("Ink 劇情設定")]
    [Tooltip("Ink 劇情管理器")]
    public FinalInkDialogue inkManager;

    [Tooltip("對應的 Ink 故事檔 (.ink.json)")]
    public TextAsset inkStoryAsset;

    [Tooltip("撿取時要播放的開場 Knot 名稱（可空）")]
    public string startKnotName = "";

    [Tooltip("看完道具後要回到的 Knot 名稱（可空）")]
    public string returnKnotName = "";

    [Header("圖片設定")]
    public Sprite itemImage; // 顯示線索圖片


    private bool playerInRange = false;
    public bool collected = false;


    void Update()
    {
        if (playerInRange && !collected && Input.GetKeyDown(KeyCode.Space))
        {
            CollectItem();
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

    void CollectItem()
    {
        if (collected) return;

        if (itemData == null)
        {
            Debug.LogWarning($"⚠️ ItemPickup：{name} 沒有設定 ItemData！");
            return;
        }

        itemData.AddItem(itemID, itemName);
        collected = true;

        if (inkManager != null && inkStoryAsset != null)
        {

            inkManager.SetPlayerCanMove(false);

            inkManager.EnterDialogueMode(inkStoryAsset, startKnotName, () =>
            {
                if (itemImage != null && PreviewImageManager.Instance != null)
                {
                    Debug.Log("👉 [ItemPickup] 呼叫 ShowImage：" + itemImage.name);
                    PreviewImageManager.Instance.ShowImage(itemImage);
                }


                var bookUI = FindObjectOfType<FinalBookUIManager>();
                if (bookUI != null)
                    bookUI.OpenItemOverlay(itemID, returnKnotName);
            });
        }
        else
        {
            // 沒有 Ink，直接開啟線索
            var bookUI = FindObjectOfType<FinalBookUIManager>();
            if (bookUI != null)
                bookUI.OpenItemOverlay(itemID);
        }

        if (destroyOnPickup)
            gameObject.SetActive(false);
    }

}
