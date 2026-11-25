using UnityEngine;

/// <summary>
/// Komponen penanda volume area pemunculan sampah (Trash Spawner v2).
/// Dipasang pada GameObject BoxCollider (isTrigger).
/// Memiliki bobot peluang (weight) dan menggambar Gizmo hijau transparan di Scene View.
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class TrashSpawnArea : MonoBehaviour
{
    [Tooltip("Bobot peluang terpilihnya area ini oleh TrashSpawner.")]
    [Range(1, 100)]
    public int weight = 25;

    [Tooltip("Warna Gizmo di Scene View.")]
    public Color gizmoColor = new Color(0.2f, 0.85f, 0.3f, 0.22f);

    private BoxCollider boxCollider;

    public BoxCollider AreaCollider
    {
        get
        {
            if (boxCollider == null)
            {
                boxCollider = GetComponent<BoxCollider>();
            }
            return boxCollider;
        }
    }

    private void Awake()
    {
        if (AreaCollider != null)
        {
            AreaCollider.isTrigger = true;
        }
    }

    /// <summary>
    /// Menghasilkan titik acak (X, Z) di dalam batas collider area.
    /// Y ditempatkan di bagian atas collider agar siap di-raycast ke bawah.
    /// </summary>
    public Vector3 GetRandomPointInside()
    {
        BoxCollider col = AreaCollider;
        if (col == null) return transform.position;

        Vector3 center = col.center;
        Vector3 size = col.size;

        float randomX = Random.Range(center.x - size.x * 0.5f, center.x + size.x * 0.5f);
        float randomZ = Random.Range(center.z - size.z * 0.5f, center.z + size.z * 0.5f);
        float topY = center.y + size.y * 0.5f;

        return transform.TransformPoint(new Vector3(randomX, topY, randomZ));
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        BoxCollider col = AreaCollider;
        if (col == null) return;

        Gizmos.matrix = transform.localToWorldMatrix;

        Color fill = gizmoColor;
        Gizmos.color = fill;
        Gizmos.DrawCube(col.center, col.size);

        Color wire = gizmoColor;
        wire.a = 0.85f;
        Gizmos.color = wire;
        Gizmos.DrawWireCube(col.center, col.size);
    }

    private void OnDrawGizmosSelected()
    {
        BoxCollider col = AreaCollider;
        if (col == null) return;

        Gizmos.matrix = transform.localToWorldMatrix;
        Color selectColor = Color.yellow;
        selectColor.a = 0.4f;
        Gizmos.color = selectColor;
        Gizmos.DrawCube(col.center, col.size);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(col.center, col.size);
    }
#endif
}
