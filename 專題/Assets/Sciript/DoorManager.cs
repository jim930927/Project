using System.Collections.Generic;
using UnityEngine;

public class DoorManager : MonoBehaviour
{
    public static DoorManager Instance { get; private set; }

    // 🔐 所有永久解鎖的門（用 uniqueID）
    private HashSet<string> unlockedDoors = new HashSet<string>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Debug.Log("🚪 DoorManager 已啟動。");
    }

    // ========================================================
    // 🔍 查詢門是否已永久解鎖
    // ========================================================
    public bool IsUnlocked(string doorID)
    {
        return unlockedDoors.Contains(doorID);
    }

    // ========================================================
    // 🔓 解鎖門（遊戲中解鎖的瞬間會呼叫）
    // ========================================================
    public void UnlockDoor(string doorID)
    {
        if (!unlockedDoors.Contains(doorID))
        {
            unlockedDoors.Add(doorID);
            Debug.Log($"🔓【永久解鎖】門：{doorID}");
        }
    }

    // ========================================================
    // 🔒（很少用）重新上鎖
    // ========================================================
    public void LockDoor(string doorID)
    {
        if (unlockedDoors.Contains(doorID))
        {
            unlockedDoors.Remove(doorID);
            Debug.Log($"🔒【永久上鎖】門：{doorID}");
        }
    }

    // ========================================================
    // 💾 存檔時呼叫：取得所有已解鎖門的列表
    // ========================================================
    public List<string> GetUnlockedDoorList()
    {
        return new List<string>(unlockedDoors);
    }

    // ========================================================
    // 📥 讀檔時呼叫：套用讀取到的永久解鎖列表
    // ========================================================
    public void LoadUnlockedDoors(List<string> savedList)
    {
        unlockedDoors.Clear();
        foreach (var door in savedList)
            unlockedDoors.Add(door);

        Debug.Log($"📥 已套用永久解鎖門，共 {unlockedDoors.Count} 個。");
    }
}
