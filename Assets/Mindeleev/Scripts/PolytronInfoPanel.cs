using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

/// <summary>
/// Displays an info panel for a Polytron. Pops up when the player is near, collapses when far.
/// Uses TextMeshPro for text.
/// </summary>
public class PolytronInfoPanel : MonoBehaviour
{
    [Header("Panel Settings")]
    public float showDistance = 3.0f;
    float animationDuration = 0.5f;
    // Vector3 panelOffset = new Vector3(0, 0.5f, 0);

    [Header("References")]
    public Canvas canvas;
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
    private Coroutine animCoroutine;

    void Start()
    {

        cameraTransform = CrossPlatformUtils.FindCamera().transform;

        // Create empty panel if not assigned
        if (canvas == null)
        {
            // Debug.Assert(1 == 0);
            var canvasGO = new GameObject("PolytronInfoPanelCanvas", typeof(Canvas));
            canvasGO.transform.SetParent(transform, false);
            canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.transform.localScale = Vector3.one;

            var panelGO = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            panelGO.transform.SetParent(canvasGO.transform, false);
            var panelRT = panelGO.GetComponent<RectTransform>();
            panelRT.sizeDelta = new Vector2(4, 3);
            panelRT.transform.localPosition = new Vector3(0, 0.5f, 0);
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
        else
        {
            Transform panel = canvas.transform.Find("Panel");
            Debug.Assert(panel != null);
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

            TestPanel();
        }

        canvas.gameObject.SetActive(false);
        canvas.transform.localScale = Vector3.zero;
    }

    void TestPanel()
    {
        headerText.text = "headerText";
        bodyText.text = "bodytext bello lungo e che probabilmente va anche a capo, qui ci si può ragionare";
        centerButtonText.text = "center button";
        button1Text.text = "button1 text";
        button2Text.text = "button2 text";
        button3Text.text = "button3 text";
        button4Text.text = "button4 text";
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
            canvas.transform.rotation = Quaternion.LookRotation(canvas.transform.position - cameraTransform.position, Vector3.up);
        }

    }

    IEnumerator AnimatePanel(bool show)
    {
        canvas.gameObject.SetActive(true);
        float t = 0f;
        Vector3 startScale = canvas.transform.localScale;
        Vector3 endScale = show ? Vector3.one : Vector3.zero;

        while (t < animationDuration)
        {
            t += Time.deltaTime;
            canvas.transform.localScale = Vector3.Lerp(startScale, endScale, t / animationDuration);
            yield return null;
        }
        canvas.transform.localScale = endScale;
        if (!show) canvas.gameObject.SetActive(false);
    }
}