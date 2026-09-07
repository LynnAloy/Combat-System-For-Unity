using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private Transform followedTarget;
    [SerializeField] private float cameraToPlayerDistance;
    [SerializeField] private float minVerticalAngle;
    [SerializeField] private float maxVerticalAngle;
    [SerializeField] private Vector2 framingOffset;
    [SerializeField] private float rotationSpeed;
    [SerializeField] private bool invertYAxis;
    [SerializeField] private bool invertXAxis;

    private float rotationY;
    private float rotationX;
    private float invertYValue;
    private float invertXValue;

    private bool isCameraEnabled = true;

    private void Start()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        //useCamera = !Cursor.visible;//when cursor is visible, we lock the camera
        //useCmaera is inited to true
    }

    private void LateUpdate()
    {
        if (followedTarget == null || PauseManager.IsPaused)
        {
            return;
        }

        // 光标和开关只控制视角输入。
        if (!Cursor.visible && isCameraEnabled)
        {
            invertXValue = invertXAxis ? -1f : 1f;
            invertYValue = invertYAxis ? -1f : 1f;

            rotationY += Input.GetAxis("Camera X") * rotationSpeed * invertXValue;
            rotationX += Input.GetAxis("Camera Y") * rotationSpeed * invertYValue;
            rotationX = Mathf.Clamp(rotationX, minVerticalAngle, maxVerticalAngle);
        }

        // 位置始终跟随代码驱动的玩家根节点。
        Quaternion cameraRotation = Quaternion.Euler(rotationX, rotationY, 0f);
        Vector3 framing = new Vector3(framingOffset.x, framingOffset.y, 0f);
        Vector3 focusedPosition = followedTarget.position + framing;
        Vector3 cameraOffset = cameraRotation * Vector3.back * cameraToPlayerDistance;
        Vector3 cameraPosition = focusedPosition + cameraOffset;

        transform.SetPositionAndRotation(cameraPosition, cameraRotation);
    }

    public void SetCameraEnabled(bool isEnabled)
    {
        isCameraEnabled = isEnabled;

        if(isEnabled)
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
        else
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }

    public Quaternion PlanarRotation => Quaternion.Euler(0, rotationY, 0);
}
