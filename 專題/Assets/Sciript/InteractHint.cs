using UnityEngine;

public class InteractHint : MonoBehaviour
{
    private bool isPlayerNear = false;
    public GameObject hintUI; // 直接拖 prefab 上的小提示 UI

    void Start()
    {
        if (hintUI != null)
            hintUI.SetActive(false);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNear = true;
            if (hintUI != null) hintUI.SetActive(true);
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNear = false;
            if (hintUI != null) hintUI.SetActive(false);
        }
    }
}
