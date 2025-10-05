using UnityEngine;

/// <summary>
/// Attach this to a Polytron GameObject to enable an outline/halo effect that can be toggled on/off.
/// </summary>
[RequireComponent(typeof(Renderer))]
[RequireComponent(typeof(SpatialClickable3D))]
public class Outline : MonoBehaviour
{
    public Color outlineColor = Color.yellow;
    public float outlineWidth = 0.05f;

    GameObject outlineGameObject;

    // private Vector3 outlineScaleFactor = new Vector3(1.5f, 1.5f, 1.5f);

    public Material outlineMaterial;
    private Material[] originalMaterials;
    private Renderer rend;
    private bool isOutlined = false;

    void Awake()
    {
        rend = GetComponent<Renderer>();
        originalMaterials = rend.sharedMaterials;
    }

    /// <summary>
    /// Enable the outline/halo effect.
    /// </summary>
    internal void EnableOutline()
    {
        if (isOutlined) return;

        var mf = GetComponent<MeshFilter>();
        var mr = GetComponent<MeshRenderer>();

        if (mf != null && mr != null && outlineMaterial != null)
        {
            outlineGameObject = new GameObject("Outline");

            outlineGameObject.transform.SetParent(transform);
            outlineGameObject.transform.localPosition = Vector3.zero;
            outlineGameObject.transform.localRotation = Quaternion.identity;
            Vector3 outlineScaleFactor = new Vector3(outlineWidth, outlineWidth, outlineWidth);
            outlineGameObject.transform.localScale = outlineScaleFactor + new Vector3(1, 1, 1);

            var mfCopy = outlineGameObject.AddComponent<MeshFilter>();
            var mrCopy = outlineGameObject.AddComponent<MeshRenderer>();

            mfCopy.sharedMesh = mf.sharedMesh;
            mrCopy.material = new Material(outlineMaterial);
            mrCopy.material.SetColor("_OutlineColor", outlineColor);
        }

        isOutlined = true;
    }

    /// <summary>
    /// Disable the outline/halo effect.
    /// </summary>
    internal void DisableOutline()
    {
        if (!isOutlined) return;
        Destroy(outlineGameObject);
        isOutlined = false;
    }

    public void RebuildOutline()
    {
        DisableOutline();
        EnableOutline();
    }
}


