using UnityEngine;

public class BedController : MonoBehaviour
{
    [Header("床圖片切換")]
    public SpriteRenderer bedRenderer;
    public Sprite defaultBed;
    public Sprite quiltBed;
    public Sprite neatBed;

    [Header("可生成物件")]
    public GameObject chestPrefab;
    public Transform chestSpawnPoint;

    private bool chestSpawned = false;

    // 🔹 用於保存當前狀態
    private string currentState = "bed_default";

    public void ChangeImage(string state)
    {
        if (bedRenderer == null) return;

        currentState = state; // ✅ 記錄目前狀態（供存檔用）

        switch (state)
        {
            case "bed_quilt":
                bedRenderer.sprite = quiltBed;
                break;
            case "bed_neat":
                bedRenderer.sprite = neatBed;
                break;
            default:
                bedRenderer.sprite = defaultBed;
                break;
        }
        Debug.Log($"🛏️ 床圖片切換為：{state}");
    }

    public string GetCurrentState()
    {
        return currentState;
    }

    public void SpawnObject(string objName)
    {
        if (objName == "chest" && !chestSpawned && chestPrefab != null)
        {
            Instantiate(chestPrefab, chestSpawnPoint.position, Quaternion.identity);
            chestSpawned = true;
            Debug.Log("📦 床底箱子已生成");
        }
    }
}
