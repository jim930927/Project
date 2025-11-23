using UnityEngine;

public class DatabaseSingleton : MonoBehaviour
{
    public static ClueData ClueDB;
    public static ItemData ItemDB;

    void Awake()
    {
        if (ClueDB == null)
        {
            ClueDB = Resources.Load<ClueData>("ClueDatabase");
            DontDestroyOnLoad(ClueDB);
        }
        if (ItemDB == null)
        {
            ItemDB = Resources.Load<ItemData>("ItemDatabase");
            DontDestroyOnLoad(ItemDB);
        }
    }
}

