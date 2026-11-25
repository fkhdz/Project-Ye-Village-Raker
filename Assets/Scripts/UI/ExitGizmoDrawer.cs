using UnityEngine;

/// <summary>
/// Komponen helper untuk menggambar Gizmo wire cube pada Exit marker.
/// Warna biru transparan agar mudah terlihat di Scene View.
/// Dipasang pada Exit_To_Zona2 oleh Level1TPSBuilder.
/// </summary>
public class ExitGizmoDrawer : MonoBehaviour
{
    [HideInInspector]
    public Color gizmoColor = new Color(0.2f, 0.4f, 1f, 0.35f);

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        BoxCollider col = GetComponent<BoxCollider>();
        if (col == null) return;

        Gizmos.color = gizmoColor;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(col.center, col.size);

        // Juga gambar filled transparan
        Color filled = gizmoColor;
        filled.a = 0.1f;
        Gizmos.color = filled;
        Gizmos.DrawCube(col.center, col.size);
    }

    private void OnDrawGizmosSelected()
    {
        BoxCollider col = GetComponent<BoxCollider>();
        if (col == null) return;

        Gizmos.color = gizmoColor;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(col.center, col.size);
    }
#endif
}
