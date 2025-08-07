using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Unity.VisualScripting;

public class WorldSpacePanel : MonoBehaviour
{
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI descriptionText;

    public Button button;


    Camera cameraMain;

    private Transform cameraMainTransform;
    public Vector3 offset = new Vector3(0, 0, 10f);

    void Start()
    {
        cameraMain = FindCamera();
        if (cameraMainTransform == null && cameraMain != null)
        {
            cameraMainTransform = cameraMain.transform;
        }

        Debug.Log($"camera FOV: {cameraMain.fieldOfView}");

        var cameraService = SpatialSys.UnitySDK.SpatialBridge.cameraService;
        if (cameraService != null)
        {
            Debug.Log($"First Person Camera FoV: {cameraService.firstPersonFov}");
            Debug.Log($"Third Person Camera FoV: {cameraService.thirdPersonFov}");
            cameraService.thirdPersonFov = cameraService.firstPersonFov;
        }

        /*
                transform.SetParent(targetCamera, worldPositionStays: false);
                transform.localPosition = offset;
                transform.localRotation = Quaternion.identity;
        */

        if (button != null)
        {
            button.onClick.AddListener(OnButtonClick);
        }

        if (titleText == null)
        {
            titleText = GetComponentInChildren<TextMeshProUGUI>();
        }

    }
    private void OnButtonClick()
    {
        Debug.Log("Button clicked!");
        // Implement your button click logic here

        titleText.text = "Button Clicked!";

    }

    private Camera FindCamera()
    {

        var cam = GameObject.FindGameObjectWithTag("MainCamera")?.GetComponent<Camera>();
        Debug.Assert(cam != null);
        if (cam != null) return cam;
        return null;
    }


    /*
        void LateUpdate()
        {
            if (targetCamera == null) return;


            // Posiziona il pannello davanti alla camera
            transform.position = targetCamera.position + targetCamera.forward * offset.z + targetCamera.up * offset.y + targetCamera.right * offset.x;
            transform.rotation = Quaternion.LookRotation(transform.position - targetCamera.position);

            Debug.Log($"moving to {transform.position} {transform.rotation}");
        }
    */


    /*
    void Update()
    {
        Camera currentCam = FindCamera();
        if (currentCam.transform != cameraMainTransform)
        {
            Debug.Log("Camera changed");
            cameraMainTransform = currentCam.transform;
        }
    }
    */


    void LateUpdate()
    {
        if (cameraMainTransform == null) return;

        // Ottieni punto sul bordo destro della view (x=1 è destra, y=0.5 è centro verticale)
        Vector3 rightEdgeWorldPos = cameraMain.ViewportToWorldPoint(new Vector3(offset.x, 0.5f, offset.z));

        // Imposta il pannello lì, con un piccolo margine a sinistra
        Vector3 right = cameraMainTransform.transform.right;
        float margin = 0.1f;

        transform.position = rightEdgeWorldPos - right * margin;

        // Rendi il pannello piatto rispetto alla camera
        transform.rotation = Quaternion.Euler(cameraMainTransform.eulerAngles.x, cameraMainTransform.eulerAngles.y, 0);
        // transform.rotation = Quaternion.identity;


        /*

                        float distance = Vector3.Distance(transform.position, targetCamera.position);
        float scaleFactor = 0.0008f;
        transform.localScale = Vector3.one * (distance * 
        scaleFactor);
        */


}

    public void UpdateText(string title, string description)
    {
        if (titleText != null) titleText.text = title;
        if (descriptionText != null) descriptionText.text = description;
    }
}
