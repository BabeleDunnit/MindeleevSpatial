using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GridManager
{
    private MutatronEngine engine;

    static Material sLineMat;

    public GridManager(MutatronEngine engine)
    {
        this.engine = engine;
    }

    static Material GetLineMat()
    {
        if (sLineMat == null) sLineMat = new Material(Shader.Find("Sprites/Default"));
        return sLineMat;
    }

    public void DrawCircle(MutatronEngine.HexCellData hcd, float radius, Color color, float lineWidth = 0.05f, int segments = 20)
    {
        GameObject go = hcd.circle;
        LineRenderer lr = go.GetComponent<LineRenderer>();
        lr.startWidth = lineWidth;
        lr.endWidth = lineWidth;
        lr.startColor = lr.endColor = color;
        lr.positionCount = segments;

        for (int i = 0; i < segments; i++)
        {
            float angle = (float)i / segments * Mathf.PI * 2f;
            float x = Mathf.Cos(angle) * radius;
            float y = Mathf.Sin(angle) * radius;
            lr.SetPosition(i, hcd.worldCoords + new Vector3(x, 0.1f, y));
        }
    }

    public void DrawLine(Vector3 start, Vector3 end, Color color, float width = 0.05f)
    {
        var go = new GameObject("Line");
        go.transform.SetParent(engine.transform);
        var lr = go.AddComponent<LineRenderer>();

        lr.positionCount = 2;
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);

        lr.startWidth = lr.endWidth = width;
        lr.sharedMaterial = GetLineMat();
        lr.startColor = lr.endColor = color;
    }

    public GameObject CreateCircle(int ring, int idxInRing)
    {
        var go = new GameObject($"circle_{ring}_{idxInRing}");
        go.transform.SetParent(engine.transform);

        LineRenderer lr = go.AddComponent<LineRenderer>();
        lr.loop = true;
        lr.sharedMaterial = GetLineMat();
        lr.positionCount = 0;

        return go;
    }

    public GameObject CreateSink(KeyValuePair<HexCoord, MutatronEngine.HexCellData> hckv)
    {
        string recipe = "tC";
        GameObject sink = PolytronsFactory.Instance.Create($"sink/{recipe}", 0.3f);

        // the central sink is a bit higher
        if (engine.IsMutatronCenter(hckv.Value))
        {
            sink.transform.position = new Vector3(hckv.Value.worldCoords.x, 2f, hckv.Value.worldCoords.z);
        }
        else
        {
            sink.transform.position = new Vector3(hckv.Value.worldCoords.x, 1.0f, hckv.Value.worldCoords.z);
        }

        hckv.Value.sink = sink.GetComponent<PolytronSink>();
        hckv.Value.sink.weight = 0.2f;
        hckv.Value.sink.hexCoord = hckv.Key;
        hckv.Value.sink.GetComponent<MeshRenderer>().enabled = false;

        return sink;
    }

    public void CreateHexGridDataStructure()
    {
        Vector2 center2D = new Vector2(engine.transform.position.x, engine.transform.position.z);
        for (int ring = 0; ring <= engine.maxRings; ring++)
        {
            int hexesInRing = ring == 0 ? 1 : 6 * ring;
            for (int idxInRing = 0; idxInRing < hexesInRing; idxInRing++)
            {
                HexCoord hex = ring == 0 ? new HexCoord(0, 0) : HexCoord.AtPolar(ring, idxInRing);

                Vector2 hexPos2D = hex.Position() * 2f + center2D;
                Vector3 position = new Vector3(hexPos2D.x, engine.transform.position.y, hexPos2D.y);

                var cellData = new MutatronEngine.HexCellData
                {
                    ring = ring,
                    idxInRing = idxInRing,
                    worldCoords = position,
                    circle = CreateCircle(ring, idxInRing)
                    ,
                    // ensure each cell has a base polyhedron character for tile recipe construction
                    tileBasePolyhedron = engine.actualLevelConfig.tileBasePoly.ToString()
                };

                if (IsMetatronCoord(ring, idxInRing))
                {
                    cellData.isOnMetatronPattern = true;
                }

                engine.gridCellsMap[hex] = cellData;

                if (ring == 0 && idxInRing == 0)
                {
                    engine.mutatronCenter = cellData;
                }
            }
        }

        foreach (var hckv in engine.gridCellsMap)
        {
            GameObject sink = CreateSink(hckv);
            sink.name += $"_{hckv.Value.ring}_{hckv.Value.idxInRing}";
        }
    }

    bool IsMetatronCoord(int ring, int idxInRing)
    {
        if (ring == 0 || ring == 1) return true;
        for (int i = 2; i < 10; i++)
        {
            if (ring == i && (idxInRing % i == 0)) return true;
        }
        return false;
    }

    public IEnumerator BuildTilesCoroutine()
    {
        if (!engine.mustBuildFirstTime)
        {
            yield return new WaitForSeconds(5.5f);
        }

        foreach (var hckv in engine.gridCellsMap)
        {
            if (hckv.Value.ring <= engine.actualLevelConfig.actualRingsCount)
            {
                float angleToCenter = hckv.Key.PolarAngle();
                Quaternion tileRotation = Quaternion.Euler(0f, -angleToCenter * 360f / 6.28f, 0f);

                string tileRecipe = PolyhedronRecipeKabbalah.IntToOperatorsSequence(hckv.Value.polytronicNumber) + hckv.Value.tileBasePolyhedron;

                GameObject tile = PolytronsFactory.Instance.Create($"tile/{tileRecipe}", 1f);
                tile.name += $"_mutatron_{hckv.Value.ring}_{hckv.Value.idxInRing}";
                tile.transform.localScale = new Vector3(1f, 0.01f, 1f);
                tile.transform.position = hckv.Value.worldCoords + new Vector3(0, 0.1f, 0);
                tile.transform.localRotation = tileRotation;
                hckv.Value.tile = tile.GetComponent<MutatronTile>();

                yield return new WaitForSeconds(0.15f);
            }
        }

        engine.AfterTilesCreation();
    }

    // Rebuild a single tile mesh if it's different from the requested recipe
    public void RebuildTileMesh(HexCoord coord, string recipe)
    {
        if (engine != null && engine.suppressTileUpdates)
        {
            Debug.Log($"[GridManager.RebuildTileMesh] suppressed rebuild at coord={coord} newRecipe={recipe}");
            return;
        }
        if (!engine.gridCellsMap.TryGetValue(coord, out var cell)) return;
        PolyhedronGenerator tile = cell.tile;
        if (tile == null)
        {
            Debug.LogWarning($"[GridManager.RebuildTileMesh] no tile found at coord={coord} to rebuild with recipe={recipe}");
            return;
        }

        if (tile.recipe == recipe)
        {
            Debug.Log($"[GridManager.RebuildTileMesh] coord={coord} recipe unchanged ({recipe}), skipping rebuild");
            return;
        }

        Debug.Log($"[GridManager.RebuildTileMesh] Rebuilding tile at coord={coord}: oldRecipe={tile.recipe} newRecipe={recipe}");
        tile.recipe = recipe;
        tile.RebuildMesh();
    }

    // Update all tiles for the current level configuration
    public void UpdateTiles()
    {
        if (engine != null && engine.suppressTileUpdates)
        {
            Debug.Log("[GridManager.UpdateTiles] suppressed");
            return;
        }
        foreach (var hckv in engine.gridCellsMap)
        {
            if (hckv.Value.ring > engine.actualLevelConfig.actualRingsCount) continue;

            string tileRecipe = PolyhedronRecipeKabbalah.IntToOperatorsSequence(hckv.Value.polytronicNumber) + hckv.Value.tileBasePolyhedron;
            RebuildTileMesh(hckv.Key, tileRecipe);
        }
    }
}
