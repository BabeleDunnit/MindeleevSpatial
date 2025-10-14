using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;
using Unity.VisualScripting;

/// <summary>
/// Displays an info panel for a Polytron. Pops up when the player is near, collapses when far.
/// Uses TextMeshPro for text.
/// </summary>
public class PolytronInfoPanel : MonoBehaviour
{
    // [Header("Panel Settings")]
    float showDistance = 1.5f;
    float animationDuration = 0.5f;
    // Vector3 panelOffset = new Vector3(0, 0.5f, 0);

    [Header("References")]
    public Canvas canvasComponent;
    TextMeshProUGUI headerText;
    TextMeshProUGUI bodyText;
    Button centerButton;
    TextMeshProUGUI centerButtonText;
    Button button1;
    TextMeshProUGUI button1Text;
    Button button2;
    TextMeshProUGUI button2Text;
    Button button3;
    TextMeshProUGUI button3Text;
    Button button4;
    TextMeshProUGUI button4Text;


    Transform cameraTransform;

    private bool isVisible = false;

    private bool mustActivate = false;
    private Coroutine animCoroutine;

    void Start()
    {
        cameraTransform = CrossPlatformUtils.FindCamera().transform;

        // Create empty panel if not assigned
        if (canvasComponent == null)
        {
            // Debug.Assert(1 == 0);
            var canvasGO = new GameObject("PolytronInfoPanelCanvas", typeof(Canvas), typeof(GraphicRaycaster));
            canvasGO.transform.SetParent(transform, false);

            canvasComponent = canvasGO.GetComponent<Canvas>();
            canvasComponent.renderMode = RenderMode.WorldSpace;
            canvasComponent.transform.localScale = Vector3.one;

            // Ensure the world-space canvas has a camera assigned (some raycasters need this)
            var cam = CrossPlatformUtils.FindCamera();
            if (cam != null)
                canvasComponent.worldCamera = cam;

            // Make sure the canvas sorts above default geometry so raycasters see it first
            canvasComponent.overrideSorting = true;
            canvasComponent.sortingOrder = 100;

            var panelGO = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            panelGO.transform.SetParent(canvasGO.transform, false);

            var panelRT = panelGO.GetComponent<RectTransform>();
            panelRT.sizeDelta = new Vector2(5, 4);
            panelRT.transform.localPosition = new Vector3(0, 1.5f, 0);

            var img = panelGO.GetComponent<Image>();
            // img.color = new Color(1, 1, 1, 0.001f); // invisible
            img.color = new Color(0.1f, 0.1f, 0.2f, 0.8f); // semi-transparent for debugging
            img.raycastTarget = true;

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
        else
        {
            canvasComponent.gameObject.AddComponent<GraphicRaycaster>();

            Transform panel = canvasComponent.transform.Find("Panel");
            Debug.Assert(panel != null);

            panel.GetComponent<Image>().raycastTarget = true;


            headerText = panel.Find("HeaderText").GetComponent<TextMeshProUGUI>();
            bodyText = panel.transform.Find("BodyText").GetComponent<TextMeshProUGUI>();
            centerButton = panel.transform.Find("CenterButton").GetComponent<Button>();
            centerButtonText = centerButton.GetComponentInChildren<TextMeshProUGUI>();
            button1 = panel.transform.Find("Button1").GetComponent<Button>();
            button1Text = button1.GetComponentInChildren<TextMeshProUGUI>();
            button2 = panel.transform.Find("Button2").GetComponent<Button>();
            button2Text = button2.GetComponentInChildren<TextMeshProUGUI>();
            button3 = panel.transform.Find("Button3").GetComponent<Button>();
            button3Text = button3.GetComponentInChildren<TextMeshProUGUI>();
            button4 = panel.transform.Find("Button4").GetComponent<Button>();
            button4Text = button4.GetComponentInChildren<TextMeshProUGUI>();

            button4.GetComponent<Image>().raycastTarget = true;

            // ResetPanel();

            Polytron p = GetComponent<Polytron>();

            headerText.text = p.sealName;
            bodyText.text = $"{PolytronName.GetPeriodString(p.sealNumber)}";
            button4Text.text = "Make Architron";
            UpdatePanelGUI();
        }

        canvasComponent.gameObject.SetActive(false);
        canvasComponent.transform.localScale = Vector3.zero;
    }

    void TestPanelFull()
    {
        headerText.text = "headerText";
        bodyText.text = "bodytext bello lungo e che probabilmente va anche a capo, qui ci si può ragionare";
        centerButtonText.text = "center button";
        button1Text.text = "button1 text";
        button2Text.text = "button2 text";
        button3Text.text = "button3 text";
        button4Text.text = "button4 text";

        // Ensure UI visibility matches content
        UpdatePanelGUI();
    }

    void ResetPanel()
    {
        headerText.text = "";
        bodyText.text = "";
        centerButtonText.text = "";
        button1Text.text = "";
        button2Text.text = "";
        button3Text.text = "";
        button4Text.text = "";

        // Ensure UI visibility matches content
        // UpdatePanelGUI();
    }


    /// <summary>
    /// Update panel controls visibility according to whether their corresponding
    /// TextMeshProUGUI fields contain non-empty text. Call this after changing texts.
    /// </summary>
    public void UpdatePanelGUI()
    {
        bool HasText(TextMeshProUGUI t) => t != null && !string.IsNullOrWhiteSpace(t.text);

        headerText.gameObject.SetActive(HasText(headerText));
        bodyText.gameObject.SetActive(HasText(bodyText));
        centerButton.gameObject.SetActive(centerButtonText != null && HasText(centerButtonText));
        button1.gameObject.SetActive(button1Text != null && HasText(button1Text));
        button2.gameObject.SetActive(button2Text != null && HasText(button2Text));
        button3.gameObject.SetActive(button3Text != null && HasText(button3Text));
        button4.gameObject.SetActive(button4Text != null && HasText(button4Text));
    }

    void Update()
    {
        float dist = Vector3.Distance(transform.position, CrossPlatformUtils.GetAvatarPosition());
        bool shouldShow = (dist < showDistance) || mustActivate;
        //         bool shouldShow = mustActivate;

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
            canvasComponent.transform.rotation = Quaternion.LookRotation(canvasComponent.transform.position - cameraTransform.position, Vector3.up);
        }

    }

    IEnumerator AnimatePanel(bool show)
    {
        canvasComponent.gameObject.SetActive(true);
        float t = 0f;
        Vector3 startScale = canvasComponent.transform.localScale;
        Vector3 endScale = show ? Vector3.one : Vector3.zero;

        while (t < animationDuration)
        {
            t += Time.deltaTime;
            canvasComponent.transform.localScale = Vector3.Lerp(startScale, endScale, t / animationDuration);
            yield return null;
        }
        canvasComponent.transform.localScale = endScale;
        if (!show) canvasComponent.gameObject.SetActive(false);
    }

    public void Activate(bool show)
    {
        //if (animCoroutine != null) StopCoroutine(animCoroutine);
        // animCoroutine = StartCoroutine(AnimatePanel(show));
        mustActivate = show;
    }
}