using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;
using System.Linq;

/// <summary>
/// Displays an info panel for a Polytron. Pops up when the player is near, collapses when far.
/// Uses TextMeshPro for text.
/// </summary>
public class PolytronInfoPanel : MonoBehaviour
{
    // [Header("Panel Settings")]

    // disable this
    float showDistance = 0f;


    float animationDuration = 0.5f;
    // Vector3 panelOffset = new Vector3(0, 0.5f, 0);

    [Header("References")]
    public Canvas canvasComponent;

    Transform panelTransform;

    TextMeshProUGUI headerText_;
    TextMeshProUGUI bodyText_;
    Button centerButton;
    TextMeshProUGUI centerButtonText_;
    Button button1;
    TextMeshProUGUI button1Text_;
    Button button2;
    TextMeshProUGUI button2Text_;
    Button button3;
    TextMeshProUGUI button3Text_;
    Button button4;
    TextMeshProUGUI button4Text_;

    MutatronEngine mutatron;

    // Internal string properties that wrap the TextMeshProUGUI.text fields.
    // Setting these will update the underlying UI text and call UpdatePanelGUI().
    string headerText
    {
        get => headerText_ != null ? headerText_.text : "";
        set
        {
            if (headerText_ != null) headerText_.text = value;
            UpdatePanelGUI();
        }
    }

    string bodyText
    {
        get => bodyText_ != null ? bodyText_.text : "";
        set
        {
            if (bodyText_ != null) bodyText_.text = value;
            UpdatePanelGUI();
        }
    }

    string centerButtonText
    {
        get => centerButtonText_ != null ? centerButtonText_.text : "";
        set
        {
            if (centerButtonText_ != null) centerButtonText_.text = value;
            UpdatePanelGUI();
        }
    }

    string button1Text
    {
        get => button1Text_ != null ? button1Text_.text : "";
        set
        {
            if (button1Text_ != null) button1Text_.text = value;
            UpdatePanelGUI();
        }
    }

    string button2Text
    {
        get => button2Text_ != null ? button2Text_.text : "";
        set
        {
            if (button2Text_ != null) button2Text_.text = value;
            UpdatePanelGUI();
        }
    }

    string button3Text
    {
        get => button3Text_ != null ? button3Text_.text : "";
        set
        {
            if (button3Text_ != null) button3Text_.text = value;
            UpdatePanelGUI();
        }
    }

    string button4Text
    {
        get => button4Text_ != null ? button4Text_.text : "";
        set
        {
            if (button4Text_ != null) button4Text_.text = value;
            UpdatePanelGUI();
        }
    }

    Transform cameraTransform;

    private bool isVisible = false;

    private bool mustActivate = false;
    private Coroutine animCoroutine;

    IPolytronStateProvider provider;
    MutatronEngine providerEngine;
    Polytron myPolytron;
    // if state arrives before UI is built, keep it here and apply after UI creation
    private PolytronState? pendingState = null;

    void Start()
    {
        cameraTransform = CrossPlatformUtils.FindCamera().transform;

        mutatron = FindObjectOfType<MutatronEngine>();
        Debug.Assert(mutatron != null, "MutatronEngine not found in scene, please check");

        myPolytron = GetComponent<Polytron>();

        // Note: defer provider subscription until after the panel UI is created below.
        // we'll look it up later and subscribe once header/body/button references exist.

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

            //             Debug.Assert(false);

            panelTransform = canvasComponent.transform.Find("Panel");
            Debug.Assert(panelTransform != null);

            headerText_ = panelTransform.Find("HeaderText").GetComponent<TextMeshProUGUI>();
            Debug.Assert(headerText_ != null);
            bodyText_ = panelTransform.transform.Find("BodyText").GetComponent<TextMeshProUGUI>();
            centerButton = panelTransform.transform.Find("CenterButton").GetComponent<Button>();
            centerButtonText_ = centerButton.GetComponentInChildren<TextMeshProUGUI>();
            button1 = panelTransform.transform.Find("Button1").GetComponent<Button>();
            button1Text_ = button1.GetComponentInChildren<TextMeshProUGUI>();
            button2 = panelTransform.transform.Find("Button2").GetComponent<Button>();
            button2Text_ = button2.GetComponentInChildren<TextMeshProUGUI>();
            button3 = panelTransform.transform.Find("Button3").GetComponent<Button>();
            button3Text_ = button3.GetComponentInChildren<TextMeshProUGUI>();
            button4 = panelTransform.transform.Find("Button4").GetComponent<Button>();
            button4Text_ = button4.GetComponentInChildren<TextMeshProUGUI>();

            ResetTexts();

            Polytron p = GetComponent<Polytron>();

            /*
                       headerText_.text = (p.sealNumber + 1) + " - " + p.sealName;
                       bodyText_.text = $"{PolytronName.GetPeriodString(p.sealNumber)}";
            */

            UpdatePanelGUI();

            // Hook up button handlers so presses are forwarded to this GameObject (other components can implement handlers)
            if (centerButton != null) centerButton.onClick.AddListener(OnCenterButtonPressed);
            if (button1 != null) button1.onClick.AddListener(() => OnButtonPressed(1));
            if (button2 != null) button2.onClick.AddListener(() => OnButtonPressed(2));
            if (button3 != null) button3.onClick.AddListener(() => OnButtonPressed(3));
            if (button4 != null) button4.onClick.AddListener(() => OnButtonPressed(4));
        }

