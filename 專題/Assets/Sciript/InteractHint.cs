using UnityEngine;

public class InteractHint : MonoBehaviour
{
    private bool isPlayerNear = false;
    public GameObject hintUI; // ������ prefab �W���p���� UI

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
