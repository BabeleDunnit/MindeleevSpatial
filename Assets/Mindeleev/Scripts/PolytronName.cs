using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

public static class PolytronName
{
    // ===== Data model =====
    public struct Entry
    {
        public int Index;              // 1..72
        public string Angel;           // es. "Vehuiah"
        public string Demon;           // es. "Bael"
        public int StartMonth, StartDay;
        public int EndMonth, EndDay;   // intervallo INCLUSIVO

        public bool ContainsDate(int m, int d)
        {
            // range nello stesso mese
            if (StartMonth == EndMonth)
            {
                return (m == StartMonth && d >= StartDay && d <= EndDay);
            }
            // range su due mesi consecutivi (nei dati non attraversiamo fine anno)
            if (m == StartMonth)
                return d >= StartDay;
            if (m == EndMonth)
                return d <= EndDay;
            return false; // mese intermedio? nei nostri dati non esistono intervalli > 2 mesi
        }
    }

    // ===== Tabella 72 (angelo, demone, periodo) =====
    // Periodi: inclusivi. Basati sulla tabella concordata.
    public static readonly Entry[] Table = new Entry[]
    {
        E( 1,"Vehuiah","Bael"           , 3,21, 3,25),
        E( 2,"Jeliel","Agares"          , 3,26, 3,30),
        E( 3,"Sitael","Vassago"         , 3,31, 4, 4),
        E( 4,"Elemiah","Gamigin"        , 4, 5, 4, 9),
        E( 5,"Mahasiah","Marbas"        , 4,10, 4,14),
        E( 6,"Lelahel","Valefor"        , 4,15, 4,20),
        E( 7,"Achaiah","Amon"           , 4,21, 4,25),
        E( 8,"Cahetel","Barbatos"       , 4,26, 4,30),
        E( 9,"Haziel","Paimon"          , 5, 1, 5, 5),
        E(10,"Aladiah","Buer"           , 5, 6, 5,10),
        E(11,"Lauviah","Gusion"         , 5,11, 5,15),
        E(12,"Hahaiah","Sitri"          , 5,16, 5,20),
        E(13,"Iezalel","Beleth"         , 5,21, 5,25),
        E(14,"Mebahel","Leraje"         , 5,26, 5,31),
        E(15,"Hariel","Eligos"          , 6, 1, 6, 5),
        E(16,"Hakamiah","Zepar"         , 6, 6, 6,10),
        E(17,"Lauviah II","Botis"       , 6,11, 6,15),
        E(18,"Caliel","Bathin"          , 6,16, 6,21),
        E(19,"Leuviah","Sallos"         , 6,22, 6,26),
        E(20,"Pahaliah","Purson"        , 6,27, 7, 1),
        E(21,"Nelchael","Marax"         , 7, 2, 7, 6),
        E(22,"Yeyayel","Ipos"           , 7, 7, 7,11),
        E(23,"Melahel","Aim"            , 7,12, 7,16),
        E(24,"Haheuiah","Naberius"      , 7,17, 7,22),
        E(25,"Nith-Haiah","Glasya-Labolas",7,23, 7,27),
        E(26,"Haaiah","Bune"            , 7,28, 8, 1),
        E(27,"Ierathel","Ronove"        , 8, 2, 8, 6),
        E(28,"Seheiah","Berith"         , 8, 7, 8,12),
        E(29,"Reiyel","Astaroth"        , 8,13, 8,17),
        E(30,"Omael","Forneus"          , 8,18, 8,22),
        E(31,"Lecabel","Foras"          , 8,23, 8,28),
        E(32,"Vasariah","Asmoday"       , 8,29, 9, 2),
        E(33,"Yehuiah","Gaap"           , 9, 3, 9, 7),
        E(34,"Lehahiah","Furfur"        , 9, 8, 9,12),
        E(35,"Chavakiah","Marchosias"   , 9,13, 9,17),
        E(36,"Menadel","Stolas"         , 9,18, 9,23),
        E(37,"Aniel","Phenex"           , 9,24, 9,28),
        E(38,"Haamiah","Halphas"        , 9,29,10, 3),
        E(39,"Rehael","Malphas"         ,10, 4,10, 8),
        E(40,"Ieiazel","Raum"           ,10, 9,10,13),
        E(41,"Hahahel","Focalor"        ,10,14,10,18),
        E(42,"Mikael","Vepar"           ,10,19,10,23),
        E(43,"Veuahiah","Sabnock"       ,10,24,10,28),
        E(44,"Yelahiah","Shax"          ,10,29,11, 2),
        E(45,"Sehaliah","Vine"          ,11, 3,11, 7),
        E(46,"Ariel","Bifrons"          ,11, 8,11,12),
        E(47,"Asaliah","Vual"           ,11,13,11,17),
        E(48,"Mihael","Haagenti"        ,11,18,11,22),
        E(49,"Vehuel","Crocell"         ,11,23,11,27),
        E(50,"Daniel","Furcas"          ,11,28,12, 2),
        E(51,"Hahasiah","Balam"         ,12, 3,12, 7),
        E(52,"Imamiah","Alloces"        ,12, 8,12,12),
        E(53,"Nanael","Camio"           ,12,13,12,16),
        E(54,"Nithael","Murmur"         ,12,17,12,21),
        E(55,"Mebahiah","Orobas"        ,12,22,12,26),
        E(56,"Poiel","Gremory"          ,12,27,12,31),
        E(57,"Nemamiah","Ose"           , 1, 1, 1, 5),
        E(58,"Yeialel","Amy"            , 1, 6, 1,10),
        E(59,"Harahel","Orias"          , 1,11, 1,15),
        E(60,"Mizrael","Vapula"         , 1,16, 1,20),
        E(61,"Umabel","Zagan"           , 1,21, 1,25),
        E(62,"Iah-Hel","Valac"          , 1,26, 1,30),
        E(63,"Anauel","Andras"          , 1,31, 2, 4),
        E(64,"Mehiel","Haures"          , 2, 5, 2, 9),
        E(65,"Damabiah","Andrealphus"   , 2,10, 2,14),
        E(66,"Manakel","Cimeies"        , 2,15, 2,19),
        E(67,"Eyael","Amdusias"         , 2,20, 2,24),
        E(68,"Habuhiah","Belial"        , 2,25, 2,29),
        E(69,"Rochel","Decarabia"       , 3, 1, 3, 5),
        E(70,"Jabamiah","Seere"         , 3, 6, 3,10),
        E(71,"Haiaiel","Dantalion"      , 3,11, 3,15),
        E(72,"Mumiah","Andromalius"     , 3,16, 3,20),
    };

