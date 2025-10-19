using System.Collections.Generic;
using UnityEngine;

public class DoorManager : MonoBehaviour
{
    public static DoorManager Instance { get; private set; }

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
    }

    public bool IsUnlocked(string doorID)
    {
        return unlockedDoors.Contains(doorID);
    }

    public void UnlockDoor(string doorID)
    {
        if (!unlockedDoors.Contains(doorID))
        {
            unlockedDoors.Add(doorID);
            Debug.Log($"🔓 門 {doorID} 已解鎖！");
        }
    }

    public void LockDoor(string doorID)
    {
        if (unlockedDoors.Contains(doorID))
        {
            unlockedDoors.Remove(doorID);
            Debug.Log($"🔒 門 {doorID} 被重新上鎖");
        }
    }
}
