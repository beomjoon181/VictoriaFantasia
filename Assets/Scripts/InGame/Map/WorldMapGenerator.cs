using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 3D 지도 생성에 쓰는 재질 묶음.
/// </summary>
public readonly struct WorldMapMaterials
{
    /// <summary>Unity Terrain 용 재질 (URP Terrain/Lit)</summary>
    public readonly Material Terrain;

    /// <summary>바다 수면 재질</summary>
    public readonly Material Water;

    /// <summary>국경선(LineRenderer) 재질. 정점 색을 반영해야 한다.</summary>
    public readonly Material Border;

    public WorldMapMaterials(Material terrain, Material water, Material border)
    {
        Terrain = terrain;
        Water = water;
        Border = border;
    }
}

/// <summary>
/// WorldMapDefinition 데이터를 읽어 씬에 3D 지도(지형 + 바다 + 국경선)를 조립한다.
/// 에디터 빌더와 런타임 어느 쪽에서도 호출할 수 있도록 에디터 API 를 쓰지 않는다.
/// (TerrainData 를 에셋으로 저장하는 일은 호출하는 쪽 책임)
///
/// 생성 구조:
///   WorldMap
///   ├─ Terrain              (Unity Terrain: 높낮이 + 지형 텍스처, TerrainGroundSampler: 지면 높이 조회)
///   ├─ Sea                  (해수면 평면)
///   ├─ Region_DemonKingdom  (NationRegionView)
///   │   └─ Border           (LineRenderer, 지면을 따라 그려지는 국경선)
///   └─ ...
/// WorldMap 루트의 ScreenSpaceLineWidth 가 국경선 두께를 카메라 거리에 맞춰 화면상 일정하게 유지한다.
/// </summary>
public static class WorldMapGenerator
{
    /// <summary>지도 루트 오브젝트 이름 (재생성 시 기존 지도를 찾는 데 사용)</summary>
    public const string RootName = "WorldMap";

    /// <summary>
    /// 국경선 꼭짓점 간격 (m). 산비탈에서 선이 땅에 묻히지 않도록 촘촘히 지면 높이를 따라간다.
    /// 지형 높이는 지도 크기와 무관하므로 지도 좌표가 아닌 실제 거리로 정한다. (지도를 키워도 간격이 벌어지지 않게)
    /// </summary>
    const float BorderStepMeters = 60f;

    /// <summary>지형 기본맵 전환 거리 = 지도 대각선 길이 × 이 값 (가장 먼 카메라 거리보다 충분히 멀게)</summary>
    const float BasemapDistanceMultiplier = 10f;

    /// <summary>국경선 모서리를 깎는 횟수. 지도가 커져 외곽선 조각이 길어졌을 때 해안 곡선에서 벗어나 보이지 않게 한다.</summary>
    const int BorderSmoothIterations = 3;

    /// <summary>바다 평면이 지형 범위보다 몇 배 넓은지 (카메라가 비스듬히 볼 때 수평선 쪽이 비지 않게)</summary>
    const float SeaExtentMultiplier = 4f;

    /// <summary>
    /// 지도 전체를 생성해 루트 Transform 을 돌려준다.
    /// </summary>
    /// <param name="definition">지도 데이터</param>
    /// <param name="terrainData">높이·텍스처를 채울 TerrainData (내용은 덮어쓴다)</param>
    /// <param name="materials">지형·바다·국경선 재질</param>
    /// <param name="parent">루트를 붙일 부모 (없으면 씬 최상위)</param>
    public static Transform Generate(WorldMapDefinition definition, TerrainData terrainData,
        WorldMapMaterials materials, Transform parent = null)
    {
        var root = new GameObject(RootName).transform;
        root.SetParent(parent, false);

        // 해안·국경을 거칠게 만든 외곽선 (지형 마스크와 국경선이 같은 선을 쓰도록 한 번만 계산)
        var outlines = new List<List<Vector2>>();
        foreach (var region in definition.Regions)
            outlines.Add(EdgeRoughener.Roughen(region.outline, definition.RoughnessDepth, definition.RoughnessAmount));

        FillTerrainData(definition, terrainData, outlines);
        CreateTerrain(definition, terrainData, materials.Terrain, root);
        CreateSea(definition, materials.Water, root);

        var borders = new List<LineRenderer>();
        for (var i = 0; i < definition.Regions.Count; i++)
        {
            var border = CreateRegion(definition, definition.Regions[i], outlines[i], terrainData, materials.Border, root);
            if (border != null)
                borders.Add(border);
        }

        // 지도가 수십 km 라 고정 두께로는 멀리서 국경선이 사라지므로, 카메라 거리에 맞춰 화면상 두께를 유지한다.
        root.gameObject.AddComponent<ScreenSpaceLineWidth>()
            .Initialize(borders.ToArray(), definition.BorderPixelWidth, definition.Relief.SeaLevel);

        return root;
    }

