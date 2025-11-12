using UnityEngine;

public class BedController : MonoBehaviour
{
    [Header("床圖片切換")]
    public SpriteRenderer bedRenderer;
    public Sprite defaultBed;
    public Sprite quiltBed;
    public Sprite neatBed;

    [Header("可生成物件")]
    public GameObject chestPrefab;          // 若場景中已有關閉的箱子，直接拖這個物件（預設 inactive）
    public Transform chestSpawnPoint;

    private bool chestSpawned = false;      // 防重複生成
    private string currentState = "bed_default";

    public void ChangeImage(string state)
    {
        if (!bedRenderer) return;
        currentState = state;

        switch (state)
        {
            case "bed_quilt": bedRenderer.sprite = quiltBed; break;
            case "bed_neat": bedRenderer.sprite = neatBed; break;
            default: bedRenderer.sprite = defaultBed; break;
        }
        Debug.Log($"🛏️ 床圖片切換為：{state}");
    }

    public string GetCurrentState() => currentState;

    // 依你的專案選一種生成方式即可（A 或 B）

    // A) 場景已放一個關閉的箱子物件（預設 inactive）
    public void SpawnObject(string objName)
    {
        if (objName != "chest") return;
        if (chestSpawned) return;

        if (chestPrefab != null)
        {
            // 若 chestPrefab 是場景中的關閉物件，直接啟用
            chestPrefab.SetActive(true);
            chestSpawned = true;
            Debug.Log("📦 床底箱子已啟用");
        }
        else
        {
            // B) 沒有現成物件 → 動態 Instantiate 一個預製物
            Debug.LogWarning("⚠️ 未指定場景中的 chestPrefab，改用 Instantiate 方式");
            // 確保你的 chestPrefab 指向一個 Prefab 資源
            // var chest = Instantiate(chestPrefab, chestSpawnPoint.position, Quaternion.identity);
            // chestSpawned = true;
            // Debug.Log("📦 床底箱子已生成");
        }

        // ❌ 不再做任何 Load 通知或延遲——Chest 會在自己的 Awake() 自動吃到狀態
    }
}
