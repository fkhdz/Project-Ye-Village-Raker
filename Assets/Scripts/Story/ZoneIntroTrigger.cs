using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class ZoneIntroTrigger : MonoBehaviour
{
    public string sequenceId = "Zona2";

    private void Awake()
    {
        BoxCollider col = GetComponent<BoxCollider>();
        if (col != null) col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        StoryFlow.Instance?.TryZoneIntro(sequenceId);
    }
}
