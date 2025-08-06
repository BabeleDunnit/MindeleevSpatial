using UnityEngine;
using TMPro;

public class WorldSpacePanel : MonoBehaviour
{
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI descriptionText;

    private Transform targetCamera;
    public Vector3 offset = new Vector3(0, 0, 10f);

    void Start()
    {
        Camera cameraMain = FindCamera();
        if (targetCamera == null && cameraMain != null)
        {
            targetCamera = cameraMain.transform;
        }
    }

    private Camera FindCamera()
    {

        var cam = GameObject.FindGameObjectWithTag("MainCamera")?.GetComponent<Camera>();
        Debug.Assert(cam != null);
        if (cam != null) return cam;
        return null;
    }


    void LateUpdate()
    {
        if (targetCamera == null) return;


        // Posiziona il pannello davanti alla camera
        transform.position = targetCamera.position + targetCamera.forward * offset.z + targetCamera.up * offset.y + targetCamera.right * offset.x;
        transform.rotation = Quaternion.LookRotation(transform.position - targetCamera.position);

        Debug.Log($"moving to {transform.position} {transform.rotation}");
    }

    public void UpdateText(string title, string description)
    {
        if (titleText != null) titleText.text = title;
        if (descriptionText != null) descriptionText.text = description;
    }
}
