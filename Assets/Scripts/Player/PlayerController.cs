using UnityEngine;

/// <summary>
/// Pergerakan Kades (third-person, camera-relative) sesuai dokumen 02.
/// Dipasang pada GameObject "Kades" yang memiliki Rigidbody + CapsuleCollider.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Referensi Kamera")]
    [Tooltip("Transform Main Camera. Jika kosong, otomatis memakai Camera.main.")]
    public Transform cam;

    [Header("Status Gerak")]
    public float moveSpeed = 5f;
    public float turnSmoothTime = 0.1f;
    public bool isSlowed = false;

    private Rigidbody rb;
    private Animator anim;
    private float turnSmoothVelocity;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        anim = GetComponentInChildren<Animator>();

        // Fallback aman: jika slot Cam belum diisi di Inspector.
        if (cam == null && Camera.main != null)
        {
            cam = Camera.main.transform;
        }
    }

    private void FixedUpdate()
    {
        // 1. Input WASD mentah.
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        Vector3 direction = new Vector3(horizontal, 0f, vertical).normalized;

        // Kecepatan aktual: debuff B3 mengurangi 40% kecepatan.
        float currentSpeed = isSlowed ? moveSpeed * 0.6f : moveSpeed;

        if (direction.magnitude >= 0.1f && cam != null)
        {
            // 2. Arah gerak relatif kamera: sudut target memakai yaw kamera (sumbu Y).
            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + cam.eulerAngles.y;

            // Rotasi karakter dihaluskan menuju arah gerak.
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSmoothTime);
            transform.rotation = Quaternion.Euler(0f, angle, 0f);

            // 3. Vektor gerak hasil rotasi target.
            Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;

            // 4. Fisika Rigidbody: sumbu Y TIDAK diubah agar gravitasi bawaan tetap bekerja.
            rb.linearVelocity = new Vector3(moveDir.x * currentSpeed, rb.linearVelocity.y, moveDir.z * currentSpeed);
        }
        else
        {
            // Diam: hentikan X/Z, biarkan Y (gravitasi) apa adanya.
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
        }

        // 5. Animasi berjalan (aman jika Animator belum terpasang).
        if (anim != null)
        {
            anim.SetBool("isWalking", direction.magnitude >= 0.1f);
        }
    }
}
