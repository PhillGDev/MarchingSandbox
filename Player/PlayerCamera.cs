using UnityEngine;
using TMPro;
public class PlayerCamera : MonoBehaviour
{
    public TextMeshProUGUI Tooltip;
    public Transform CameraTransform, LockTransform, HandsPos;
    public float Sway, Smooth;
    public static PlayerCamera Singleton;
    bool active = true;
    string tooltip = "";
    public void SetActive(bool State)
    {
        active = State;
    }
    public void SetLock(Transform toSet)
    {
        LockTransform = toSet;
    }
    public void SetCursorLock(CursorLockMode mode)
    {
        Cursor.lockState = mode;
    }
    private void Awake()
    {
        Singleton = this;
    }
    public void UpdateCamera()
    {
        if (LockTransform != null)
        {
            CameraTransform.SetPositionAndRotation(LockTransform.position, LockTransform.rotation);
        }
        float x = Input.GetAxis("Mouse Y") * Sway;
        float y = Input.GetAxis("Mouse X") * Sway;
        Quaternion xQuat = Quaternion.AngleAxis(-x, Vector3.right);
        Quaternion yQuat = Quaternion.AngleAxis(y, Vector3.up);
        Quaternion total = xQuat * yQuat;
        HandsPos.transform.localRotation = Quaternion.Slerp(HandsPos.transform.localRotation, total, Smooth * Time.smoothDeltaTime);
    }
    private void Update()
    {
        UpdateCamera();
        if (tooltip != "")
        {
            Tooltip.text = tooltip;
            tooltip = "";
        }
    }
    public void ShowTooltip(string tooltip)
    {
        this.tooltip = tooltip;
    }
}
