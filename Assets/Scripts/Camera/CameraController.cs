using System.Collections;
using System.Collections.Generic;
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

    private bool useCamera;
    //private bool useCamera;


    private void Start()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        //useCamera = !Cursor.visible;//when cursor is visible, we lock the camera
        //useCmaera is inited to true
    }

    void Update()
    {
        //useCamera = !Cursor.visible;
        if (!Cursor.visible)
        {
            invertYValue = invertYAxis ? -1f : 1f;
            invertXValue = invertXAxis ? -1f : 1f;
            rotationY += Input.GetAxis("Camera X") * rotationSpeed * invertYValue;
            rotationX += Input.GetAxis("Camera Y") * rotationSpeed * invertXValue;
            rotationX = Mathf.Clamp(rotationX, minVerticalAngle, maxVerticalAngle);
            var targetRotation = Quaternion.Euler(rotationX, rotationY, 0);
            var focusedPosition = followedTarget.position + new Vector3(framingOffset.x, framingOffset.y);
            transform.SetPositionAndRotation(focusedPosition - (targetRotation * new Vector3(0, 0, cameraToPlayerDistance)), targetRotation);
        }
    }

    public Quaternion PlanarRotation => Quaternion.Euler(0, rotationY, 0);
}
