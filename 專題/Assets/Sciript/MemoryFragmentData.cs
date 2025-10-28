using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MemoryFragmentDatabase", menuName = "Game/Memory Fragment Database")]
public class MemoryFragmentData : ScriptableObject
{
    [System.Serializable]
    public class Fragment
    {
        public string id;
        public string name;
        public bool collected;
        public float collectedTime;
    }

    public List<Fragment> fragments = new List<Fragment>();

    public delegate void FragmentAddedHandler(Fragment fragment);
    public event FragmentAddedHandler OnFragmentAdded;

    public void AddFragment(string id, string name = null)
    {
        Fragment frag = fragments.Find(f => f.id == id);
        if (frag != null)
        {
            if (!frag.collected)
            {
                frag.collected = true;
                frag.collectedTime = Time.time;
                OnFragmentAdded?.Invoke(frag);
            }
        }
        else
        {
            frag = new Fragment { id = id, name = name ?? id, collected = true, collectedTime = Time.time };
            fragments.Add(frag);
            OnFragmentAdded?.Invoke(frag);
        }

        Debug.Log($"🧠 獲得記憶碎片：{frag.name}");
    }

    public int GetCollectedCount()
    {
        return fragments.FindAll(f => f.collected).Count;
    }

    public bool HasFragment(string id)
    {
        Fragment frag = fragments.Find(f => f.id == id);
        return frag != null && frag.collected;
    }

    public void ResetAll()
    {
        foreach (var f in fragments)
            f.collected = false;
    }
}
