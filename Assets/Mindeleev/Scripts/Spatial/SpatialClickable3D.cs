using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// WARNING:
// THIS COMPONENT MUST *NOT* BE USED FROM A COROUTINE
// on Oculus, there is a bug which will cause only the FIRST created canvas to work.

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

    // canvas scale is 1 so these are meters
    private Vector2 hitAreaSize = new Vector2(1f, 1f);

    Transform cameraTransform;

    internal Canvas canvasComponent;

    void Awake()
    {
        cameraTransform = CrossPlatformUtils.FindCamera().transform;

        // if (button == null)
        // {
        // Canvas World Space
        var canvasGO = new GameObject("ClickCanvas", typeof(Canvas), typeof(GraphicRaycaster));
        canvasGO.transform.SetParent(transform, false);

        canvasComponent = canvasGO.GetComponent<Canvas>();
        canvasComponent.renderMode = RenderMode.WorldSpace;
        canvasComponent.transform.localPosition = Vector3.zero;
        canvasComponent.transform.localRotation = Quaternion.identity;
        canvasComponent.transform.localScale = Vector3.one;

        // Ensure the world-space canvas has a camera assigned (some raycasters need this)
        var cam = CrossPlatformUtils.FindCamera();
        if (cam != null)
            canvasComponent.worldCamera = cam;

        // Make sure the canvas sorts above default geometry so raycasters see it first
        canvasComponent.overrideSorting = true;
        canvasComponent.sortingOrder = 100;

        // Hit area (Image + Button)
        var imgGO = new GameObject("HitArea", typeof(RectTransform), typeof(Image), typeof(Button));
        imgGO.transform.SetParent(canvasGO.transform, false);

        // After creating imgGO (the UI element)
        var proxy = imgGO.AddComponent<SpatialClickable3DProxy>();
        proxy.target = this;

        var rt = imgGO.GetComponent<RectTransform>();
        rt.sizeDelta = hitAreaSize;

        var img = imgGO.GetComponent<Image>();
        // img.color = new Color(1, 1, 1, 0.001f); // invisible
        img.color = new Color(1, 0, 0, 0.3f); // semi-transparent for debugging
        img.raycastTarget = true;

        Button button = imgGO.GetComponent<Button>();
        var colors = button.colors;
        colors.highlightedColor = Color.green;
        button.colors = colors;
    }

    public void OnProxyPointerClick(PointerEventData eventData)
    {
        Debug.Log($"OnProxyPointerClick: clicked in SpatialClickable3D via proxy, clicks: {eventData.clickCount}");

        /*
                // Forward the pointer event to any Polytron (or other) component on this GameObject so
                // higher-level logic can react to clicks (double-click, clickCount, etc.).
                Polytron p = GetComponent<Polytron>();
                if (p != null)
                {
                    p.OnSpatialClickable3DClick(eventData);
                    return;
                }
        */

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
            // Billboard toward camera
            // var canvas = button.transform.parent;
            canvasComponent.transform.rotation = Quaternion.LookRotation(canvasComponent.transform.position - cameraTransform.position, Vector3.up);

            /*            
                                    // Move canvas slightly toward the camera to avoid being inside the 3D object
                        float offset = 1f; 
                                    Vector3 dirToCam = (canvas.position - cam.position).normalized;
                                    // Set position relative to the object, not accumulating
                                    canvas.position = transform.position - dirToCam * offset;
             */
        }
    }
}