    private static Entry E(int idx, string a, string d, int sm, int sd, int em, int ed)
        => new Entry { Index = idx, Angel = a, Demon = d, StartMonth = sm, StartDay = sd, EndMonth = em, EndDay = ed };

    // ===== Public API =====

    /// <summary>
    /// Restituisce un nome ibrido (angel + demon) per l'indice 1..72.
    /// Mai usa il nome completo; ricava pseudo-sillabe e compone qualcosa di leggibile.
    /// </summary>
    public static string GetName(int i)
    {
        if (i < 1 || i > 72) throw new ArgumentOutOfRangeException(nameof(i), "Index must be 1..72");
        var e = Table[i - 1];
        var aPart = PickAngelChunk(e.Angel);
        var dPart = PickDemonChunk(e.Demon);
        // Debug.Log($"a: {aPart}, d: {dPart}");
        var fused = Fuse(aPart, dPart);
        return TitleCase(Clean(fused));
    }


// ...existing code...
    /// <summary>
    /// Returns a human-readable period string for the table entry at 1-based index.
    /// Example: "from March, 21 to March, 25"
    /// </summary>
    public static string GetPeriodString(int index)
    {
        if (index < 0 || index >= Table.Length) throw new ArgumentOutOfRangeException(nameof(index), "Index must be between 0 and " + (Table.Length - 1));
        var e = Table[index];
        return $"From {MonthName(e.StartMonth)}, {e.StartDay} to {MonthName(e.EndMonth)}, {e.EndDay}";
    }

    /// <summary>
    /// Dato giorno/mese (1-based), restituisce l'indice 1..72 dell'angelo/demone responsabile di quel periodo.
    /// Lancia se la data è invalida (es. 31/11).
    /// </summary>
    public static int GetIdxFromDayAndMonth(int day, int month, int yearForValidation = 2025)
    {
        if (month < 1 || month > 12) throw new ArgumentOutOfRangeException(nameof(month));
        // convalida giorni reali del mese (rispetta anni bisestili se yearForValidation è bisestile)
        int max = DateTime.DaysInMonth(yearForValidation, month);
        if (day < 1 || day > max) throw new ArgumentOutOfRangeException(nameof(day), $"Day {day} is invalid for month {month} in {yearForValidation}.");

        foreach (var e in Table)
        {
            if (e.ContainsDate(month, day))
                return e.Index;
        }
        throw new InvalidOperationException($"No angel/demon period found for {day:D2}/{month:D2}. (Check table consistency.)");
    }

