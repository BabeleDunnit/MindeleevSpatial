using UnityEngine;
using System;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
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

    // left top
    Button button1;
    TextMeshProUGUI button1Text_;

    // left bottom
    Button button2;
    TextMeshProUGUI button2Text_;

    // right top
    Button button3;
    TextMeshProUGUI button3Text_;

    // right bottom
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
    // Track last known genetic mode state to log transitions
    private bool lastGeneticActive = false;
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
            Debug.Log($"[PolytronInfoPanel] Found providerEngine, geneticModeActive={providerEngine.geneticModeActive}");
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
        // Debug.Log($"[PolytronInfoPanel] OnPolytronStateChanged called for polytron={p.sealNumber}, geneticModeActive={(providerEngine!=null?providerEngine.geneticModeActive:false)}");
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
            PolytronLocation.FollowingAvatar => "Following Avatar",
            PolytronLocation.Returning => "Returning",
            _ => "Unknown"
        };

        string roleText;
        switch (state.Role)
        {
            case PolytronRole.Architron:
                roleText = "Architron";
                break;
            case PolytronRole.ArchitronGenetic:
                roleText = "Architron (Genetic)";
                break;
            case PolytronRole.GeneticFriend:
                roleText = "Genetic Friend";
                break;
            default:
                roleText = "Polytron";
                break;
        }

        bodyText_.text = $"Role: {roleText}\nLocation: {locText}\nRecipe: {state.Recipe}, Seed: {myPolytron.MindeleevTable.GetPolytronicNumber(state.Recipe)}, NextSeed: {myPolytron.MindeleevTable.NextMissingPolytronicNumber()}\n";

        // Show emanations count and current selection index (if any)
        int emanationCount = 0;
        int currentIndex = 0;
        if (myPolytron != null && myPolytron.MindeleevTable != null)
        {
            emanationCount = myPolytron.MindeleevTable.Count;
            currentIndex = Mathf.Clamp(myPolytron.MindeleevCursor, 0, Math.Max(0, emanationCount - 1));
        }

        // Detect genetic mode transitions and log. Also emit detailed state diagnostics
        bool geneticActive = providerEngine != null && providerEngine.geneticModeActive;
        // Debug.Log($"[PolytronInfoPanel] ApplyStateToPanel: polytron={state.SealNumber}, role={state.Role}, interactive={state.Interactive}, location={state.Location}, geneticActive={geneticActive}");
        if (geneticActive != lastGeneticActive)
        {
            // Debug.Log($"[PolytronInfoPanel] Genetic mode transition for polytron {state.SealNumber}: {lastGeneticActive} -> {geneticActive}");
            lastGeneticActive = geneticActive;
        }

        if (emanationCount > 0)
        {
            bodyText_.text += $"Emanations: {emanationCount} (showing {currentIndex + 1}/{emanationCount})\n";

            // Add score display
            float currentEmanationScore = 0f;
            var currentEmanationList = myPolytron.MindeleevTable.GetEmanationsList();
            if (currentEmanationList.Count > 0 && myPolytron.MindeleevCursor >= 0 && myPolytron.MindeleevCursor < currentEmanationList.Count)
            {
                var currentRecipe = currentEmanationList[myPolytron.MindeleevCursor];
                try
                {
                    var parsed = PolyhedronRecipeParser.Parse(currentRecipe);
                    currentEmanationScore = PolyhedronRecipeUtils.ComputeComplexity(parsed);
                }
                catch { }
            }
            bodyText_.text += $"Scores: Total: {myPolytron.totalPolytronScore:F2},";
            bodyText_.text += $"Emanation: {currentEmanationScore:F2}\n";

            // enable Prev/Next buttons via text (UpdatePanelGUI will toggle visibility)
            if (geneticActive && (state.Role == PolytronRole.Architron || state.Role == PolytronRole.ArchitronGenetic))
            {
                if (button1 != null) button1Text = "Breed";
                //Debug.Log($"[PolytronInfoPanel] Setting button1='Breed' for Architron {state.SealNumber}");
                if (button2 != null) button2Text = "";
                //Debug.Log($"[PolytronInfoPanel] Hiding button2 (Next) for Architron {state.SealNumber}");
            }
            else if (geneticActive && state.Selection != SelectionSlot.None)
            {
                // Show Swap on the selected PARENT polytrons (Palette/Operators selectors), not on ephemeral GeneticFriend children
                if (button1 != null) button1Text = "Swap";
                // Debug.Log($"[PolytronInfoPanel] Setting button1='Swap' for selected parent polytron {state.SealNumber} sel={state.Selection}");
                if (button2 != null) button2Text = "";
                //Debug.Log($"[PolytronInfoPanel] Hiding button2 (Next) for selected parent polytron {state.SealNumber}");
            }
            else
            {
                if (button1 != null) button1Text = "Prev";
                Debug.Log($"[PolytronInfoPanel] Setting button1='Prev' for polytron {state.SealNumber}");
                if (button2 != null) button2Text = "Next";
                Debug.Log($"[PolytronInfoPanel] Setting button2='Next' for polytron {state.SealNumber}");
            }

            // Button3: Entangle (only for Architron in normal mode)
            if (!geneticActive && state.Role == PolytronRole.Architron)
            {
                if (button3 != null) button3Text = "Entangle";
                Debug.Log($"[PolytronInfoPanel] Setting button3='Entangle' for Architron {state.SealNumber}");
            }
            else
            {
                if (button3 != null) button3Text = "";
            }
        }
        else
        {
            if (button1 != null) button1Text = "";
            if (button2 != null) button2Text = "";
        }

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
                   if (state.Role != PolytronRole.Architron && state.Role != PolytronRole.ArchitronGenetic && state.Interactive && (state.Location == PolytronLocation.Mutatron || state.Location == PolytronLocation.Home))
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
        // During genetic mode we hide this action to avoid conflicting UI with Breed/Swap.
        if (button4 != null)
        {
            if (geneticActive)
            {
                button4Text = ""; // hide during genetics
                // Debug.Log($"[PolytronInfoPanel] Hiding Make Architron for polytron {state.SealNumber} due to genetic mode");
            }
            else
            {
                if (state.Role != PolytronRole.Architron && state.Role != PolytronRole.ArchitronGenetic
                    && state.Interactive
                    && (state.Location == PolytronLocation.Mutatron || state.Location == PolytronLocation.Home))
                {
                    button4Text = "Make Architron";
                    // Debug.Log($"[PolytronInfoPanel] Showing Make Architron for polytron {state.SealNumber}");
                }
                else
                {
                    button4Text = "";
                }
            }
        }



        // Emit diagnostic snapshot of button texts and current active states before GUI update
        try
        {
            var b1t = button1Text_ != null ? button1Text_.text : "<null>";
            var b2t = button2Text_ != null ? button2Text_.text : "<null>";
            var b3t = button3Text_ != null ? button3Text_.text : "<null>";
            var b4t = button4Text_ != null ? button4Text_.text : "<null>";
            bool b1Has = button1Text_ != null && !string.IsNullOrWhiteSpace(button1Text_.text);
            bool b2Has = button2Text_ != null && !string.IsNullOrWhiteSpace(button2Text_.text);
            bool b4Has = button4Text_ != null && !string.IsNullOrWhiteSpace(button4Text_.text);
            bool b1ActiveBefore = button1 != null ? button1.gameObject.activeSelf : false;
            bool b2ActiveBefore = button2 != null ? button2.gameObject.activeSelf : false;
            // Debug.Log($"[PolytronInfoPanel] Before UpdatePanelGUI: b1='{b1t}' has={b1Has} activeBefore={b1ActiveBefore}; b2='{b2t}' has={b2Has} activeBefore={b2ActiveBefore}; b4='{b4t}' has={b4Has}");
        }
        catch { }

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
        // Diagnostic: log HasText results before toggling
        try
        {
            bool h_header = HasText(headerText_);
            bool h_body = HasText(bodyText_);
            bool h_center = centerButtonText_ != null && HasText(centerButtonText_);
            bool h_b1 = button1Text_ != null && HasText(button1Text_);
            bool h_b2 = button2Text_ != null && HasText(button2Text_);
            bool h_b3 = button3Text_ != null && HasText(button3Text_);
            bool h_b4 = button4Text_ != null && HasText(button4Text_);
            // Debug.Log($"[PolytronInfoPanel] UpdatePanelGUI pre: header={h_header} body={h_body} center={h_center} b1={h_b1} b2={h_b2} b3={h_b3} b4={h_b4}");
        }
        catch { }

        headerText_.gameObject.SetActive(HasText(headerText_));
        bodyText_.gameObject.SetActive(HasText(bodyText_));
        centerButton.gameObject.SetActive(centerButtonText_ != null && HasText(centerButtonText_));
        button1.gameObject.SetActive(button1Text_ != null && HasText(button1Text_));
        button2.gameObject.SetActive(button2Text_ != null && HasText(button2Text_));
        button3.gameObject.SetActive(button3Text_ != null && HasText(button3Text_));
        button4.gameObject.SetActive(button4Text_ != null && HasText(button4Text_));

        // Diagnostic: log activeSelf states after toggling
        try
        {
            // Debug.Log($"[PolytronInfoPanel] UpdatePanelGUI post: b1.active={button1.gameObject.activeSelf} b2.active={button2.gameObject.activeSelf} b4.active={button4.gameObject.activeSelf} b1.text='{(button1Text_!=null?button1Text_.text:"<null>")}' b2.text='{(button2Text_!=null?button2Text_.text:"<null>")}'");
        }
        catch { }
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

        // Poll the provider's global genetic flag and force a panel refresh when it changes.
        if (providerEngine != null)
        {
            bool globalGenetic = providerEngine.geneticModeActive;
            if (globalGenetic != lastGeneticActive)
            {
                Debug.Log($"[PolytronInfoPanel] Detected geneticActive change in Update for polytron {myPolytron?.sealNumber}: {lastGeneticActive} -> {globalGenetic}");
                lastGeneticActive = globalGenetic;
                PolytronState st;
                if (provider != null)
                    st = provider.ComputeState(myPolytron);
                else
                    st = LocalPolytronStateEvaluator.ComputeState(myPolytron);
                ApplyStateToPanel(st);
            }
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
        // Prev / Next emanation (buttons 1 and 2)
        if (index == 1)
        {
            // Handle genetic-mode special actions (Breed/Swap) if present
            try
            {
                var txt = button1Text_ != null ? button1Text_.text : "";
                if (txt == "Breed")
                {
                    Breed();
                    return;
                }
                else if (txt == "Swap")
                {
                    Swap();
                    return;
                }
            }
            catch { }

            CycleEmanation(-1);
            return;
        }

        if (index == 2)
        {
            // Only cycle forward if Next is visible
            try
            {
                var txt2 = button2Text_ != null ? button2Text_.text : "";
                if (txt2 == "Next")
                {
                    CycleEmanation(1);
                }
            }
            catch { }
            return;
        }

        if (index == 3)
        {
            // Entangle action for Architron (may be empty stub)
            try
            {
                Entangle();
            }
            catch { }
            return;
        }

        if (index == 4 && button4Text_ != null && button4Text_.text == "Make Architron")
        {
            Debug.Log("Changing Architron");
            mutatron.SetNewArchitron(GetComponent<Polytron>().sealNumber);
        }
    }

    void CycleEmanation(int delta)
    {
        if (myPolytron == null || myPolytron.MindeleevTable == null || providerEngine == null) return;
        var list = myPolytron.MindeleevTable.GetEmanationsList();
        if (list == null || list.Count == 0) return;

        int count = list.Count;
        int idx = myPolytron.MindeleevCursor;
        idx = ((idx + delta) % count + count) % count; // wrap
        myPolytron.MindeleevCursor = idx;

        string recipe = list[idx];
        Debug.Log($"[PolytronInfoPanel] Cycling emanation for polytron_id={myPolytron.sealNumber} to index={idx} recipe={recipe}");
        // Apply selected recipe directly on the Polytron and rebuild immediately.
        // We avoid calling the engine helper since it refuses recipe changes
        // for polytrons currently bound on the Mutatron. The UI intent is to
        // preview/search emanations so apply the recipe and notify the engine.
        try
        {
            // Capture previous mesh id to detect no-op rebuilds
            var mf = myPolytron.GetComponent<MeshFilter>();
            int? prevMeshId = null;
            try { prevMeshId = mf?.mesh != null ? (int?)mf.mesh.GetInstanceID() : null; } catch { prevMeshId = null; }

            myPolytron.recipe = recipe;
            myPolytron.RebuildMesh();
            myPolytron.GetComponent<PointerOutlineStateController>().OnHoverEnter();

            int? newMeshId = null;
            try { newMeshId = mf?.mesh != null ? (int?)mf.mesh.GetInstanceID() : null; } catch { newMeshId = null; }

            if (prevMeshId.HasValue && newMeshId.HasValue && prevMeshId.Value == newMeshId.Value)
            {
                Debug.LogWarning($"[PolytronInfoPanel] Rebuild resulted in same mesh id={newMeshId} for polytron_id={myPolytron.sealNumber}; visible recipe may be unchanged");
            }

            // myPolytron.AddEmanation(recipe);
            // Ensure cursor points to selected index (defensive)
            myPolytron.MindeleevCursor = idx;
            // Rewrite the bound tile's emanation from this polytron emanation
            try
            {
                RewriteTileEmanationFromRecipe(recipe);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[PolytronInfoPanel] RewriteTileEmanationFromRecipe failed: {ex}");
            }

            providerEngine.NotifyPolytronStateChanged(myPolytron);

            // Update score display after emanation change
            UpdateEmanationScoreDisplay();
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[PolytronInfoPanel] Failed to apply emanation recipe: {ex}");
        }
    }

    // Helper that rewrites the tile polytronicNumber from a polytron recipe string
    void RewriteTileEmanationFromRecipe(string recipe)
    {
        if (myPolytron == null) return;
        if (string.IsNullOrEmpty(recipe)) return;

        int seed = PolyhedronRecipeKabbalah.RecipeToInt(recipe, false);
        if (seed < 0)
        {
            Debug.LogWarning($"[PolytronInfoPanel] RewriteTileEmanationFromRecipe: invalid seed for recipe '{recipe}'");
            return;
        }

        var sink = myPolytron.boundSink;
        if (sink == null)
        {
            Debug.LogWarning($"[PolytronInfoPanel] RewriteTileEmanationFromRecipe: polytron {myPolytron.sealNumber} not bound to a sink");
            return;
        }

        var coord = sink.hexCoord;
        if (providerEngine == null)
        {
            Debug.LogWarning("[PolytronInfoPanel] RewriteTileEmanationFromRecipe: providerEngine not set");
            return;
        }

        if (!providerEngine.gridCellsMap.TryGetValue(coord, out var cell))
        {
            Debug.LogWarning($"[PolytronInfoPanel] RewriteTileEmanationFromRecipe: no cell for coord {coord}");
            return;
        }

        cell.polytronicNumber = seed;
        string newTileRecipe = PolyhedronRecipeKabbalah.IntToOperatorsSequence(seed) + cell.tileBasePolyhedron;
        Debug.Log($"[PolytronInfoPanel] RewriteTileEmanationFromRecipe: setting polytronicNumber={seed} at ring={cell.ring} idx={cell.idxInRing} recipe={newTileRecipe}");
        providerEngine.RebuildTileMesh(coord, newTileRecipe);
    }

    /// <summary>
    /// Updates and displays the current emanation score and total polytron score in the UI.
    /// </summary>
    private void UpdateEmanationScoreDisplay()
    {
        if (myPolytron == null)
            return;
        // Refresh the panel display to show updated scores
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

    // Stub invoked when Breed button is pressed on Architron during genetic selection
    public void Breed()
    {
        // intentionally empty: wiring point for genetic breed action
    }

    // Stub invoked when Swap button is pressed on selected genetic friends
    public void Swap()
    {
        // intentionally empty: wiring point for swap action
    }

    // Stub invoked when Entangle button is pressed on Architron in normal mode
    public void Entangle()
    {
        // intentionally empty: entangle is now automatic via MutatronEngine.NotifyPolytronStateChanged
    }

    // Entangle toggle state and matched list
    private bool entangleActive = false;
    private List<Polytron> entangledPolytrons = new List<Polytron>();

    // Activate or deactivate the panel externally
    public void Activate(bool show)
    {
        mustActivate = show;
    }

}