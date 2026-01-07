using UnityEngine;
using UnityEngine.InputSystem; // Needed for direct hardware access
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class PlayerNetwork : NetworkBehaviour
{
    [Header("Scene Settings")]
    public string menuSceneName = "Menu"; 

    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float lookSpeed = 0.05f;
    public float sensitivity = 5f;
    public float jumpForce = 5f;

    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private AudioListener playerAudioListener;
    [SerializeField] private LayerMask layersToIgnore;

    // Network State
    private NetworkVariable<int> health = new NetworkVariable<int>(100, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // Inputs & Logic
    private Vector2 moveInput;
    private Vector2 lookInput;
    private float xRotation = 0f;
    private bool isGrounded = true;

    private Rigidbody rb;
    private Animator anim;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        anim = GetComponent<Animator>();

        if (playerCamera != null) playerCamera.gameObject.SetActive(false);
        if (playerAudioListener != null) playerAudioListener.enabled = false;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        SceneManager.sceneLoaded += OnSceneChanged;
        CheckSceneState(SceneManager.GetActiveScene());
    }

    public override void OnNetworkDespawn()
    {
        SceneManager.sceneLoaded -= OnSceneChanged;
    }

    private void OnSceneChanged(Scene scene, LoadSceneMode mode)
    {
        CheckSceneState(scene);
    }

    private void CheckSceneState(Scene scene)
    {
        if (scene.name == menuSceneName)
        {
            if (rb != null) rb.isKinematic = true;
            if (playerCamera != null) playerCamera.gameObject.SetActive(false);
            if (playerAudioListener != null) playerAudioListener.enabled = false;
            enabled = false;
        }
        else
        {
            if (rb != null) rb.isKinematic = false;

            if (IsOwner)
            {
                transform.position += Vector3.up * 0.5f;

                if (playerCamera != null)
                {
                    playerCamera.gameObject.SetActive(true);
                    
                    int finalMask = -1;
                    finalMask &= ~layersToIgnore.value;

                    int p1Layer = LayerMask.NameToLayer("p1");
                    int p2Layer = LayerMask.NameToLayer("p2");

                    if (OwnerClientId == 0)
                    {
                        if (p2Layer != -1) finalMask &= ~(1 << p2Layer);
                    }
                    else
                    {
                        if (p1Layer != -1) finalMask &= ~(1 << p1Layer);
                    }

                    playerCamera.cullingMask = finalMask;
                }

                if (playerAudioListener != null) playerAudioListener.enabled = true;
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                enabled = true;
            }
        }
    }

    void Update()
    {
        if (!IsOwner) return;

        // --- 1. READ KEYBOARD (WASD) ---
        float x = 0;
        float y = 0;
        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed) y = +1;
            if (Keyboard.current.sKey.isPressed) y = -1;
            if (Keyboard.current.aKey.isPressed) x = -1;
            if (Keyboard.current.dKey.isPressed) x = +1;
        }
        moveInput = new Vector2(x, y);

        // --- 2. READ MOUSE (Look) ---
        float mouseX = 0;
        float mouseY = 0;
        if (Mouse.current != null)
        {
            mouseX = Mouse.current.delta.x.ReadValue();
            mouseY = Mouse.current.delta.y.ReadValue();
        }
        lookInput = new Vector2(mouseX, mouseY);

        // --- 3. READ JUMP (Space) ---
        // "wasPressedThisFrame" is the manual version of OnJump
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            PerformJump();
        }

        // Apply Logic
        HandleMovement();
        HandleCameraLook(); 
        UpdateAnimations();
    }

    private void HandleMovement()
    {
        if (rb.isKinematic) return;

        Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;
        transform.position += move * moveSpeed * Time.deltaTime;
    }

    private void HandleCameraLook()
    {
        if (playerCamera == null) return;

        float mouseX = lookInput.x * lookSpeed * sensitivity;
        transform.Rotate(Vector3.up * mouseX);

        float mouseY = lookInput.y * lookSpeed * sensitivity;
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        playerCamera.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
    }

    private void UpdateAnimations()
    {
        if (anim == null) return;
        bool isMoving = moveInput != Vector2.zero;
        anim.SetBool("move", isMoving);
        anim.SetBool("isGrounded", isGrounded);
    }

    // Replaced OnJump with this manual function
    private void PerformJump()
    {
        if (!IsOwner || rb.isKinematic) return;

        if (isGrounded)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            isGrounded = false;
            if(anim != null) anim.SetTrigger("jump");
        }
    }

    public void TakeDamage(int damage)
    {
        if (!IsServer) return;
        health.Value -= damage;
        if(health.Value <= 0) health.Value = 100;
    }

    void OnCollisionEnter(Collision collision)
    {
        if(collision.gameObject.CompareTag("Ground")) isGrounded = true;
    }
}