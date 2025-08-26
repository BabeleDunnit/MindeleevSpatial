using UnityEngine;
using System;

public class PolytronCannon : MonoBehaviour
{
    public float shootForce = 500f;

    // private PolytronsFactory factory;

    private float lastClickTime = 0f;
    private const float doubleClickThreshold = 0.3f;

    void Start()
    {
        /*
        // Find sibling PolytronsFactory component
        factory = GetComponentInParent<PolytronsFactory>();
        if (factory == null)
        {
            factory = GetComponent<PolytronsFactory>();
        }
        if (factory == null)
        {
            Debug.LogError("PolytronCannon: No PolytronsFactory found!");
        }
        */

    }

    void OnMouseDown()
    {
        float timeSinceLastClick = Time.time - lastClickTime;
        lastClickTime = Time.time;

        if (timeSinceLastClick <= doubleClickThreshold)
        {
            HandleDoubleClick();
        }
        else
        {
            HandleClick();
        }
    }

    private void HandleClick()
    {
        ShootPolytron();
    }

    private void HandleDoubleClick()
    {
        ShootPolytron();
    }

    private void ShootPolytron()
    {
        // if (factory == null) return;

        GameObject polytron = PolytronsFactory.Instance.Create("polytron");
        if (polytron == null)
        {
            Debug.LogWarning("PolytronCannon: PolytronsFactory.Create returned null.");
            return;
        }

        Rigidbody rb = polytron.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = polytron.AddComponent<Rigidbody>();
        }
        rb.drag = 0.2f;
        rb.AddForce(transform.forward * shootForce);

        PolytronCollisionHandler ca = polytron.GetComponent<PolytronCollisionHandler>();
        if (ca == null)
        {
            ca = polytron.AddComponent<PolytronCollisionHandler>();
        }

        polytron.transform.position += new Vector3(0, 2, 0);
    }
}