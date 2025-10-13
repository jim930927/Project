using UnityEngine;

public class HP : MonoBehaviour
{
    public int hp = 3;

    void Update()
    {
        if (hp < 0) hp = 0;
    }
}