    // ═════════════════════════ 지형 ═════════════════════════

    /// <summary>
    /// TerrainData 에 크기·높이맵·지형 레이어·스플랫맵을 채운다.
    /// </summary>
    static void FillTerrainData(WorldMapDefinition definition, TerrainData terrainData, List<List<Vector2>> outlines)
    {
        var relief = definition.Relief;
        var bounds = definition.MapBounds;

        // 높이맵: 해상도를 먼저 정한 뒤 크기를 정해야 한다. (해상도를 바꾸면 크기가 초기화될 수 있음)
        var heightGrid = new MapGrid(bounds, relief.heightmapResolution, relief.heightmapResolution, false);
        terrainData.heightmapResolution = relief.heightmapResolution;
        terrainData.size = new Vector3(
            bounds.width * definition.WorldScale, relief.HeightRange, bounds.height * definition.WorldScale);
        var landCells = RasterizeRegions(outlines, heightGrid);
        terrainData.SetHeights(0, 0, HeightmapBuilder.Build(heightGrid, landCells, relief, definition.MountainRanges));

        // 스플랫맵: 영역마다 자기 지형 레이어를 칠한다.
        var layers = CollectLayers(definition, out var regionLayers);
        var splatGrid = new MapGrid(bounds, relief.alphamapResolution, relief.alphamapResolution, true);
        terrainData.alphamapResolution = relief.alphamapResolution;
        terrainData.terrainLayers = layers.ToArray();
        var regionCells = RasterizeRegions(outlines, splatGrid);
        terrainData.SetAlphamaps(0, 0,
            SplatmapBuilder.Build(splatGrid, regionCells, regionLayers, layers.Count, relief.borderBlendRadius));
    }

    /// <summary>
    /// 영역 외곽선들을 격자에 칠해 표본별 영역 번호(0 = 바다, n = n-1 번째 영역) 배열을 만든다.
    /// </summary>
    static int[] RasterizeRegions(List<List<Vector2>> outlines, MapGrid grid)
    {
        var cells = new int[grid.Count];
        for (var i = 0; i < outlines.Count; i++)
        {
            if (outlines[i].Count >= 3)
                PolygonRasterizer.Fill(outlines[i], grid, cells, i + 1);
        }

        return cells;
    }

    /// <summary>
    /// 영역들이 쓰는 지형 레이어를 중복 없이 모으고, 영역 순번 → 레이어 번호 표를 만든다.
    /// </summary>
    static List<TerrainLayer> CollectLayers(WorldMapDefinition definition, out int[] regionLayers)
    {
        var layers = new List<TerrainLayer>();
        regionLayers = new int[definition.Regions.Count];

        for (var i = 0; i < definition.Regions.Count; i++)
        {
            var terrain = definition.Regions[i].terrain;
            var layer = terrain != null ? terrain.TerrainLayer : null;
            if (layer == null)
            {
                Debug.LogWarning($"WorldMapGenerator: 지형 레이어가 없는 영역이 있어 첫 번째 레이어로 칠합니다. ({RegionName(definition.Regions[i])})");
                regionLayers[i] = 0;
                continue;
            }

            var index = layers.IndexOf(layer);
            if (index < 0)
            {
                index = layers.Count;
                layers.Add(layer);
            }

            regionLayers[i] = index;
        }

        return layers;
    }

