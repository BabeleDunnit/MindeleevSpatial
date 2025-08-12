using UnityEngine;
using TMPro;
using SpatialSys.UnitySDK;

public class AttachToHand : MonoBehaviour
{
    // public TextMeshProUGUI titleText;
    // public TextMeshProUGUI descriptionText;

    public Vector3 offset = new Vector3(0.1f, 0.05f, 0.1f);
    // public float vrScaleFactor = 0.002f;
    // public float desktopScaleFactor = 1f;

    private Transform targetHand;
    private IAvatar avatar;

    void Start()
    {
        // Ottieni l'avatar locale
        avatar = SpatialBridge.actorService.localActor.avatar;

        if (avatar != null)
        {
            if (avatar.isBodyLoaded)
            {
                SetTargetHand();
            }
            else
            {
                avatar.onAvatarLoadComplete += OnAvatarReady;
            }
        }

        // Imposta scala (verrà eventualmente aggiornata di nuovo in OnAvatarReady)
        // bool inVR = !Application.isEditor && XRDevice.isPresent;
        // transform.localScale = Vector3.one * (inVR ? vrScaleFactor : desktopScaleFactor);
    }

    void OnDestroy()
    {
        if (avatar != null)
        {
            avatar.onAvatarLoadComplete -= OnAvatarReady;
        }
    }

    private void OnAvatarReady()
    {
        SetTargetHand();
        // Ricalcola la scala se vuoi cambiarla solo quando l'avatar è pronto
        // transform.localScale = Vector3.one * vrScaleFactor;
    }

    private void SetTargetHand()
    {
        targetHand = avatar.GetAvatarBoneTransform(HumanBodyBones.LeftHand);
        if (targetHand == null)
        {
            Debug.LogWarning("LeftHand not found on avatar.");
        }
    }

    void LateUpdate()
    {
        if (targetHand == null) return;

        // Debug.Log($"targetHand pos: {targetHand.position}");

        // Posiziona il pannello rispetto alla mano
        transform.position = targetHand.position + targetHand.TransformVector(offset) ;
        transform.rotation = Quaternion.LookRotation(targetHand.forward);
    }

    /*
        public void UpdateText(string title, string description)
        {
            if (titleText != null) titleText.text = title;
            if (descriptionText != null) descriptionText.text = description;
        }
        */
    
}

