using UnityEngine;

public class BillboardUI : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Position")]
    public float height = 1.8f;
    public float radius = 1.2f;

    [Header("Billboard")]
    public bool followCameraSide = true;

    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;
    }

    void LateUpdate()
    {
        if (mainCamera == null) return;

        if (followCameraSide && target != null)
        {
            Vector3 direction = target.position - mainCamera.transform.position;
            direction.y = 0f;

            if (direction.sqrMagnitude > 0.001f)
            {
                direction.Normalize();
                transform.position = target.position - direction * radius + Vector3.up * height;
            }
        }

        transform.LookAt(transform.position + mainCamera.transform.forward,
            mainCamera.transform.up);
    }
}