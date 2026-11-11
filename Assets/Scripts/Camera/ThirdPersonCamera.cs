using UnityEngine;

/// <summary>
/// Kamera third-person sederhana: mengikuti Kades dan dapat diputar 360 derajat
/// menggunakan mouse. Dipasang pada Main Camera.
/// (Menggantikan Cinemachine FreeLook karena paket tersebut belum ada di proyek,
/// dengan perilaku yang sama sesuai GDD: follow + orbit 360 derajat.)
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

    private float yaw;
    private float pitch = 18f;
    private Vector3 currentVelocity;

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

        // Rotasi orbit 360 derajat mengikuti pergerakan mouse.
        yaw += Input.GetAxis("Mouse X") * mouseSensitivity;
        pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        // Pivot berada di sekitar bahu karakter.
        Vector3 pivot = target.position + targetOffset;
        Quaternion orbitRotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 desiredPosition = pivot + orbitRotation * new Vector3(0f, 0f, -distance);

        // Posisi kamera mengikuti target dengan sedikit smoothing.
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref currentVelocity, followSmoothTime);
        transform.rotation = Quaternion.LookRotation(pivot - transform.position);
    }
}
