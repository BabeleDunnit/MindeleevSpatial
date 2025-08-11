using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;


[RequireComponent(typeof(Collider))]
public class SpatialClickable3D : MonoBehaviour
{
    [Header("UI hit area (auto se nullo)")]
    public Button button;
    // private float uiScale = 1.0f;

    // canvas scale is 1 so these are meters
    private Vector2 hitAreaSize = new Vector2(2.2f, 2.2f);

    Transform cam;

    /*
    // Attach this script to your Polytron prefab
    void Start()
    {
        // Create a world space canvas if not already present
        if (GetComponentInChildren<Canvas>() == null)
        {
            GameObject canvasGO = new GameObject("PolytronCanvas");
            canvasGO.transform.SetParent(transform, false);
            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 100; // Ensure it's on top
            canvasGO.AddComponent<GraphicRaycaster>();

            RectTransform rt = canvas.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(1, 1); // Adjust as needed
            rt.localPosition = Vector3.zero;
            rt.localRotation = Quaternion.identity;

            // Add a transparent button to catch events
            GameObject btnGO = new GameObject("PolytronButton");
            btnGO.transform.SetParent(canvasGO.transform, false);
            var img = btnGO.AddComponent<UnityEngine.UI.Image>();
            img.color = new Color(1, 1, 1, 0.01f); // Almost invisible
            var btn = btnGO.AddComponent<UnityEngine.UI.Button>();
            btn.transition = UnityEngine.UI.Selectable.Transition.None;
            RectTransform btnRT = btnGO.GetComponent<RectTransform>();
            btnRT.sizeDelta = rt.sizeDelta;

            // Forward events to Polytron
            btn.onClick.AddListener(() => OnClicked());
            // For hover, use EventTrigger or custom script
        }
    }
*/



    void Awake()
    {
        cam = FindSpatialCamera().transform;

        if (button == null)
        {

            gameObject.layer = LayerMask.NameToLayer("UI");
            Debug.Log($"gameObject.layer: {gameObject.layer}");

            // Canvas World Space
            var canvasGO = new GameObject("ClickCanvas", typeof(Canvas)
            // ,typeof(GraphicRaycaster)
            );

            canvasGO.layer = LayerMask.NameToLayer("UI");

            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.transform.SetParent(transform, false);
            canvas.transform.localPosition = Vector3.zero;
            canvas.transform.localRotation = Quaternion.identity;

            canvas.transform.localScale = Vector3.one /* * uiScale */;
            // canvas.transform.localScale = Vector3.one * uiScale;
            // canvas.transform.localScale = Vector3.one * 1.0f;

            // canvas.sortingOrder = 100; // Ensure it's on top

            //gr.ignoreReversedGraphics = false;
            //gr.blockingObjects = GraphicRaycaster.BlockingObjects.All;
            //gr.blockingMask = 65535;
            // PhysicsRaycaster pr = canvasGO.AddComponent<PhysicsRaycaster>();



            // Hit area (Image + Button)
            var imgGO = new GameObject("HitArea", typeof(RectTransform), typeof(Image), typeof(Button));
            imgGO.transform.SetParent(canvasGO.transform, false);

            imgGO.layer = LayerMask.NameToLayer("UI");

            var rt = imgGO.GetComponent<RectTransform>();
            rt.sizeDelta = hitAreaSize;

            var img = imgGO.GetComponent<Image>();
            // img.color = new Color(1, 1, 1, 0.001f); // invisibile ma cliccabile
            img.color = new Color(1, 1, 0, 0.3f); // semi-transparent red for debugging
            img.raycastTarget = true;

            button = imgGO.GetComponent<Button>();
            button.onClick.AddListener(OnClicked);

            GraphicRaycaster gr = canvasGO.AddComponent<GraphicRaycaster>();

        }
    }


    void LateUpdate()
    {
        if (cam != null)
        {
            // Billboard “piatto” verso la camera
            var canvas = button.transform.parent;
            canvas.rotation = Quaternion.LookRotation(canvas.position - cam.position, Vector3.up);
            
            
                        // Move canvas slightly toward the camera to avoid being inside the 3D object
            float offset = 1f; 
                        Vector3 dirToCam = (canvas.position - cam.position).normalized;
                        // Set position relative to the object, not accumulating
                        canvas.position = transform.position - dirToCam * offset;
            
        }
    }

    void OnClicked()
    {
        // TODO: la tua logica di click
        Debug.Log("3D object clicked via Spatial UI ray!");
        WorldSpacePanel wsp = GameObject.Find("InspectorCanvas").GetComponent<WorldSpacePanel>();

        wsp.titleText.text = "cliccato su un polytrone";

        IPointerClickHandler ch = GetComponent<IPointerClickHandler>();
        ch.OnPointerClick(null);
    }

    private Camera FindSpatialCamera()
    {
        var cam = GameObject.FindGameObjectWithTag("MainCamera")?.GetComponent<Camera>();
        if (cam != null) return cam;
        return null;
    }

}
