using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InteractionUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject root;
    public TextMeshProUGUI interactionText;
    public Image progressCircle;

    [Header("Look At Camera")]
    public bool lookAtCamera = true;

    private Camera mainCamera;

    private void Awake()
    {
        mainCamera = Camera.main;
        Hide();
    }

    private void LateUpdate()
    {
        if (!lookAtCamera || mainCamera == null) return;

        transform.LookAt(transform.position + mainCamera.transform.rotation * Vector3.forward,
            mainCamera.transform.rotation * Vector3.up);
    }

    public void Show(string message)
    {
        if (root != null)
        {
            root.SetActive(true);
        }

        if (interactionText != null)
        {
            interactionText.text = message;
        }
    }

    public void Hide()
    {
        if (root != null)
        {
            root.SetActive(false);
        }

        SetProgress(0f);
    }

    public void SetProgress(float value)
    {
        if (progressCircle != null)
        {
            progressCircle.fillAmount = Mathf.Clamp01(value);
        }
    }
}