    /// <summary>
    /// TerrainData 로 Terrain 오브젝트를 만들고, 지도 범위 왼쪽 아래 모서리에 놓는다.
    /// </summary>
    static void CreateTerrain(WorldMapDefinition definition, TerrainData terrainData, Material material, Transform root)
    {
        // CreateTerrainGameObject 는 Terrain + TerrainCollider 를 함께 붙여 준다. (이후 지도 클릭 판정에 활용 가능)
        var go = Terrain.CreateTerrainGameObject(terrainData);
        go.name = "Terrain";
        go.transform.SetParent(root, false);
        // 높이맵 0 = 바다 밑바닥이므로, 지형을 바다 밑바닥 높이만큼 내려 실제 높이(해수면 등)가 월드 Y 와 일치하게 한다.
        go.transform.localPosition = definition.ToWorld(definition.MapBounds.min, definition.Relief.seaFloor);

        var terrain = go.GetComponent<Terrain>();
        // 카메라 등이 지면 높이를 물어볼 수 있게 높이 조회 컴포넌트를 붙인다.
        go.AddComponent<TerrainGroundSampler>().Initialize(terrain, definition.Relief.SeaLevel);
        terrain.materialTemplate = material;
        terrain.heightmapPixelError = 1f;     // 멀리서 봐도 산 모양이 뭉개지지 않게 정밀도를 높인다
        // 저해상도 기본맵으로 바뀌지 않고 항상 원본 텍스처로 그린다. (가장 먼 카메라 거리보다 충분히 멀게, 지도 크기에 비례)
        terrain.basemapDistance = definition.MapBounds.size.magnitude * definition.WorldScale * BasemapDistanceMultiplier;
        terrain.drawInstanced = true;
        terrain.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;
    }

    // ═════════════════════════ 바다 ═════════════════════════

