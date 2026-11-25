using UnityEngine;

/// <summary>
/// Kamera third-person sederhana: mengikuti Kades dan dapat diputar 360 derajat
/// menggunakan mouse. Dipasang pada Main Camera.
/// (Menggantikan Cinemachine FreeLook karena paket tersebut belum ada di proyek,
/// dengan perilaku yang sama sesuai GDD: follow + orbit 360 derajat.)
/// Modul 17: ditambahkan Camera Collision (SphereCast anti-tembus) yang mulus.
/// </summary>
public class ThirdPersonCamera : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("Karakter yang diikuti. Jika kosong, otomatis mencari GameObject bertag Player.")]
    public Transform target;
    public Vector3 targetOffset = new Vector3(0f, 1.6f, 0f);

    [Header("Pengaturan Kamera")]
    public float distance = 6f;
    public float mouseSensitivity = 3f;
    public float minPitch = -20f;
    public float maxPitch = 60f;
    public float followSmoothTime = 0.05f;

    [Header("Camera Collision (Modul 17)")]
    [SerializeField] private bool enableCameraCollision = true;
    [SerializeField] private float collisionRadius = 0.28f;
    [SerializeField] private float collisionBuffer = 0.18f;
    [SerializeField] private float minCollisionDistance = 0.85f;
    [SerializeField] private float collisionInSpeed = 18f;   // cepat menarik masuk agar tidak sempat tembus
    [SerializeField] private float collisionOutSpeed = 6f;   // lembut mundur kembali saat pandangan bebas
    [Tooltip("Layer 2 (Ignore Raycast) dikecualikan agar kamera tidak memantul ke dinding tak terlihat.")]
    [SerializeField] private LayerMask collisionMask = ~(1 << 2);

    private float yaw;
    private float pitch = 18f;
    private Vector3 currentVelocity;
    private float currentCollisionDistance;
    private readonly RaycastHit[] hitBuffer = new RaycastHit[16];

    private void Start()
    {
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                target = player.transform;
            }
        }

        yaw = transform.eulerAngles.y;
        currentCollisionDistance = distance;

        // Kunci kursor saat bermain agar mouse bisa memutar kamera.
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        // Jangan putar kamera saat game dijeda (menu pause/toko).
        if (PauseMenu.isPaused || Time.timeScale == 0f)
        {
            return;
        }

        // Rotasi orbit 360 derajat mengikuti pergerakan mouse.
        yaw += Input.GetAxis("Mouse X") * mouseSensitivity;
        pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        // Pivot berada di sekitar bahu karakter.
        Vector3 pivot = target.position + targetOffset;
        Quaternion orbitRotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 desiredPosition = pivot + orbitRotation * new Vector3(0f, 0f, -distance);

        // Posisi kamera mengikuti target dengan sedikit smoothing.
        Vector3 smoothedPosition = Vector3.SmoothDamp(transform.position, desiredPosition, ref currentVelocity, followSmoothTime);

        if (enableCameraCollision)
        {
            smoothedPosition = ResolveCameraCollision(pivot, smoothedPosition);
        }

        // Pengaman tambahan: jangan sampai kamera menyelusup ke bawah permukaan tanah.
        smoothedPosition.y = Mathf.Max(smoothedPosition.y, pivot.y - 0.6f);

        transform.position = smoothedPosition;
        transform.rotation = Quaternion.LookRotation(pivot - transform.position);
    }

    /// <summary>
    /// Menarik kamera ke depan jika ada objek fisik terlihat di antara pivot dan posisi kamera.
    /// Mengabaikan collider Player, layer Ignore Raycast, trigger, dan blocker tak terlihat.
    /// </summary>
    private Vector3 ResolveCameraCollision(Vector3 pivot, Vector3 desiredPosition)
    {
        Vector3 toCamera = desiredPosition - pivot;
        float desiredDistance = toCamera.magnitude;
        if (desiredDistance < 0.001f)
        {
            return desiredPosition;
        }

        Vector3 direction = toCamera / desiredDistance;
        float allowedDistance = desiredDistance;

        int hitCount = Physics.SphereCastNonAlloc(
            pivot,
            collisionRadius,
            direction,
            hitBuffer,
            desiredDistance,
            collisionMask,
            QueryTriggerInteraction.Ignore);

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = hitBuffer[i];
            if (hit.collider == null || IsCollisionIgnored(hit.collider))
            {
                continue;
            }

            float candidate = hit.distance - collisionBuffer;
            if (candidate < allowedDistance)
            {
                allowedDistance = candidate;
            }
        }

        allowedDistance = Mathf.Max(minCollisionDistance, allowedDistance);

        // Tarik masuk cepat, mundur lembut.
        float speed = allowedDistance < currentCollisionDistance ? collisionInSpeed : collisionOutSpeed;
        currentCollisionDistance = Mathf.MoveTowards(currentCollisionDistance, allowedDistance, speed * Time.deltaTime);
        currentCollisionDistance = Mathf.Clamp(currentCollisionDistance, minCollisionDistance, desiredDistance);

        return pivot + direction * currentCollisionDistance;
    }

    private bool IsCollisionIgnored(Collider collider)
    {
        // Dinding/blocker tak terlihat (RiverWaterBlockers) ada di layer Ignore Raycast.
        if (collider.gameObject.layer == 2)
        {
            return true;
        }

        // Abaikan seluruh collider Player (termasuk child seperti Gerobak).
        if (target != null && collider.transform.IsChildOf(target))
        {
            return true;
        }

        // Abaikan collider tanpa MeshRenderer aktif di dirinya/parent-nya.
        return !HasVisibleRenderer(collider.gameObject);
    }

    private static bool HasVisibleRenderer(GameObject obj)
    {
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null && renderer.enabled)
        {
            return true;
        }

        Transform parent = obj.transform.parent;
        int guard = 0;
        while (parent != null && guard < 6)
        {
            renderer = parent.GetComponent<Renderer>();
            if (renderer != null && renderer.enabled)
            {
                return true;
            }

            parent = parent.parent;
            guard++;
        }

        return false;
    }
}
