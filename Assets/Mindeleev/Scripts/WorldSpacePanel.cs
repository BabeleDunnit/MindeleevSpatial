using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Unity.VisualScripting;
using SpatialSys.UnitySDK;

public class WorldSpacePanel : MonoBehaviour
{
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI descriptionText;

    // where are we running? mobile, web, hmd?
    SpatialPlatform platform;

    public Button button;


    Camera cameraMain;

    private Transform cameraMainTransform;
    public Vector3 offset = new Vector3(0.5f, 0, 1f);

    void Start()
    {
        cameraMain = FindCamera();
        if (cameraMainTransform == null && cameraMain != null)
        {
            cameraMainTransform = cameraMain.transform;
        }

        Debug.Log($"camera FOV: {cameraMain.fieldOfView}");

        var cameraService = SpatialBridge.cameraService;
        if (cameraService != null)
        {
            Debug.Log($"First Person Camera FoV: {cameraService.firstPersonFov}");
            Debug.Log($"Third Person Camera FoV: {cameraService.thirdPersonFov}");
            cameraService.thirdPersonFov = cameraService.firstPersonFov;
        }

        if (button != null)
        {
            button.onClick.AddListener(OnButtonClick);
        }

        if (titleText == null)
        {
            titleText = GetComponentInChildren<TextMeshProUGUI>();
        }



        // detect platform
        platform = SpatialBridge.actorService.localActor.platform;
        switch (platform)
        {
            case SpatialPlatform.MetaQuest:
                Debug.Log("Running on HMD");
                // should keep it at z = 0.5 and scale 
                offset = new Vector3(0.6f, 0, 2.5f);
                break;
            case SpatialPlatform.Mobile:
                Debug.Log("Running on Mobile");
                break;
            case SpatialPlatform.Web:
                Debug.Log("Running on Web");
                offset = new Vector3(0.9f, 0, 0.9f);
                break;
            default:
                Debug.LogWarning("Unknown platform type");
                break;
        }
        
    }
    private void OnButtonClick()
    {
        Debug.Log("Button clicked!");
        // Implement your button click logic here

        // titleText.text = "Button Clicked!";

        // find PolytronEngine02 and call EnableOutline() on all children polytrons
        PolytronEngine polytronEngine = GameObject.Find("PolytronEngine02").GetComponent<PolytronEngine>();
        foreach (var polytron in polytronEngine.GetAllPolytrons())
        {
            var polytronOutline = polytron.GetComponentInChildren<Outline>();
            if (polytronOutline != null)
            {
                polytronOutline.EnableOutline();
            }
        }

        Polytron p = polytronEngine.GetAllPolytrons()[0];
        Canvas canvas = p.GetComponentInChildren<Canvas>();

        string s = $"polytron name: {p.name}\n"
            + $"canvas name: {canvas.name}\n";



        titleText.text = s;


    }


    /*
    bool IsVRDeviceConnected()
    {
        return SpatialBridge.deviceService.isHeadsetPresent
            || SpatialBridge.deviceService.isController1Present
            || SpatialBridge.deviceService.isController2Present;
    }
    */

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