    /// <summary>
    /// 해수면 높이에 넓은 사각 수면을 깐다. 해수면보다 낮은 지형(바다 밑바닥)은 이 아래로 가려진다.
    /// </summary>
    static void CreateSea(WorldMapDefinition definition, Material material, Transform root)
    {
        // Unity 기본 Plane(10 x 10, 위를 향함)을 늘려 쓴다. 클릭 판정은 지형 콜라이더가 맡으므로 콜라이더는 뗀다.
        const float PrimitivePlaneSize = 10f;
        var go = GameObject.CreatePrimitive(PrimitiveType.Plane);
        go.name = "Sea";
        DestroySafely(go.GetComponent<Collider>());
        go.transform.SetParent(root, false);

        var bounds = definition.MapBounds;
        var size = definition.ToWorld(new Vector2(bounds.width, bounds.height) * SeaExtentMultiplier, 0f);
        go.transform.localPosition = definition.ToWorld(bounds.center, definition.Relief.SeaLevel);
        go.transform.localScale = new Vector3(size.x / PrimitivePlaneSize, 1f, size.z / PrimitivePlaneSize);

        var renderer = go.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    /// <summary>
    /// 에디터(플레이 중이 아님)에서는 즉시, 플레이 중에는 프레임 끝에 오브젝트를 제거한다.
    /// </summary>
    static void DestroySafely(Object target)
    {
        if (target == null)
            return;

        if (Application.isPlaying)
            Object.Destroy(target);
        else
            Object.DestroyImmediate(target);
    }

    // ═════════════════════════ 국가 영역 / 국경선 ═════════════════════════

    /// <summary>
    /// 국가 영역 오브젝트(NationRegionView)와 그 국경선을 만든다.
    /// </summary>
    /// <returns>만든 국경선 (외곽선이 부족하면 null)</returns>
    static LineRenderer CreateRegion(WorldMapDefinition definition, WorldMapDefinition.NationRegion region,
        List<Vector2> outline, TerrainData terrainData, Material borderMaterial, Transform root)
    {
        var go = new GameObject($"Region_{RegionName(region)}", typeof(NationRegionView));
        go.transform.SetParent(root, false);
        go.GetComponent<NationRegionView>().Initialize(region.country, region.terrain);

        return outline.Count >= 3
            ? CreateBorder(definition, outline, terrainData, borderMaterial, go.transform)
            : null;
    }

    /// <summary>
    /// 영역 외곽선을 따라 지면 높이에 맞춘 닫힌 국경선(LineRenderer)을 그린다.
    /// 이웃 영역과 맞닿은 부분은 양쪽에서 같은 곡선으로 겹쳐 그려지므로 한 줄로 보인다.
    /// 두께는 ScreenSpaceLineWidth 가 카메라 거리에 맞춰 매 프레임 정한다.
    /// </summary>
    static LineRenderer CreateBorder(WorldMapDefinition definition, List<Vector2> outline, TerrainData terrainData,
        Material material, Transform parent)
    {
        var go = new GameObject("Border", typeof(LineRenderer));
        go.transform.SetParent(parent, false);

        // 모서리를 깎아 부드러운 곡선으로 만든 뒤(해안선은 높이맵 블러로 둥글게 만들어지므로 그 모양에 맞춘다),
        // 지면 기복을 따라가도록 촘촘하게 나누고, 각 점을 지면(또는 수면) 위로 띄운다.
        var points = Densify(SmoothCorners(outline, BorderSmoothIterations), BorderStepMeters / definition.WorldScale);
        var positions = new Vector3[points.Count];
        for (var i = 0; i < points.Count; i++)
        {
            var normalized = definition.ToNormalized(points[i]);
            // GetInterpolatedHeight 는 지형 바닥 기준 높이이므로 지형이 놓인 높이(바다 밑바닥)를 더한다.
            var ground = terrainData.GetInterpolatedHeight(normalized.x, normalized.y) + definition.Relief.seaFloor;
            var height = Mathf.Max(ground, definition.Relief.SeaLevel) + definition.BorderLift;
            positions[i] = definition.ToWorld(points[i], height);
        }

        var line = go.GetComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.loop = true;
        line.positionCount = positions.Length;
        line.SetPositions(positions);
        line.widthMultiplier = 1f; // 임시 두께 (ScreenSpaceLineWidth 가 덮어쓴다)
        line.startColor = definition.BorderColor;
        line.endColor = definition.BorderColor;
        line.numCornerVertices = 2;           // 꺾이는 곳이 뾰족하게 튀지 않도록 둥글게
        line.alignment = LineAlignment.View;  // 원근 시점에서도 두께가 일정하게 카메라를 향한다
        line.sharedMaterial = material;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;
        return line;
    }

    /// <summary>
    /// 닫힌 꺾은선의 모서리를 깎아 부드럽게 만든다. (Chaikin 방식: 각 변의 1/4, 3/4 지점으로 대체를 반복)
    /// 변 하나에서 만들어지는 점이 변의 방향과 무관하게 같으므로, 이웃 영역이 공유하는 국경은 양쪽에서 같은 곡선이 된다.
    /// </summary>
    /// <param name="outline">닫힌 꺾은선</param>
    /// <param name="iterations">반복 횟수 (클수록 부드럽지만 점이 2배씩 늘어난다)</param>
    static List<Vector2> SmoothCorners(List<Vector2> outline, int iterations)
    {
        var current = outline;
        for (var iteration = 0; iteration < iterations; iteration++)
        {
            var next = new List<Vector2>(current.Count * 2);
            for (var i = 0; i < current.Count; i++)
            {
                var a = current[i];
                var b = current[(i + 1) % current.Count];
                next.Add(Vector2.Lerp(a, b, 0.25f));
                next.Add(Vector2.Lerp(a, b, 0.75f));
            }

            current = next;
        }

        return current;
    }

    /// <summary>
    /// 닫힌 꺾은선의 각 변을 step 이하 간격으로 잘게 나눈다. (마지막 점 → 첫 점 변 포함)
    /// </summary>
    static List<Vector2> Densify(List<Vector2> outline, float step)
    {
        var result = new List<Vector2>();
        for (var i = 0; i < outline.Count; i++)
        {
            var a = outline[i];
            var b = outline[(i + 1) % outline.Count];
            var pieces = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(a, b) / step));
            for (var k = 0; k < pieces; k++)
                result.Add(Vector2.Lerp(a, b, (float)k / pieces));
        }

        return result;
    }

    /// <summary>
    /// 오브젝트 이름에 쓸 영역 이름. 국가가 비어 있으면 "Unknown".
    /// </summary>
    static string RegionName(WorldMapDefinition.NationRegion region)
    {
        return region.country != null ? region.country.name : "Unknown";
    }
}
