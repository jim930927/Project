using UnityEngine;
using System.Collections;

public class Portal : MonoBehaviour
{
    public string portalID;
    public string targetPortalID;
    public float cooldown = 0.5f;

    private static float lastTeleportTime = -999f;
    private bool isPlayerInside = false;
    private GameObject player;
    private ScreenFader fader;

    private void Start()
    {
        fader = Object.FindFirstObjectByType<ScreenFader>();
    }

    private void Update()
    {
        if (isPlayerInside && player != null)
        {
            if (Input.GetKeyDown(KeyCode.Space) && Time.time - lastTeleportTime > cooldown)
            {
                StartCoroutine(Teleport());
            }
        }
    }

    private IEnumerator Teleport()
    {
        Portal targetPortal = FindTargetPortal();
        if (targetPortal != null && fader != null)
        {
            // ²H¥X
            yield return StartCoroutine(fader.FadeOut());

            // ¶Ç°e
            player.transform.position = targetPortal.transform.position;
            lastTeleportTime = Time.time;

            // ²H¤J
            yield return StartCoroutine(fader.FadeIn());
        }
    }

    private Portal FindTargetPortal()
    {
        Portal[] portals = FindObjectsOfType<Portal>();
        foreach (Portal p in portals)
        {
            if (p.portalID == targetPortalID)
            {
                return p;
            }
        }
        return null;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInside = true;
            player = other.gameObject;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInside = false;
            player = null;
        }
    }
}
