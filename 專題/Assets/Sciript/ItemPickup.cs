using UnityEngine;
using UnityEngine.UI;

public class ItemPickup : MonoBehaviour
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
    public InkDialogueManager inkManager;

    [Tooltip("對應的 Ink 故事檔 (.ink.json)")]
    public TextAsset inkStoryAsset;

    [Tooltip("撿取時要播放的開場 Knot 名稱（可空）")]
    public string startKnotName = "";

    [Tooltip("看完道具後要回到的 Knot 名稱（可空）")]
    public string returnKnotName = "";

    private bool playerInRange = false;
    private bool collected = false;

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

        // ✅ 新增道具至資料庫
        itemData.AddItem(itemID, itemName);
        collected = true;

        Debug.Log($"🎒 撿取道具：{itemID}");

        // ✅ 如果有 Ink 對話，播放劇情後再顯示道具內容
        if (inkManager != null && inkStoryAsset != null)
        {
            inkManager.EnterDialogueMode(inkStoryAsset, startKnotName, () =>
            {
                // 劇情結束後顯示道具細節
                var bookUI = FindObjectOfType<BookUIManager>();
                if (bookUI != null)
                    bookUI.OpenBook(); // 打開書
                // 直接切換到道具頁
                bookUI.SendMessage("SwitchTab", "item", SendMessageOptions.DontRequireReceiver);
            });
        }
        else
        {
            // 沒有劇情，直接打開書的道具頁
            var bookUI = FindObjectOfType<BookUIManager>();
            if (bookUI != null)
            {
                bookUI.OpenBook();
                bookUI.SendMessage("SwitchTab", "item", SendMessageOptions.DontRequireReceiver);
            }
        }

        if (destroyOnPickup)
            Destroy(gameObject);
    }
}