    /*
        // ===== Test helpers =====
        public static void Test_PrintAll72()
        {
            for (int i = 1; i <= 72; i++)
            {
                var e = Table[i - 1];
                Console.WriteLine($"{i,2}. {GetName(i)}   [{e.Angel} / {e.Demon}]  {FmtPeriod(e)}");
            }
        }

        /// <summary>
        /// Stampa un nome per ogni giorno dell'anno 'year' (rispetta mesi e bisestile).
        /// </summary>
        public static void Test_PrintAllByCalendar(int year = 2025)
        {
            for (int m = 1; m <= 12; m++)
            {
                int max = DateTime.DaysInMonth(year, m);
                for (int d = 1; d <= max; d++)
                {
                    int idx = GetIdxFromDayAndMonth(d, m, year);
                    string name = GetName(idx);
                    Console.WriteLine($"{year}-{m:D2}-{d:D2}  ->  #{idx:D2}  {name}");
                }
            }
        }
    */

    // ===== Internals: name synthesis =====

    private static readonly Regex VowelSplit = new Regex(@"(?i)(?=[aeiouy])", RegexOptions.Compiled);
// private static readonly Regex VowelSplit = new Regex(@"(?i)(?<=[aeiouy])", RegexOptions.Compiled);

    private static string[] Syllabify(string s)
    {
        // Spezza “prima delle vocali” per avvicinare sillabe; poi pulisce chunk vuoti e non alpha.
        var parts = VowelSplit.Split(s.ToLower());
        var cleaned = new List<string>();
        foreach (var p in parts)
        {
            var t = Regex.Replace(p, @"[^a-z]", "");
            if (!string.IsNullOrEmpty(t)) cleaned.Add(t);
        }
        if (cleaned.Count == 0) cleaned.Add(s.ToLower());
        return cleaned.ToArray();
    }

    private static string PickAngelChunk(string angel)
    {
        var syl = Syllabify(angel);
        // Debug.Log(String.Join(':', syl));
        // prendi 1–2 prime sillabe, evitando di replicare tutto il nome:
        if (syl.Length == 1) return syl[0].Substring(0, Math.Min(3, syl[0].Length));
        string pick = syl[0] + (syl.Length > 1 ? syl[1] : "");
        if (pick.Length > 5) pick = pick.Substring(0, 5);
        return pick;
    }

    private static string PickDemonChunk(string demon)
    {
        var syl = Syllabify(demon);
        // prendi 1–2 ultime sillabe:
        if (syl.Length == 1)
        {
            string a = syl[0];
            int start = Math.Max(0, a.Length - 3);
            return a.Substring(start);
        }
        string last = syl[syl.Length - 1];
        string prev = syl.Length >= 2 ? syl[syl.Length - 2] : "";
        string pick = (prev.Length <= 2 ? prev : prev.Substring(prev.Length - 2)) + last;
        if (pick.Length > 5) pick = pick.Substring(pick.Length - 5);
        return pick;
    }

    private static string Fuse(string a, string d)
    {
        // Evita giunzioni dure: se doppia vocale/consonante al confine, limala.
        if (a.Length == 0) return d;
        if (d.Length == 0) return a;

        char lastA = a[a.Length - 1];
        char firstD = d[0];
        bool vowA = IsVowel(lastA);
        bool vowD = IsVowel(firstD);

        if (vowA && vowD)
        {
            // elimina la vocale finale di a
            // a = a.TrimEnd('a','e','i','o','u','y');
        }
        else if (!vowA && !vowD)
        {
            // inserisci una vocale di collegamento
            return a + "a" + d;
        }

        return a + d;
    }

    private static bool IsVowel(char c)
        => "aeiouyAEIOUY".IndexOf(c) >= 0;

    private static string Clean(string s)
    {
        // niente trattini/spazi; massimo 10 caratteri
        s = Regex.Replace(s, @"[^a-zA-Z]", "");
        if (s.Length > 10) s = s.Substring(0, 10);
        return s;
    }

    private static string TitleCase(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        if (s.Length == 1) return s.ToUpper();
        return char.ToUpper(s[0]) + s.Substring(1).ToLower();
    }

    public static string FmtPeriod(Entry e)
        => $"{e.StartDay} {MonthName(e.StartMonth)} – {e.EndDay} {MonthName(e.EndMonth)}";

    private static string MonthName(int m)
        => new DateTime(2025, m, 1).ToString("MMMM");
}
