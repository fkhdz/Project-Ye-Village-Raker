using UnityEngine;

/// <summary>
/// Modul 18A (BUG-10): memberi tahu pemain saat menabrak batas peta atau tepi sungai.
/// Dipasang pada child trigger tipis di dalam area playable (Layer 2 / Ignore Raycast),
/// sehingga tidak pernah mengganggu collision ThirdPersonCamera.
/// </summary>
[RequireComponent(typeof(Collider))]
public class BoundaryFeedbackTrigger : MonoBehaviour
{
    [SerializeField] private string warningMessage = "Batas Wilayah Desa Sukamaju";
    [SerializeField] private float cooldownSeconds = 2.5f;

    [Tooltip("Durasi banner peringatan tampil di layar.")]
    [SerializeField] private float displayDuration = 2.2f;

    private float nextAllowedTime = 0f;

    private static readonly Color WarningColor = new Color(0.85f, 0.62f, 0.12f, 1f);

    private void Reset()
    {
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        TryNotify(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TryNotify(other);
    }

    private void TryNotify(Collider other)
    {
        if (other == null)
        {
            return;
        }

        if (!other.CompareTag("Player") && other.GetComponentInParent<PlayerController>() == null)
        {
            return;
        }

        if (Time.time < nextAllowedTime)
        {
            return;
        }

        nextAllowedTime = Time.time + Mathf.Max(0.1f, cooldownSeconds);

        if (UIManager.instance != null)
        {
            UIManager.instance.ShowFeedback(warningMessage, WarningColor, displayDuration);
        }
        else
        {
            Debug.Log("[Boundary] " + warningMessage);
        }
    }
}
