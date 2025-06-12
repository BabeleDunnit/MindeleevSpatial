using UnityEngine;
using System.Linq;
using TMPro;  // Add this line for TextMeshPro

public class PolyhedraGridGenerator : MonoBehaviour
{
    [Header("Grid Settings")]
    public int maxRepetitions = 1;
    public float gridSpacing = 3.0f;
    public float polyhedronScale = 0.5f;  // Add scale factor

    public bool showConwayRecipe = true;

    [Header("Prefab")]
    public GameObject polytronPrefab;


    private readonly char[] basePolyhedra = { 'C', 'T', 'O', 'D', 'I' };
    // private readonly string[] operators = { "", "dkkkd", "dkk2kd", "k(3,.5)", "k" };  // Added empty string for null operation
//     private readonly string[] operators = { "", "k(3,.5)", "k" };  // Added empty string for null operation
    private readonly string[] operators = { "", "tk", "t3k2t" };  // Added empty string for null operation

    void Start()
    {
        if (polytronPrefab == null)
        {
            Debug.LogError("Polytron prefab not assigned!");
            return;
        }

        GenerateGrid();
    }

    void GenerateGrid()
    {
        // Get origin position from this GameObject
        Vector3 origin = transform.position;

        // Create a parent object for organization
        GameObject gridParent = new GameObject("PolyhedraGrid");
        gridParent.transform.parent = transform;
        gridParent.transform.position = origin;

        // Generate the grid
        for (int polyIdx = 0; polyIdx < basePolyhedra.Length; polyIdx++)
        {
            for (int opIdx = 0; opIdx < operators.Length; opIdx++)
            {
                for (int rep = 1; rep < maxRepetitions; rep++)
                {
                    // Skip repetitions for null operator (only show base polyhedron once)
                    if (operators[opIdx] == "" && rep > 1) continue;

                    // Calculate position relative to origin
                    Vector3 position = origin + new Vector3(
                        polyIdx * gridSpacing,
                        opIdx * gridSpacing,
                        rep * gridSpacing
                    );

                    // Create instance
                    GameObject instance = Instantiate(polytronPrefab, position, Quaternion.identity, gridParent.transform);
                    instance.transform.localScale = Vector3.one * polyhedronScale;  // Apply scale

                    // Generate recipe: operator repeated 'rep' times + base polyhedron
                    string recipe;
                    if (operators[opIdx] == "")
                    {
                        recipe = basePolyhedra[polyIdx].ToString();  // Just the polyhedron name
                    }
                    else
                    {
                        recipe = string.Concat(Enumerable.Repeat(operators[opIdx], rep)) + basePolyhedra[polyIdx];
                    }
                    
                    // Set name for easy identification
                    instance.name = $"Polytron_{recipe}";

                    // Configure PolyhedronGenerator
                    var generator = instance.GetComponent<PolyhedronGenerator>();
                    if (generator != null)
                    {
                        generator.polyhedronRecipe = recipe;
                    }

                    // Add text label for easier identification
                    if (showConwayRecipe)
                    {
                        CreateLabel(instance, recipe, position);
                    }
                }
            }
        }
    }

    private void CreateLabel(GameObject parent, string recipe, Vector3 position)
    {
        // Create a TextMeshPro object for the label
        GameObject label = new GameObject($"Label_{recipe}");
        label.transform.parent = parent.transform;
        label.transform.localPosition = Vector3.down * 1.5f;
        
        // Add TextMeshPro component instead of TextMesh
        TextMeshPro tmpText = label.AddComponent<TextMeshPro>();
        tmpText.text = recipe;
        tmpText.fontSize = 3;  // TMP uses different scale for font size
        tmpText.alignment = TextAlignmentOptions.Center;
        tmpText.color = Color.white;
        
        // Configure the TMP text
        tmpText.enableAutoSizing = true;
        tmpText.fontSizeMin = 1;
        tmpText.fontSizeMax = 2;
        
        // Set material and other rendering properties
        tmpText.material = new Material(Shader.Find("TextMeshPro/Mobile/Distance Field"));
        
        // Make text more readable by placing it vertically and facing forward
        label.transform.localRotation = Quaternion.identity;
        
        // Set rect transform properties
        RectTransform rectTransform = tmpText.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(2, 0.5f);  // Width and height of the text area
    
        // Optional: Add horizontal billboard script for camera facing
        // label.AddComponent<HorizontalBillboard>();
    }
}

public class HorizontalBillboard : MonoBehaviour
{
    void Update()
    {
        // Get the parent's position (polyhedron position)
        Vector3 parentPos = transform.parent.position;
        
        // Calculate direction from label to user's assumed position (parent's position with Y=0)
        Vector3 userPos = parentPos;
        userPos.y = 0;  // Keep Y at ground level for consistent upright orientation
        
        // Make label face away from user position
        Vector3 direction = (parentPos - userPos).normalized;
        
        // Create rotation that faces user but keeps Y axis up
        transform.rotation = Quaternion.LookRotation(-direction, Vector3.up);
    }
}