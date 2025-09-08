using UnityEngine;

public static class MaterialUtils
{
    /// <summary>
    /// Imposta il colore di un GameObject convertendo HSV → RGB
    /// </summary>
    /// <param name="go">Il GameObject con MeshRenderer</param>
    /// <param name="h">Hue [0..1]</param>
    /// <param name="s">Saturazione [0..1]</param>
    /// <param name="v">Luminosità/Valore [0..1]</param>
    public static void SetMaterialHSV(GameObject go, float h, float s, float v)
    {
        var mr = go.GetComponent<MeshRenderer>();
        if (mr == null) return;

        // calcola nuovo colore
        Color newColor = Color.HSVToRGB(h, s, v);

        // crea una copia runtime del materiale (così non modifichi l'asset globale)
        mr.material.color = newColor;
    }
}
