using UnityEngine;
using System.Linq;
using System.Collections.Generic;

public class PolytronOrbital : MonoBehaviour
{
    public GameObject polytronPrefab; // Assign the Polytron prefab in the inspector
    public float orbitalRadius = 2.0f;
    public float orbitalYOffset = 0.0f;


   public struct Elettrone
    {
        public int n;
        public int l;
        public int m;
        public int s;

        public string OrbitaleKey => $"{n}{OrbitalSymbol(l)}";

        private static string OrbitalSymbol(int l)
        {
            return l switch
            {
                0 => "s",
                1 => "p",
                2 => "d",
                3 => "f",
                4 => "g", // teorici
                _ => $"l{l}"
            };
        }
    }
       static int ParseOrder(string key)
    {
        // Per ordinare gli orbitali in output (es: 1s, 2s, 2p, 3s...)
        int n = int.Parse(key.Substring(0, key.Length - 1));
        char lChar = key[^1];
        int l = lChar switch
        {
            's' => 0,
            'p' => 1,
            'd' => 2,
            'f' => 3,
            'g' => 4,
            _ => 99
        };
        return (n + l) * 10 + n; // ordina come Madelung
    }

    public void GenerateAufbau()
    {
        Debug.Log("inizio Aufbau");
        int maxZ = 30; // cambia a piacere
        var elettroni = new List<Elettrone>();

        // Generazione completa
        for (int n = 1; n <= 7; n++)
        {
            for (int l = 0; l < n; l++)
            {
                for (int m = -l; m <= l; m++)
                {
                    for (int s = -1; s <= 1; s += 2)
                    {
                        elettroni.Add(new Elettrone { n = n, l = l, m = m, s = s });
                    }
                }
            }
        }

        // Ordinamento Madelung (n + l, poi n)
        var sorted = elettroni
            .OrderBy(e => e.n + e.l)
            .ThenBy(e => e.n)
            .ToList();

        // Costruzione configurazioni per ogni Z
        var config = new Dictionary<string, int>();

        for (int z = 1; z <= maxZ; z++)
        {
            var e = sorted[z - 1];
            string key = e.OrbitaleKey;

            if (!config.ContainsKey(key))
                config[key] = 0;
            config[key]++;

            // Output configurazione per Z corrente
            Debug.Log($"z: {z}");
            Debug.Log(string.Join(" ",
                config
                    .OrderBy(kvp => ParseOrder(kvp.Key)) // garantisce ordine orbitale
                    .Select(kvp => $"{kvp.Key}{kvp.Value}")
            ));
            Debug.Log("");
        }
        Debug.Log("fine aufbau");
    }

    void Start()
    {

        GenerateAufbau();


        // Get the PolyhedronGenerator on this nucleus
        var generator = GetComponent<PolyhedronGenerator>();
        if (generator == null || polytronPrefab == null)
        {
            Debug.LogWarning("PolyhedronGenerator or PolytronPrefab not assigned.");
            return;
        }

        // Extract the base polyhedron (last uppercase letter in the recipe)
        string recipe = generator.polyhedronRecipe;
        char basePoly = recipe.LastOrDefault(c => char.IsUpper(c));
        if (basePoly == default)
        {
            Debug.LogWarning("No base polyhedron found in recipe: " + recipe);
            return;
        }

        // Prepare the base recipe string
        string baseRecipe = basePoly.ToString();

        // try to clone the Pauli/Aufbau sequence
        // use something analogous to quantic numbers

        /*
          for (int n = 1; n <= 7; n++) {
    for (int l = 0; l <= n - 1; l++) {
      for (int m = -l; m <= l; m++) {
        for (int s = -1; s <= 1; s += 2) {
*/



        string[] ops = { "k", "a", "n", "l" };

        for (int i = 2; i < 6; i++)
        {
            orbitalRadius = i * 2;
            for (int j = 0; j < i; j++)
            {
                if (i < 2) continue;
                float angle = j * Mathf.PI * 2f / (float)i;
                Vector3 offset = new Vector3(
                    Mathf.Cos(angle) * orbitalRadius,
                    orbitalYOffset,
                    Mathf.Sin(angle) * orbitalRadius
                );
                Vector3 position = transform.position + offset;

                GameObject orbital = Instantiate(polytronPrefab, position, Quaternion.identity, transform);
                orbital.transform.localScale = transform.localScale * 0.3f; // Match nucleus scale

                // Set the base recipe
                var orbitalGen = orbital.GetComponent<PolyhedronGenerator>();
                if (orbitalGen != null)
                {
                    orbitalGen.polyhedronRecipe = string.Concat(Enumerable.Repeat(ops[i - 2], j)) + baseRecipe;
                }

                orbital.name = $"Orbital_{baseRecipe}_{i + 1}";
            }
        }

    }
}