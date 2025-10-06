using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;


[RequireComponent(typeof(Collider))]
public class SpatialClickable3D : MonoBehaviour
{
    [Header("UI hit area (auto se nullo)")]
    public Button button;

    // canvas scale is 1 so these are meters
//     private Vector2 hitAreaSize = new Vector2(0.8f, 0.8f);
    private Vector2 hitAreaSize = new Vector2(1.8f, 1.8f);

    Transform cam;

    void Awake()
    {
        cam = CrossPlatformUtils.FindCamera().transform;

        if (button == null)
        {
            // Canvas World Space
            var canvasGO = new GameObject("ClickCanvas", typeof(Canvas));

            canvasGO.layer = LayerMask.NameToLayer("UI");

            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.transform.SetParent(transform, false);
            canvas.transform.localPosition = Vector3.zero;
            canvas.transform.localRotation = Quaternion.identity;

            canvas.transform.localScale = Vector3.one;

            // Hit area (Image + Button)
            var imgGO = new GameObject("HitArea", typeof(RectTransform), typeof(Image), typeof(Button));
            imgGO.transform.SetParent(canvasGO.transform, false);

            imgGO.layer = LayerMask.NameToLayer("UI");

            var rt = imgGO.GetComponent<RectTransform>();
            rt.sizeDelta = hitAreaSize;

            var img = imgGO.GetComponent<Image>();
            // img.color = new Color(1, 1, 1, 0.001f); // invisibile ma cliccabile
            img.color = new Color(1, 1, 0, 0.3f); // semi-transparent for debugging
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
        //WorldSpacePanel wsp = GameObject.Find("InspectorCanvas").GetComponent<WorldSpacePanel>();
        //wsp.pname.text = "cliccato su un polytrone";

        /*
                IPointerClickHandler ch = GetComponent<IPointerClickHandler>();
                ch.OnPointerClick(null);
                */

        Polytron p = GetComponent<Polytron>();
        p.OnSpatialClickable3DClick();
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
