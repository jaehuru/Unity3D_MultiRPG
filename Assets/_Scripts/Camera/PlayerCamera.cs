using UnityEngine;
using Jae.Manager;
public class PlayerCamera : MonoBehaviour
{
    [Header("Target Settings")]
    [Tooltip("The player controller to follow")]
    public PlayerController targetController;

    [Tooltip("Transform on the player model for the 1st person camera position")]
    public Transform firstPersonAnchor;
    
    [Tooltip("Transform on the player model for the 3rd person camera pivot")]
    public Transform thirdPersonAnchor;

    [Header("Camera Control")]
    [SerializeField] private float mouseSensitivity = 100f;
    [SerializeField] private Vector2 verticalClamp = new Vector2(-90f, 90f);
    [SerializeField] private float thirdPersonDistance = 5.0f;
    
    private float _xRotation = 0f;
    private bool _isFirstPerson = true;

    void Awake()
    {
        if (CameraManager.Instance != null)
        {
            CameraManager.Instance.RegisterMainCamera(GetComponent<Camera>());
        }
        else
        {
            Debug.LogError("[PlayerCamera] CameraManager.Instance is not found. Make sure a CameraManager is in the scene.");
        }
    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void LateUpdate()
    {
        if (targetController == null)
        {
            return;
        }
        
        Vector2 lookInput = targetController.GetLookInput();
        
        _xRotation -= lookInput.y * mouseSensitivity * Time.deltaTime;
        _xRotation = Mathf.Clamp(_xRotation, verticalClamp.x, verticalClamp.y);
        
        if (_isFirstPerson)
        {
            transform.position = firstPersonAnchor.position;
            transform.rotation = Quaternion.Euler(_xRotation, targetController.transform.eulerAngles.y, 0f);
        }
        else
        {
            thirdPersonAnchor.localRotation = Quaternion.Euler(_xRotation, 0, 0);
            transform.position = thirdPersonAnchor.position - (thirdPersonAnchor.forward * thirdPersonDistance);
            transform.LookAt(thirdPersonAnchor.position);
        }
    }

    public void SetTarget(PlayerController target)
    {
        targetController = target;
        firstPersonAnchor = targetController.transform.Find("FirstPersonAnchor");
        thirdPersonAnchor = targetController.transform.Find("ThirdPersonAnchor");

        if (firstPersonAnchor == null || thirdPersonAnchor == null)
        {
            Debug.LogError("PlayerCamera: FirstPersonAnchor or ThirdPersonAnchor not found as children of the player. Please create them.");
            enabled = false;
        }
    }
    
    public void SwitchView(bool isFirstPersonView)
    {
        _isFirstPerson = isFirstPersonView;
    }

    private void OnDestroy()
    {
        // Unregister the camera when this object is destroyed
        if (CameraManager.Instance != null)
        {
            CameraManager.Instance.UnregisterMainCamera();
        }
    }
}
