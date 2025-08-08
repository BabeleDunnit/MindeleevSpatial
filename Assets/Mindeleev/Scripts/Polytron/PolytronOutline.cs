using UnityEngine;

/// <summary>
/// Attach this to a Polytron GameObject to enable an outline/halo effect that can be toggled on/off.
/// </summary>
[RequireComponent(typeof(Renderer))]
public class PolytronOutline : MonoBehaviour
{
    public Color outlineColor = Color.yellow;
    public float outlineWidth = 0.05f;

    // private Material normalMaterial;
    private Material outlineMaterial;
    private Material[] originalMaterials;
    private Renderer rend;
    private bool isOutlined = false;

    void Awake()
    {
        rend = GetComponent<Renderer>();
        originalMaterials = rend.sharedMaterials;

        // Create a simple outline material (replace with your own shader/material if needed)
        // Shader outlineShader = Shader.Find("Outlined/Silhouetted Diffuse");
        //        Shader originalShader = Shader.Find("Custom/PolyhedronFlatShadedOutline");
        // Shader originalShader = Shader.Find("Unlit/Transparent Cutout");
        // Shader originalShader = Shader.Find("Skybox/Procedural");
        //  Shader originalShader = Shader.Find("Hidden/OutlineEffect");
                Shader originalShader = Shader.Find("Shader Graphs/SG_OutlineExtrude");


        if (originalShader == null)
        {
            Debug.LogWarning("Outline shader not found. Using Standard shader as fallback.");
            originalShader = Shader.Find("Standard");
        }

        outlineMaterial = new Material(originalShader);

        
        //outlineMaterial.SetColor("_OutlineColor", outlineColor);
        // outlineMaterial.SetFloat("_Outline", outlineWidth);
        outlineMaterial.SetColor("_Color", Color.green);
        outlineMaterial.SetFloat("_OutlineWidth", 0.01f);
        // outlineMaterial.SetFloat("_LineIntensity", 100f);
        
    }

    /// <summary>
    /// Enable the outline/halo effect.
    /// </summary>
    public void EnableOutline()
    {
        if (isOutlined) return;

        /*
             var mf = GetComponent<MeshFilter>();
                var mr = GetComponent<MeshRenderer>();

                if (mf && mr && outlineMaterial != null)
                {
                    GameObject outline = new GameObject("Outline");
                    outline.transform.SetParent(transform);
                    outline.transform.localPosition = Vector3.zero;
                    outline.transform.localRotation = Quaternion.identity;
                    outline.transform.localScale = outlineScaleFactor;

                    var mfCopy = outline.AddComponent<MeshFilter>();
                    var mrCopy = outline.AddComponent<MeshRenderer>();

                    mfCopy.sharedMesh = mf.sharedMesh;
                    mrCopy.material = outlineMaterial;
                }
        */

        var mats = new Material[originalMaterials.Length + 1];
        originalMaterials.CopyTo(mats, 0);
        mats[mats.Length - 1] = outlineMaterial;
        rend.materials = mats;
        isOutlined = true;

        /*
                var mats = new Material[1];
                mats[0] = outlineMaterial;
                rend.materials = mats;
                isOutlined = true;
        */

    }

    /// <summary>
    /// Disable the outline/halo effect.
    /// </summary>
    public void DisableOutline()
    {
        if (!isOutlined) return;

        rend.materials = originalMaterials;
        isOutlined = false;
    }
}

/*
public class OutlineMeshDuplicator : MonoBehaviour
{
    public Material outlineMaterial;
    public Vector3 outlineScaleFactor = new Vector3(1.02f, 1.02f, 1.02f);

    void Start()
    {
        var mf = GetComponent<MeshFilter>();
        var mr = GetComponent<MeshRenderer>();

        if (mf && mr && outlineMaterial != null)
        {
            GameObject outline = new GameObject("Outline");
            outline.transform.SetParent(transform);
            outline.transform.localPosition = Vector3.zero;
            outline.transform.localRotation = Quaternion.identity;
            outline.transform.localScale = outlineScaleFactor;

            var mfCopy = outline.AddComponent<MeshFilter>();
            var mrCopy = outline.AddComponent<MeshRenderer>();

            mfCopy.sharedMesh = mf.sharedMesh;
            mrCopy.material = outlineMaterial;
        }
    }
}
*/

