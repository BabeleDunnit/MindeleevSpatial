using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Collider))]
public class SpatialClickable3D : MonoBehaviour
{
    [Header("UI hit area (auto se nullo)")]
    public Button button;
    public float uiScale = 0.002f;
    public Vector2 hitAreaSize = new Vector2(220, 220);

    Transform cam;

    void Awake()
    {
        cam = FindSpatialCamera().transform;

        if (button == null)
        {
            // Canvas World Space
            var canvasGO = new GameObject("ClickCanvas", typeof(Canvas), typeof(GraphicRaycaster));
            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.transform.SetParent(transform, false);
            canvas.transform.localPosition = Vector3.zero;
            canvas.transform.localRotation = Quaternion.identity;
            canvas.transform.localScale = Vector3.one * uiScale;

            // Hit area (Image + Button)
            var imgGO = new GameObject("HitArea", typeof(RectTransform), typeof(Image), typeof(Button));
            imgGO.transform.SetParent(canvasGO.transform, false);
            var rt = imgGO.GetComponent<RectTransform>();
            rt.sizeDelta = hitAreaSize;

            var img = imgGO.GetComponent<Image>();
            img.color = new Color(1, 1, 1, 0.001f); // invisibile ma cliccabile
            img.raycastTarget = true;

            button = imgGO.GetComponent<Button>();
            button.onClick.AddListener(OnClicked);
        }
    }

    void LateUpdate()
    {
        if (cam != null)
        {
            // billboard “piatto” verso la camera
            var canvas = button.transform.parent;
            canvas.rotation = Quaternion.LookRotation(canvas.position - cam.position, Vector3.up);
        }
    }

    void OnClicked()
    {
        // TODO: la tua logica di click
        Debug.Log("3D object clicked via Spatial UI ray!");
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
