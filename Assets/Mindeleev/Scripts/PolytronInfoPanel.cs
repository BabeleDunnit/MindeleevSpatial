using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

/// <summary>
/// Displays an info panel for a Polytron. Pops up when the player is near, collapses when far.
/// The panel is positioned low, angled 45° toward the player, and follows the player.
/// Uses TextMeshPro for text.
/// </summary>
public class PolytronInfoPanel : MonoBehaviour
{
    [Header("Panel Settings")]
    public float showDistance = 3.0f;
    float animationDuration = 0.5f;
    Vector3 panelOffset = new Vector3(0, 0.5f, 0);

    [Header("References")]
    public Canvas panelCanvas = null;
    TextMeshProUGUI polytronNameText;
    Button actionButton;


    Transform cameraTransform;

    //     private Transform playerTransform;
    private bool isVisible = false;
    private Coroutine animCoroutine;

    void Start()
    {
        // Find player camera
        //         var cam = CrossPlatformUtils.FindCamera().transform;
        //         Transform cam = null;
        //         if (cam != null) playerTransform = cam.transform;

        cameraTransform = CrossPlatformUtils.FindCamera().transform;


        // Create panel if not assigned
        if (panelCanvas == null)
        {
            // Debug.Assert(1 == 0);
            Debug.Log("creo il canvas");
            var canvasGO = new GameObject("PolytronInfoPanelCanvas", typeof(Canvas));
            canvasGO.transform.SetParent(transform, false);
            panelCanvas = canvasGO.GetComponent<Canvas>();
            panelCanvas.renderMode = RenderMode.WorldSpace;
            panelCanvas.transform.localScale = Vector3.one;

            var panelGO = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            panelGO.transform.SetParent(canvasGO.transform, false);
            var panelRT = panelGO.GetComponent<RectTransform>();
            panelRT.sizeDelta = new Vector2(4, 3);
            panelRT.transform.localPosition = panelOffset;
            panelGO.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.2f, 0.8f);

            /*
                        var textGO = new GameObject("PolytronName", typeof(TextMeshProUGUI));
                        textGO.transform.SetParent(panelGO.transform, false);
                        polytronNameText = textGO.GetComponent<TextMeshProUGUI>();
                        polytronNameText.fontSize = 28;
                        polytronNameText.alignment = TextAlignmentOptions.Left;
                        polytronNameText.color = Color.white;
                        polytronNameText.rectTransform.anchoredPosition = new Vector2(20, 20);
                        polytronNameText.text = "Polytron Name";

                        var buttonGO = new GameObject("ActionButton", typeof(Button), typeof(Image));
                        buttonGO.transform.SetParent(panelGO.transform, false);
                        actionButton = buttonGO.GetComponent<Button>();
                        var buttonImg = buttonGO.GetComponent<Image>();
                        buttonImg.color = new Color(0.3f, 0.6f, 1f, 0.9f);
                        var btnRT = buttonGO.GetComponent<RectTransform>();
                        btnRT.sizeDelta = new Vector2(80, 40);
                        btnRT.anchoredPosition = new Vector2(200, -20);

                        actionButton.onClick.AddListener(OnActionButtonClicked);
                        */
        }

        panelCanvas.gameObject.SetActive(false);
        panelCanvas.transform.localScale = Vector3.zero;
    }

    void Update()
    {
        float dist = Vector3.Distance(transform.position, CrossPlatformUtils.GetAvatarPosition());
        bool shouldShow = dist < showDistance;

        if (shouldShow != isVisible)
        {
            if (animCoroutine != null) StopCoroutine(animCoroutine);
            animCoroutine = StartCoroutine(AnimatePanel(shouldShow));
            isVisible = shouldShow;
            WaveAnimation wa = GetComponent<WaveAnimation>();
            if (isVisible)
            {
                wa?.Pause(true);
            }
            else
            {                
                wa?.Pause(false);
            }
        }

        if (isVisible)
        {
            panelCanvas.transform.rotation = Quaternion.LookRotation(panelCanvas.transform.position - cameraTransform.position, Vector3.up);
        }

    }

    IEnumerator AnimatePanel(bool show)
    {
        panelCanvas.gameObject.SetActive(true);
        float t = 0f;
        Vector3 startScale = panelCanvas.transform.localScale;
        // Vector3 startScale = Vector3.zero;
        Vector3 endScale = show ? Vector3.one : Vector3.zero;

        while (t < animationDuration)
        {
            t += Time.deltaTime;
            panelCanvas.transform.localScale = Vector3.Lerp(startScale, endScale, t / animationDuration);
            yield return null;
        }
        panelCanvas.transform.localScale = endScale;
        if (!show) panelCanvas.gameObject.SetActive(false);
    }

    public void SetPolytronName(string name)
    {
        if (polytronNameText != null)
            polytronNameText.text = name;
    }

    private void OnActionButtonClicked()
    {
        Debug.Log("PolytronInfoPanel: Action button clicked.");
        // Add your logic here
    }
}