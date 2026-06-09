using UnityEngine;

public class OrbitAroundObjectToCamera : MonoBehaviour
{
    [Header("Object mà UI sẽ xoay quanh")]
    public Transform targetObject;

    [Header("Vị trí UI quanh object")]
    public float height = 2f;
    public float radius = 1.2f;

    [Header("Camera")]
    public Camera targetCamera;

    private void Awake()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }
    }

    private void LateUpdate()
    {
        if (targetObject == null || targetCamera == null) return;

        // Lấy hướng từ object tới camera
        Vector3 directionToCamera = targetCamera.transform.position - targetObject.position;

        // Bỏ trục Y để UI chỉ xoay quanh object trên mặt phẳng ngang
        directionToCamera.y = 0f;

        if (directionToCamera.sqrMagnitude < 0.001f)
        {
            directionToCamera = targetObject.forward;
        }

        directionToCamera.Normalize();

        // Đặt UI ở phía object hướng về camera
        transform.position = targetObject.position + directionToCamera * radius + Vector3.up * height;

        // Cho UI quay mặt về camera
        transform.rotation = targetCamera.transform.rotation;
    }
}