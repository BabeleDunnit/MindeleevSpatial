using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;


public class SpatialClickable3DProxy : MonoBehaviour, IPointerClickHandler
{
    public SpatialClickable3D target;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (target != null)
            target.OnProxyPointerClick(eventData);
    }
}

[RequireComponent(typeof(Collider))]
public class SpatialClickable3D : MonoBehaviour
{
    // [Header("UI hit area (auto se nullo)")]
    // public Button button;

    // canvas scale is 1 so these are meters
//     private Vector2 hitAreaSize = new Vector2(0.8f, 0.8f);
    Vector2 hitAreaSize = new Vector2(3.8f, 3.8f);

    Transform cameraTransform;

    internal Canvas canvas;

    void Awake()
    {
        cameraTransform = CrossPlatformUtils.FindCamera().transform;

        // if (button == null)
        // {
        // Canvas World Space
        var canvasGO = new GameObject("ClickCanvas", typeof(Canvas), typeof(GraphicRaycaster));

        // Try to put canvas on the UI layer so external raycasters that filter by layer can hit it
        /*
        int uiLayer = LayerMask.NameToLayer("UI");
        if (uiLayer >= 0)
            canvasGO.layer = uiLayer;
*/

            // GraphicRaycaster gr = canvasGO.AddComponent<GraphicRaycaster>();

        canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.transform.SetParent(transform, false);
            canvas.transform.localPosition = Vector3.zero;
            canvas.transform.localRotation = Quaternion.identity;

            canvas.transform.localScale = Vector3.one;

            // Ensure the world-space canvas has a camera assigned (some raycasters need this)
            var cam = CrossPlatformUtils.FindCamera();
            if (cam != null)
                canvas.worldCamera = cam;

            // Make sure the canvas sorts above default geometry so raycasters see it first
            canvas.overrideSorting = true;
            canvas.sortingOrder = 100;

            // Hit area (Image + Button)
            var imgGO = new GameObject("HitArea", typeof(RectTransform), typeof(Image), typeof(Button));
            imgGO.transform.SetParent(canvasGO.transform, false);

        /*
                // Put hit area on UI layer as well (if available)
                if (uiLayer >= 0)
                    imgGO.layer = uiLayer;
        */

        // After creating imgGO (the UI element)
        var proxy = imgGO.AddComponent<SpatialClickable3DProxy>();
            proxy.target = this;

            var rt = imgGO.GetComponent<RectTransform>();
            rt.sizeDelta = hitAreaSize;

            var img = imgGO.GetComponent<Image>();
            // img.color = new Color(1, 1, 1, 0.001f); // invisibile ma cliccabile
            img.color = new Color(1, 0, 0, 0.3f); // semi-transparent for debugging
            img.raycastTarget = true;

        Button button = imgGO.GetComponent<Button>();
        var colors = button.colors;
        colors.highlightedColor = Color.green;
        button.colors = colors;

            // button.onClick.AddListener(OnClicked);

        // }
    }

    public void OnProxyPointerClick(PointerEventData eventData)
    {
        Debug.Log($"OnProxyPointerClick: clicked in SpatialClickable3D via proxy, clicks: {eventData.clickCount}");

        // Forward the pointer event to any Polytron (or other) component on this GameObject so
        // higher-level logic can react to clicks (double-click, clickCount, etc.).
        Polytron p = GetComponent<Polytron>();
        if (p != null)
        {
            p.OnSpatialClickable3DClick(eventData);
            return;
        }

        // Fallback: if another IPointerClickHandler is present on this GameObject, try to call it.
        var handlers = GetComponents<IPointerClickHandler>();
        foreach (var h in handlers)
        {
            if (System.Object.ReferenceEquals(h, this)) continue;
            h.OnPointerClick(eventData);
        }
    
}

/*
 public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log("clicked in SpatialLickable3D");
        // Forward click count and other info to Polytron
        Polytron p = GetComponent<Polytron>();
        if (p != null)
        {
            p.OnSpatialClickable3DClick(eventData);
        }
    }
*/

    void LateUpdate()
    {
        if (cameraTransform != null)
        {
            // Billboard “piatto” verso la camera
            // var canvas = button.transform.parent;
            canvas.transform.rotation = Quaternion.LookRotation(canvas.transform.position - cameraTransform.position, Vector3.up);

            /*            
                                    // Move canvas slightly toward the camera to avoid being inside the 3D object
                        float offset = 1f; 
                                    Vector3 dirToCam = (canvas.position - cam.position).normalized;
                                    // Set position relative to the object, not accumulating
                                    canvas.position = transform.position - dirToCam * offset;
             */
        }
    }

    void OnClicked()
    {
        Debug.Log("3D object clicked via Spatial UI ray!");
        WorldSpacePanel wsp = GameObject.Find("InspectorCanvas").GetComponent<WorldSpacePanel>();
        wsp.pname.text = "cliccato su un polytrone";

        /*
                IPointerClickHandler ch = GetComponent<IPointerClickHandler>();
                ch.OnPointerClick(null);
                */

        Polytron p = GetComponent<Polytron>();
        // p.OnSpatialClickable3DClick();
    }

    /*
        private Camera FindSpatialCamera()
        {
            var cam = GameObject.FindGameObjectWithTag("MainCamera")?.GetComponent<Camera>();
            if (cam != null) return cam;
            return null;
        }
    */

}