        // Now that UI refs are created, find provider and subscribe safely.
        provider = FindObjectsOfType<MutatronEngine>().OfType<IPolytronStateProvider>().FirstOrDefault();
        providerEngine = provider as MutatronEngine;
        if (providerEngine != null)
        {
            providerEngine.OnPolytronStateChanged += OnPolytronStateChanged;
            // initialize panel with authoritative state
            var st = providerEngine.ComputeState(myPolytron);
            ApplyStateToPanel(st);
        }

        // If a state arrived before UI was ready, apply it now
        if (pendingState.HasValue)
        {
            ApplyStateToPanel(pendingState.Value);
            pendingState = null;
        }

        canvasComponent.gameObject.SetActive(false);
        canvasComponent.transform.localScale = Vector3.zero;
    }

    void OnDestroy()
    {
        if (providerEngine != null)
            providerEngine.OnPolytronStateChanged -= OnPolytronStateChanged;
    }

    // handler invoked by MutatronEngine when a polytron state changes
    void OnPolytronStateChanged(Polytron p, PolytronState state)
    {
        if (p != myPolytron) return;
        ApplyStateToPanel(state);
    }

    void UpdateTextsAndButtons()
    {
        // keep a fallback/polling path for safety if no provider is present
        PolytronState state;
        if (provider != null)
        {
            state = provider.ComputeState(myPolytron);
        }
        else
        {
            state = LocalPolytronStateEvaluator.ComputeState(myPolytron);
        }
        ApplyStateToPanel(state);
    }

    // Centralized mapping of state -> visible texts and button availability.
    void ApplyStateToPanel(PolytronState state)
    {
        // Defensive: if UI is not yet initialized, store and return.
        if (headerText_ == null || bodyText_ == null)
        {
            pendingState = state;
            return;
        }


        // Header always shows name and role
        headerText_.text = (state.SealNumber + 1) + " - " + state.SealName;
 
        // Body shows location and recipe, and a short status
        string locText = state.Location switch
        {
            PolytronLocation.Home => "At Home",
            PolytronLocation.MutatronCenter => "Mutatron Center",
            PolytronLocation.Mutatron => "On Mutatron",
            PolytronLocation.Returning => "Returning",
            _ => "Unknown"
        };
 
        string roleText = state.Role == PolytronRole.Architron ? "Architron" : (state.Role == PolytronRole.GeneticFriend ? "Genetic Friend" : "Polytron");
        bodyText_.text = $"Role: {roleText}\nLocation: {locText}\nRecipe: {state.Recipe}";
 
 /*
        // Center button: available when on Mutatron to "focus" / center camera (example)
        if (centerButton != null)
        {
            if (state.Location == PolytronLocation.Mutatron || state.Location == PolytronLocation.MutatronCenter)
            {
                centerButton.gameObject.SetActive(true);
                centerButtonText_.text = "Focus";
            }
            else
            {
                centerButton.gameObject.SetActive(false);
            }
        }
 
        // Button 4: Make Architron (only if not already architron and interactive and on mutatron)
        if (button4 != null)
        {
            if (state.Role != PolytronRole.Architron && state.Interactive && (state.Location == PolytronLocation.Mutatron || state.Location == PolytronLocation.Home))
            {
                button4.gameObject.SetActive(true);
                button4Text_.text = "Make Architron";
            }
            else
            {
                button4.gameObject.SetActive(false);
            }
        }
 
        // Button1..3: example: quick actions depend on state
        if (button1 != null)
        {
            // Example: if not interactive, hide action buttons
            if (!state.Interactive)
            {
                button1.gameObject.SetActive(false);
                button2.gameObject.SetActive(false);
                button3.gameObject.SetActive(false);
            }
            else
            {
                button1.gameObject.SetActive(true);
                button1Text_.text = "Teleport Home";
                button2.gameObject.SetActive(true);
                button2Text_.text = "Send to Mutatron";
                button3.gameObject.SetActive(true);
                button3Text_.text = "Inspect";
            }
        }
 
        */


        // Button 4: Make Architron (only if not already architron and interactive and on mutatron)
        if (button4 != null)
        {
            if (state.Role != PolytronRole.Architron
                && state.Interactive
                && (state.Location == PolytronLocation.Mutatron || state.Location == PolytronLocation.Home))
            {
                //                 button4.gameObject.SetActive(true);
                button4Text = "Make Architron";
            }
            else
            {
                //                 button4.gameObject.SetActive(false);
                button4Text = "";
            }
        }



        UpdatePanelGUI();
    }

    void TestPanelFull()
    {
        headerText_.text = "headerText";
        bodyText_.text = "bodytext bello lungo e che probabilmente va anche a capo, qui ci si può ragionare";
        centerButtonText_.text = "center button";
        button1Text_.text = "button1 text";
        button2Text_.text = "button2 text";
        button3Text_.text = "button3 text";
        button4Text_.text = "button4 text";

        // Ensure UI visibility matches content
        UpdatePanelGUI();
    }

    void ResetTexts()
    {
        headerText_.text = "";
        bodyText_.text = "";
        centerButtonText_.text = "";
        button1Text_.text = "";
        button2Text_.text = "";
        button3Text_.text = "";
        button4Text_.text = "";
    }


    /// <summary>
    /// Update panel controls visibility according to whether their corresponding
    /// TextMeshProUGUI fields contain non-empty text. Call this after changing texts.
    /// </summary>
    public void UpdatePanelGUI()
    {
        bool HasText(TextMeshProUGUI t) => t != null && !string.IsNullOrWhiteSpace(t.text);

        Debug.Assert(headerText_ != null);

        headerText_.gameObject.SetActive(HasText(headerText_));
        bodyText_.gameObject.SetActive(HasText(bodyText_));
        centerButton.gameObject.SetActive(centerButtonText_ != null && HasText(centerButtonText_));
        button1.gameObject.SetActive(button1Text_ != null && HasText(button1Text_));
        button2.gameObject.SetActive(button2Text_ != null && HasText(button2Text_));
        button3.gameObject.SetActive(button3Text_ != null && HasText(button3Text_));
        button4.gameObject.SetActive(button4Text_ != null && HasText(button4Text_));
    }

    void Update()
    {
        float dist = Vector3.Distance(transform.position, CrossPlatformUtils.GetAvatarPosition());
        bool shouldShow = (dist < showDistance) || mustActivate;

        if (shouldShow != isVisible)
        {
            if (animCoroutine != null) StopCoroutine(animCoroutine);
            animCoroutine = StartCoroutine(AnimatePanel(shouldShow));
            isVisible = shouldShow;

            // eventually pause the animation
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
            canvasComponent.transform.rotation = Quaternion.LookRotation(panelTransform.position - cameraTransform.position, Vector3.up /*+ new Vector3(30f, 30f, 30f)*/);
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

    // Added handlers for button presses.
    // These will log the press and forward a message to other components on the same GameObject.
    // Other components (for example Polytron) can implement:
    //   void OnCenterButtonPressed() { ... }
    //   void OnInfoPanelButtonPressed(int buttonIndex) { ... }
    void OnCenterButtonPressed()
    {
        Debug.Log($"[PolytronInfoPanel] Center button pressed on {name}");
        // SendMessage("OnCenterButtonPressed", SendMessageOptions.DontRequireReceiver);
    }

    void OnButtonPressed(int index)
    {
        Debug.Log($"[PolytronInfoPanel] Button{index} pressed on {name}");
        // SendMessage("OnInfoPanelButtonPressed", index, SendMessageOptions.DontRequireReceiver);
        if (index == 4 && button4Text_.text == "Make Architron")
        {
            Debug.Log("Changing Architron");
            mutatron.SetNewArchitron(GetComponent<Polytron>().sealNumber);
        }
    }

    public void Activate(bool show)
    {
        //if (animCoroutine != null) StopCoroutine(animCoroutine);
        // animCoroutine = StartCoroutine(AnimatePanel(show));
        mustActivate = show;
    }
}