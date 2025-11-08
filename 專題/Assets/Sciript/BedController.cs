using System.Xml;
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
        if (objName != "chest" || chestPrefab == null) return;

        // 1) 計算場景中現有的箱子數（包含被關閉的物件，但排除專案資產/Prefab）
        int CountSceneChests()
        {
            int count = 0;
            var all = Resources.FindObjectsOfTypeAll<ChestController>();
            foreach (var c in all)
            {
                if (c == null) continue;
                if (!c.gameObject.scene.IsValid()) continue;   // 排除專案資產
                count++;
            }
            return count;
        }

        int existing = CountSceneChests();

        // 2) 若已達上限 2 就不再生成
        if (existing >= 2)
        {
            Debug.Log($"📦 已存在 {existing} 個箱子（達上限 2），不重複生成");
            return;
        }

        // 3) 尚未達上限 → 生成一個
        Instantiate(chestPrefab, chestSpawnPoint.position, Quaternion.identity);
        Debug.Log($"📦 生成第 {existing + 1} 個箱子");
    }



}
