using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using System;
using System.Globalization;
using TMPro;



public class Aufbau : MonoBehaviour
{

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
        int maxZ = 120; // cambia a piacere
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
                    .OrderBy(kvp => int.Parse(kvp.Key.Substring(0, kvp.Key.Length - 1))) // n
                    .ThenBy(kvp => "spdfgh".IndexOf(kvp.Key[^1])) // l: s=0, p=1, d=2, ...
                    .Select(kvp => $"{kvp.Key}{kvp.Value}")
            ));
            Debug.Log("");
        }
        Debug.Log("fine aufbau");
    }


    void Start()
    {
        GenerateAufbau();
    }


}

