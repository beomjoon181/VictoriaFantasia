using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 인게임 3D 대륙 지도를 만드는 에디터 도구.
///
/// 하는 일:
/// 1. 지형 사진(Assets/Art/Map/Source)에서 하늘을 잘라 낸 땅 부분만 이음매 없는 지형 텍스처로 만든다.
/// 2. 지형 텍스처로 TerrainLayer / TerrainDefinition(평야, 메마른 땅) 에셋을 만든다.
/// 3. 참고 지도(대륙 전도)를 따라 그린 국가별 외곽선과 "넘을 수 없는 산맥"으로 WorldMapDefinition 에셋을 만든다.
///    - 마왕국: 메마른 땅 / 나머지 4개국: 평야
/// 4. 현재 씬에 3D 지도(Unity Terrain + 바다 + 국경선)를 생성하고,
///    카메라를 지도를 비스듬히 내려다보는 원근 카메라로 맞추고 이동·확대·축소 컴포넌트를 붙이며,
///    태양광을 북서쪽에서 비추도록 맞춘다.
///
/// 이미 만들어진 데이터 에셋은 덮어쓰지 않아 디자이너의 수정이 보존된다. (지도 데이터를 다시 만들려면 에셋을 지운 뒤 실행)
/// 단, 지형 높이·텍스처 배치(WorldTerrain.asset)는 생성 결과물이므로 실행할 때마다 다시 계산한다.
/// </summary>
public static class WorldMapBuilder
{
    // ───────────── 경로 ─────────────
    const string SourceFolder = "Assets/Art/Map/Source";
    const string TextureFolder = "Assets/Art/Map/Terrain";
    const string MaterialFolder = "Assets/Art/Map/Materials";
    const string DataFolder = "Assets/Data/Map";
    const string CountryFolder = "Assets/Data/Countries";
    const string MapDefinitionPath = DataFolder + "/WorldMap.asset";
    const string TerrainDataPath = DataFolder + "/WorldTerrain.asset";
    const string TerrainMaterialPath = MaterialFolder + "/MapTerrain.mat";
    const string SeaMaterialPath = MaterialFolder + "/MapSea.mat";
    const string BorderMaterialPath = MaterialFolder + "/MapBorder.mat";

    // ───────────── 셰이더 ─────────────
    /// <summary>URP 지형 셰이더 (레이어 텍스처 혼합 + 조명)</summary>
    const string TerrainShaderName = "Universal Render Pipeline/Terrain/Lit";
    /// <summary>URP 기본 조명 셰이더 (바다 수면)</summary>
    const string LitShaderName = "Universal Render Pipeline/Lit";
    /// <summary>국경선 전용 셰이더 (정점 색 + 지면에 묻히지 않도록 깊이를 카메라 쪽으로 당김)</summary>
    const string BorderShaderName = "VictoriaFantasia/MapBorder";

    /// <summary>바다 색</summary>
    static readonly Color SeaColor = new Color(0.16f, 0.32f, 0.44f);
    /// <summary>바다 수면의 매끈함 (태양 반사광 크기)</summary>
    const float SeaSmoothness = 0.35f;

    // ───────────── 카메라 / 조명 ─────────────
    // 내려다보는 각도는 MapCameraRig 가 거리에 따라 정한다. (가까울수록 눕힘)
    /// <summary>카메라 세로 시야각</summary>
    const float CameraFieldOfView = 35f;
    // 아래 거리·위치는 지도 좌표 단위라 지도 크기(WorldScale)가 바뀌어도 같은 구도가 된다.
    /// <summary>시작 거리 (지도 좌표). 대륙 전체가 화면에 들어오도록 맞춘 값. Home 키로 돌아오는 시점이기도 하다.</summary>
    const float OverviewDistanceInMapUnits = 22f;
    /// <summary>가장 멀리 물러날 수 있는 거리 (지도 좌표). 대륙 전체 보기보다 조금 더 멀리.</summary>
    const float MaxDistanceInMapUnits = 25f;
    /// <summary>시작 시 바라보는 지점 (지도 좌표). 원근 때문에 가까운 남쪽이 크게 보이므로 대륙 중심보다 살짝 남쪽을 본다.</summary>
    static readonly Vector2 OverviewTargetInMapUnits = new Vector2(0f, -0.2f);
    /// <summary>가장 가까이 다가갈 수 있는 거리 (m). 지형 높이는 지도 크기와 무관하므로 m 로 고정한다.</summary>
    const float MinDistanceMeters = 250f;
    /// <summary>
    /// 카메라 먼 클리핑 거리 (지도 좌표). 가장 멀리서 대륙과 바다 수평선까지 보이도록 넉넉히 잡는다.
    /// (가까운 클리핑 거리는 MapCameraRig 가 카메라 거리에 맞춰 자동으로 정한다)
    /// </summary>
    const float CameraFarClipInMapUnits = 100f;
    /// <summary>태양광 방향: 지도 관례대로 북서쪽에서 비춘다. 고도를 낮게 잡아 언덕·산의 음영이 잘 드러나게 한다.</summary>
    static readonly Vector3 SunEuler = new Vector3(32f, 135f, 0f);

    /// <summary>
    /// 지형 한 종류를 만들기 위한 설정.
    /// </summary>
    readonly struct TerrainRecipe
    {
        /// <summary>생성할 에셋 이름 (텍스처·레이어·정의 공통)</summary>
        public readonly string AssetName;
        /// <summary>게임 내 표시 이름</summary>
        public readonly string DisplayName;
        /// <summary>원본 사진 파일 이름</summary>
        public readonly string SourcePhoto;
        /// <summary>잘라 낼 영역의 왼쪽 위 (사진 크기 대비 0~1, y 는 위에서부터)</summary>
        public readonly Vector2 CropTopLeft;
        /// <summary>잘라 낼 영역의 크기 (사진 크기 대비 0~1)</summary>
        public readonly Vector2 CropSize;
        /// <summary>텍스처 한 장이 덮는 지면 가로 길이 (m). 세로는 잘라 낸 비율로 자동 계산.</summary>
        public readonly float TileWidthMeters;

        public TerrainRecipe(string assetName, string displayName, string sourcePhoto,
            Vector2 cropTopLeft, Vector2 cropSize, float tileWidthMeters)
        {
            AssetName = assetName;
            DisplayName = displayName;
            SourcePhoto = sourcePhoto;
            CropTopLeft = cropTopLeft;
            CropSize = cropSize;
            TileWidthMeters = tileWidthMeters;
        }
    }

    // 사진의 위쪽은 하늘·먼 산이므로 아래쪽 땅 부분만 잘라 쓴다.
    // 메마른 땅: 오른쪽 아래의 큰 바위 무더기는 반복되면 눈에 띄므로 왼쪽 부분만 쓴다.
    // 타일 크기: 대륙(동서 약 89km)의 1/8 정도로 잡는다. 이보다 작으면 멀리서 수십 번 반복돼 줄무늬처럼 보인다.
    //           (더 키우면 가까이 확대했을 때 텍스처가 흐려진다)
    static readonly TerrainRecipe BarrenLand = new TerrainRecipe(
        "BarrenLand", "메마른 땅", "BarrenLandPhoto.jpg",
        new Vector2(0f, 0.72f), new Vector2(0.55f, 0.28f), 12500f);

    static readonly TerrainRecipe Plains = new TerrainRecipe(
        "Plains", "평야", "PlainsPhoto.jpg",
        new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), 11250f);

    // ═════════════════════════ 참고 지도 외곽선 ═════════════════════════
    // 좌표는 참고 지도 이미지(1106 x 1706 px)의 픽셀 좌표(x 오른쪽, y 아래쪽)다.
    // ToMap 에서 지도 중심(550, 820)을 원점으로, 100px = 지도 좌표 1 로 바꾸고 y 를 뒤집는다. (y 위쪽 = 북쪽)
    // 국경은 이웃 두 나라가 같은 배열을 (방향만 바꿔) 공유해 틈 없이 맞물린다.

    /// <summary>참고 지도에서 지도 좌표 원점이 될 픽셀 좌표</summary>
    static readonly Vector2 MapPixelOrigin = new Vector2(550f, 820f);
    /// <summary>지도 좌표 1 에 해당하는 픽셀 수</summary>
    const float PixelsPerUnit = 100f;

    // ── 국경이 세 나라에서 만나는 점 / 국경이 해안에 닿는 점 ──
    static readonly Vector2 DemonBelatCoast = new Vector2(305, 607);    // 마왕국·벨라트 서쪽 해안
    static readonly Vector2 DemonArkeniaCoast = new Vector2(915, 672);  // 마왕국·아르케니아 동쪽 해안
    static readonly Vector2 DemonBelatArkenia = new Vector2(470, 657);  // 마왕국·벨라트·아르케니아
    static readonly Vector2 BelatArkeniaErdin = new Vector2(550, 968);  // 벨라트·아르케니아·에르딘
    static readonly Vector2 BelatErdinCoast = new Vector2(135, 1018);   // 벨라트·에르딘 서쪽 해안
    static readonly Vector2 ErdinArkeniaCaldera = new Vector2(545, 1040); // 에르딘·아르케니아·칼데라
    static readonly Vector2 ArkeniaCalderaCoast = new Vector2(965, 1035); // 아르케니아·칼데라 동쪽 해안
    static readonly Vector2 ErdinCalderaCoast = new Vector2(412, 1382);  // 에르딘·칼데라 남쪽 해안

    // ── 국경선 (양 끝점 제외한 중간 점, 앞쪽 끝점 → 뒤쪽 끝점 순) ──
    static readonly Vector2[] DemonBelatBorder = { new(380, 625), new(450, 628) };                       // DemonBelatCoast → DemonBelatArkenia
    static readonly Vector2[] DemonArkeniaBorder = { new(580, 672), new(680, 690), new(750, 695), new(840, 680) }; // DemonBelatArkenia → DemonArkeniaCoast
    static readonly Vector2[] BelatArkeniaBorder = { new(500, 710), new(510, 780), new(520, 850), new(530, 920) }; // DemonBelatArkenia → BelatArkeniaErdin (넘을 수 없는 산맥)
    static readonly Vector2[] BelatErdinBorder = { new(250, 1005), new(340, 975), new(450, 968) };       // BelatErdinCoast → BelatArkeniaErdin
    static readonly Vector2[] ErdinArkeniaBorder = { };                                                    // BelatArkeniaErdin → ErdinArkeniaCaldera
    static readonly Vector2[] ArkeniaCalderaBorder = { new(650, 1042), new(800, 1040) };                 // ErdinArkeniaCaldera → ArkeniaCalderaCoast
    static readonly Vector2[] ErdinCalderaBorder = { new(510, 1100), new(470, 1180), new(450, 1260), new(430, 1330) }; // ErdinArkeniaCaldera → ErdinCalderaCoast

    // ── 해안선 (양 끝점 제외, 지도 둘레를 시계 방향으로) ──
    static readonly Vector2[] DemonCoast = // DemonBelatCoast → DemonArkeniaCoast (북쪽 돌출부를 돌아서)
    {
        new(340, 570), new(400, 530), new(440, 500), new(450, 430), new(440, 360), new(405, 300),
        new(405, 265), new(450, 250), new(550, 240), new(650, 258), new(700, 280), new(690, 305),
        new(660, 370), new(655, 450), new(680, 520), new(780, 560), new(850, 600),
    };
    static readonly Vector2[] ArkeniaCoast = // DemonArkeniaCoast → ArkeniaCalderaCoast
    {
        new(940, 710), new(960, 760), new(975, 840), new(990, 910), new(970, 980),
    };
    static readonly Vector2[] CalderaCoast = // ArkeniaCalderaCoast → ErdinCalderaCoast
    {
        new(930, 1070), new(910, 1120), new(880, 1200), new(830, 1270), new(780, 1330),
        new(700, 1380), new(600, 1390), new(500, 1395),
    };
    static readonly Vector2[] ErdinCoast = // ErdinCalderaCoast → BelatErdinCoast
    {
        new(360, 1340), new(300, 1270), new(240, 1200), new(190, 1130), new(160, 1060),
    };
    static readonly Vector2[] BelatCoast = // BelatErdinCoast → DemonBelatCoast
    {
        new(110, 960), new(120, 880), new(140, 800), new(150, 720), new(200, 660), new(240, 630),
    };

    // ── 넘을 수 없는 산맥 능선: 참고 지도의 산 아이콘처럼 벨라트·아르케니아 국경 바로 동쪽을 따라 남북으로 뻗는다 ──
    static readonly Vector2[] ImpassableRidge =
    {
        new(480, 664), new(508, 712), new(519, 780), new(529, 850), new(539, 918), new(556, 962),
    };
    /// <summary>능선에서 산기슭까지 거리 (지도 좌표, 0.45 = 참고 지도 45px)</summary>
    const float ImpassableHalfWidth = 0.45f;
    /// <summary>가장 높은 봉우리 높이 (m). 평지(약 20m) 대비 확실히 솟아 "넘을 수 없음"이 보이도록 높게 잡는다.</summary>
    const float ImpassablePeakHeight = 130f;
    /// <summary>봉우리 촘촘함 (참고 지도처럼 작은 봉우리가 줄지어 서도록)</summary>
    const float ImpassablePeakFrequency = 5f;

    // ═════════════════════════ 진입점 ═════════════════════════

    /// <summary>
    /// 메뉴 실행 진입점: 현재 열린 씬에 지도를 (다시) 만들고 씬을 저장 대상으로 표시한다.
    /// </summary>
    [MenuItem("Tools/Build World Map (Current Scene)")]
    public static void BuildInCurrentScene()
    {
        BuildMap(Camera.main);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }

    /// <summary>
    /// 지도 에셋을 준비하고 현재 씬에 3D 지도를 생성한다. (기존 지도 오브젝트가 있으면 교체)
    /// InGameBuilder 에서도 씬 생성 과정 중에 호출한다.
    /// </summary>
    /// <param name="camera">지도에 맞출 카메라 (null 이면 카메라 설정 생략)</param>
    public static void BuildMap(Camera camera)
    {
        var definition = EnsureMapAssets();
        var materials = new WorldMapMaterials(
            EnsureMaterial(TerrainMaterialPath, TerrainShaderName, null),
            EnsureMaterial(SeaMaterialPath, LitShaderName, SetupSeaMaterial),
            EnsureBorderMaterial());
        var terrainData = EnsureTerrainData();

        var existing = GameObject.Find(WorldMapGenerator.RootName);
        if (existing != null)
            Object.DestroyImmediate(existing);

        var mapRoot = WorldMapGenerator.Generate(definition, terrainData, materials);

        // 생성기가 채운 높이·텍스처 배치를 에셋 파일에 기록한다.
        EditorUtility.SetDirty(terrainData);
        AssetDatabase.SaveAssets();

        if (camera != null)
            FitCamera(camera, definition, mapRoot.GetComponentInChildren<TerrainGroundSampler>());
        SetupSunLight();

        // 국경선 두께를 맞춘 카메라 위치 기준으로 바로 계산해 둔다. (저장되는 씬에서도 처음부터 보이도록)
        if (mapRoot.TryGetComponent<ScreenSpaceLineWidth>(out var lineWidth))
            lineWidth.Refresh();
    }

    /// <summary>
    /// 지형 텍스처·레이어·정의, 지도 정의 에셋을 모두 준비해 지도 정의를 돌려준다.
    /// </summary>
    static WorldMapDefinition EnsureMapAssets()
    {
        var barren = EnsureTerrain(BarrenLand);
        var plains = EnsureTerrain(Plains);
        var definition = EnsureMapDefinition(barren, plains);
        AssetDatabase.SaveAssets();
        return definition;
    }

    // ═════════════════════════ 카메라 / 조명 ═════════════════════════

    /// <summary>
    /// 카메라를 원근 카메라로 바꾸고, 이동·확대·축소 컴포넌트(MapCameraRig + MapCameraInput)를 붙여
    /// 대륙 전체를 남쪽 하늘에서 비스듬히 내려다보는 시점으로 맞춘다.
    /// </summary>
    /// <param name="camera">맞출 카메라</param>
    /// <param name="definition">지도 정의 (이동 범위·지면 높이 계산용)</param>
    /// <param name="groundSampler">카메라가 지형을 뚫지 않도록 지면 높이를 알려 줄 컴포넌트</param>
    static void FitCamera(Camera camera, WorldMapDefinition definition, TerrainGroundSampler groundSampler)
    {
        camera.orthographic = false;
        camera.fieldOfView = CameraFieldOfView;
        camera.farClipPlane = CameraFarClipInMapUnits * definition.WorldScale;

        var rig = GetOrAdd<MapCameraRig>(camera.gameObject);
        var rigSo = new SerializedObject(rig);
        rigSo.FindProperty("focus").vector3Value = definition.ToWorld(OverviewTargetInMapUnits, 0f);
        rigSo.FindProperty("distance").floatValue = OverviewDistanceInMapUnits * definition.WorldScale;
        rigSo.FindProperty("minDistance").floatValue = MinDistanceMeters;
        rigSo.FindProperty("maxDistance").floatValue = MaxDistanceInMapUnits * definition.WorldScale;
        rigSo.FindProperty("panBounds").rectValue = ContinentWorldBounds(definition);
        rigSo.FindProperty("groundHeight").floatValue = definition.Relief.landBase;
        rigSo.FindProperty("groundSampler").objectReferenceValue = groundSampler;
        rigSo.ApplyModifiedPropertiesWithoutUndo();
        rig.SnapToTarget(); // 에디터에서도 저장되는 카메라 위치를 시작 시점에 맞춘다

        var input = GetOrAdd<MapCameraInput>(camera.gameObject);
        var inputSo = new SerializedObject(input);
        inputSo.FindProperty("rig").objectReferenceValue = rig;
        inputSo.ApplyModifiedPropertiesWithoutUndo();
    }

    /// <summary>
    /// 모든 국가 영역 외곽선을 감싸는 월드 XZ 범위. 카메라가 대륙 밖 바다로 멀리 벗어나지 않게 하는 데 쓴다.
    /// </summary>
    static Rect ContinentWorldBounds(WorldMapDefinition definition)
    {
        var points = definition.Regions.SelectMany(region => region.outline).ToArray();
        if (points.Length == 0)
            return new Rect(Vector2.zero, Vector2.zero);

        var min = new Vector2(points.Min(p => p.x), points.Min(p => p.y)) * definition.WorldScale;
        var max = new Vector2(points.Max(p => p.x), points.Max(p => p.y)) * definition.WorldScale;
        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }

    /// <summary>
    /// 컴포넌트가 있으면 그대로, 없으면 새로 붙여 돌려준다. (빌더를 여러 번 실행해도 중복되지 않게)
    /// </summary>
    static T GetOrAdd<T>(GameObject target) where T : Component
    {
        return target.TryGetComponent<T>(out var component) ? component : target.AddComponent<T>();
    }

    /// <summary>
    /// 씬의 방향광(태양)을 북서쪽에서 비추도록 돌린다. 산의 동남쪽 비탈에 그늘이 져 입체감이 산다.
    /// </summary>
    static void SetupSunLight()
    {
        var sun = Object.FindObjectsByType<Light>(FindObjectsSortMode.None)
            .FirstOrDefault(light => light.type == LightType.Directional);
        if (sun == null)
        {
            Debug.LogWarning("WorldMapBuilder: 씬에 방향광이 없어 조명 방향을 맞추지 못했습니다.");
            return;
        }

        sun.transform.rotation = Quaternion.Euler(SunEuler);
    }

    // ═════════════════════════ 지형 에셋 ═════════════════════════

    /// <summary>
    /// 지형 하나의 텍스처 → TerrainLayer → TerrainDefinition 을 순서대로 준비한다.
    /// </summary>
    static TerrainDefinition EnsureTerrain(TerrainRecipe recipe)
    {
        var definitionPath = $"{DataFolder}/{recipe.AssetName}.asset";
        var existing = AssetDatabase.LoadAssetAtPath<TerrainDefinition>(definitionPath);
        if (existing != null)
            return existing;

        var texture = EnsureCroppedTexture(recipe, out var aspect);
        var layer = EnsureTerrainLayer(recipe, texture, aspect);

        EnsureFolder(DataFolder);
        var terrain = ScriptableObject.CreateInstance<TerrainDefinition>();
        AssetDatabase.CreateAsset(terrain, definitionPath);

        var so = new SerializedObject(terrain);
        so.FindProperty("displayName").stringValue = recipe.DisplayName;
        so.FindProperty("terrainLayer").objectReferenceValue = layer;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(terrain);
        return terrain;
    }

    /// <summary>
    /// 지형 텍스처로 TerrainLayer 에셋을 만든다. 잘라 낸 사진의 가로세로 비율을 지켜 무늬가 늘어나 보이지 않게 한다.
    /// </summary>
    static TerrainLayer EnsureTerrainLayer(TerrainRecipe recipe, Texture2D texture, float aspect)
    {
        var path = $"{DataFolder}/{recipe.AssetName}.terrainlayer";
        var layer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(path);
        if (layer != null)
            return layer;

        EnsureFolder(DataFolder);
        layer = new TerrainLayer
        {
            diffuseTexture = texture,
            tileSize = new Vector2(recipe.TileWidthMeters, recipe.TileWidthMeters * aspect),
            smoothness = 0f,
            metallic = 0f,
        };
        AssetDatabase.CreateAsset(layer, path);
        return layer;
    }

    /// <summary>
    /// 원본 사진에서 땅 부분을 잘라 이음매 없는(seamless) PNG 지형 텍스처로 저장하고 임포트한다.
    /// </summary>
    /// <param name="recipe">지형 설정</param>
    /// <param name="aspect">잘라 낸 텍스처의 세로/가로 비율</param>
    static Texture2D EnsureCroppedTexture(TerrainRecipe recipe, out float aspect)
    {
        var texturePath = $"{TextureFolder}/{recipe.AssetName}.png";
        var sourcePath = $"{SourceFolder}/{recipe.SourcePhoto}";

        // 임포터 설정(Read/Write)을 건드리지 않도록 원본 파일 바이트를 직접 읽어 디코딩한다.
        var source = new Texture2D(2, 2);
        source.LoadImage(File.ReadAllBytes(sourcePath));

        // 사진 기준(위에서부터) 비율 → 텍스처 픽셀(아래에서부터) 좌표로 변환
        var width = Mathf.RoundToInt(source.width * recipe.CropSize.x);
        var height = Mathf.RoundToInt(source.height * recipe.CropSize.y);
        var x = Mathf.RoundToInt(source.width * recipe.CropTopLeft.x);
        var yFromTop = Mathf.RoundToInt(source.height * recipe.CropTopLeft.y);
        var y = Mathf.Clamp(source.height - yFromTop - height, 0, source.height - height);

        var cropped = new Texture2D(width, height, TextureFormat.RGB24, false);
        cropped.SetPixels(MakeSeamless(source.GetPixels(x, y, width, height), width, height));
        cropped.Apply();

        EnsureFolder(TextureFolder);
        File.WriteAllBytes(texturePath, cropped.EncodeToPNG());
        aspect = (float)height / width;
        Object.DestroyImmediate(source);
        Object.DestroyImmediate(cropped);

        AssetDatabase.ImportAsset(texturePath, ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(texturePath);
        importer.textureType = TextureImporterType.Default;
        importer.wrapMode = TextureWrapMode.Repeat; // MakeSeamless 로 경계를 이어 두었으므로 그대로 반복한다
        importer.mipmapEnabled = true; // 멀리서 볼 때 지글거림 방지
        importer.filterMode = FilterMode.Trilinear;
        importer.anisoLevel = 8;       // 비스듬히 볼 때 먼 쪽 텍스처가 뭉개지지 않게
        importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
    }

    /// <summary>
    /// 가장자리 이음매가 보이지 않도록 텍스처를 가공한다. (반쯤 밀어 놓은 사본과 섞는 오프셋 블렌딩)
    ///
    /// 1단계(가로): 원본을 가로로 절반 민 사본과 섞는다. 좌우 가장자리 근처는 사본(가장자리끼리 이어진 부분)을,
    ///              가운데는 원본(사본의 이음매가 있는 곳)을 쓰므로 좌우로 반복해도 끊김이 없다.
    /// 2단계(세로): 1단계 결과에 같은 방법을 세로로 적용한다. 가로 이음매 없음은 세로 이동·혼합 후에도 유지된다.
    /// </summary>
    /// <param name="pixels">원본 픽셀 (아래 행부터, 행 우선)</param>
    /// <param name="width">가로 픽셀 수</param>
    /// <param name="height">세로 픽셀 수</param>
    static Color[] MakeSeamless(Color[] pixels, int width, int height)
    {
        var horizontal = BlendWithShifted(pixels, width, height, true);
        return BlendWithShifted(horizontal, width, height, false);
    }

    /// <summary>
    /// 한 축 방향으로 절반 민 사본과 원본을 가장자리 거리 기반 가중치로 섞는다.
    /// </summary>
    /// <param name="alongX">true 면 가로, false 면 세로 방향으로 처리</param>
    static Color[] BlendWithShifted(Color[] pixels, int width, int height, bool alongX)
    {
        // 가장자리에서 이 비율(반폭 대비) 안쪽까지 사본 → 원본으로 서서히 바뀐다.
        const float BlendRange = 0.6f;

        var result = new Color[pixels.Length];
        var size = alongX ? width : height;
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var sx = alongX ? (x + width / 2) % width : x;
                var sy = alongX ? y : (y + height / 2) % height;

                // 0 = 가장자리, 1 = 가운데
                var coord = alongX ? x : y;
                var centerness = 1f - Mathf.Abs(2f * (coord + 0.5f) / size - 1f);
                var originalWeight = Mathf.SmoothStep(0f, 1f, centerness / BlendRange);

                result[y * width + x] = Color.Lerp(pixels[sy * width + sx], pixels[y * width + x], originalWeight);
            }
        }

        return result;
    }

    // ═════════════════════════ 재질 / TerrainData ═════════════════════════

    /// <summary>
    /// 국경선 재질을 준비한다. 예전에 다른 셰이더(Sprites/Default)로 만들어진 재질이 있으면 전용 셰이더로 바꾼다.
    /// (전용 셰이더가 있어야 수십 km 밖에서도 국경선이 지면에 묻히지 않는다)
    /// </summary>
    static Material EnsureBorderMaterial()
    {
        var material = EnsureMaterial(BorderMaterialPath, BorderShaderName, null);
        var shader = Shader.Find(BorderShaderName);
        if (shader != null && material.shader != shader)
        {
            material.shader = shader;
            EditorUtility.SetDirty(material);
        }

        return material;
    }

    /// <summary>
    /// 재질 에셋을 준비한다. 없으면 지정 셰이더로 만들고 setup 으로 속성을 채운다.
    /// </summary>
    /// <param name="path">재질 에셋 경로</param>
    /// <param name="shaderName">사용할 셰이더 이름</param>
    /// <param name="setup">새로 만들 때 속성을 채우는 함수 (없으면 셰이더 기본값)</param>
    static Material EnsureMaterial(string path, string shaderName, System.Action<Material> setup)
    {
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material != null)
            return material;

        material = new Material(FindShader(shaderName));
        setup?.Invoke(material);
        EnsureFolder(Path.GetDirectoryName(path)?.Replace('\\', '/'));
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    /// <summary>
    /// 바다 수면 재질 속성: 바다색 + 매끈한 표면(태양 반사광).
    /// </summary>
    static void SetupSeaMaterial(Material material)
    {
        material.SetColor("_BaseColor", SeaColor);
        material.SetFloat("_Smoothness", SeaSmoothness);
        material.SetFloat("_Metallic", 0f);
    }

    /// <summary>
    /// 이름으로 셰이더를 찾는다. 없으면 경고 후 Sprites/Default 로 대체한다.
    /// </summary>
    static Shader FindShader(string shaderName)
    {
        var shader = Shader.Find(shaderName);
        if (shader != null)
            return shader;

        Debug.LogWarning($"WorldMapBuilder: '{shaderName}' 셰이더를 찾지 못해 Sprites/Default 로 대체합니다.");
        return Shader.Find("Sprites/Default");
    }

    /// <summary>
    /// 지형 높이·텍스처 배치를 담을 TerrainData 에셋을 준비한다. (내용은 생성기가 매번 덮어쓴다)
    /// TerrainData 는 씬에 직접 저장되지 않으므로 반드시 에셋이어야 한다.
    /// </summary>
    static TerrainData EnsureTerrainData()
    {
        var terrainData = AssetDatabase.LoadAssetAtPath<TerrainData>(TerrainDataPath);
        if (terrainData != null)
            return terrainData;

        EnsureFolder(DataFolder);
        terrainData = new TerrainData();
        AssetDatabase.CreateAsset(terrainData, TerrainDataPath);
        return terrainData;
    }

    // ═════════════════════════ 지도 정의 ═════════════════════════

    /// <summary>
    /// 참고 지도를 따라 그린 5개국 영역과 산맥으로 WorldMapDefinition 에셋을 만든다.
    /// 마왕국은 메마른 땅, 나머지는 평야로 매핑한다.
    /// </summary>
    static WorldMapDefinition EnsureMapDefinition(TerrainDefinition barren, TerrainDefinition plains)
    {
        var existing = AssetDatabase.LoadAssetAtPath<WorldMapDefinition>(MapDefinitionPath);
        if (existing != null)
            return existing;

        EnsureFolder(DataFolder);
        var definition = ScriptableObject.CreateInstance<WorldMapDefinition>();
        AssetDatabase.CreateAsset(definition, MapDefinitionPath);

        // 각 외곽선은 지도 둘레를 시계 방향(위에서 볼 때)으로 돈다.
        var regions = new (string country, TerrainDefinition terrain, Vector2[] outline)[]
        {
            ("DemonKingdom", barren, Outline(
                DemonBelatCoast, DemonCoast, DemonArkeniaCoast,
                Reverse(DemonArkeniaBorder), DemonBelatArkenia, Reverse(DemonBelatBorder))),

            ("BelatKingdom", plains, Outline(
                DemonBelatCoast, DemonBelatBorder, DemonBelatArkenia, BelatArkeniaBorder, BelatArkeniaErdin,
                Reverse(BelatErdinBorder), BelatErdinCoast, BelatCoast)),

            ("ArkeniaEmpire", plains, Outline(
                DemonBelatArkenia, DemonArkeniaBorder, DemonArkeniaCoast, ArkeniaCoast, ArkeniaCalderaCoast,
                Reverse(ArkeniaCalderaBorder), ErdinArkeniaCaldera, Reverse(ErdinArkeniaBorder), BelatArkeniaErdin,
                Reverse(BelatArkeniaBorder))),

            ("ErdinDuchy", plains, Outline(
                BelatErdinCoast, BelatErdinBorder, BelatArkeniaErdin, ErdinArkeniaBorder, ErdinArkeniaCaldera,
                ErdinCalderaBorder, ErdinCalderaCoast, ErdinCoast)),

            ("CalderaFederation", plains, Outline(
                ErdinArkeniaCaldera, ArkeniaCalderaBorder, ArkeniaCalderaCoast, CalderaCoast, ErdinCalderaCoast,
                Reverse(ErdinCalderaBorder))),
        };

        var so = new SerializedObject(definition);
        var list = so.FindProperty("regions");
        list.arraySize = regions.Length;
        for (var i = 0; i < regions.Length; i++)
        {
            var element = list.GetArrayElementAtIndex(i);
            var country = AssetDatabase.LoadAssetAtPath<CountryDefinition>($"{CountryFolder}/{regions[i].country}.asset");
            if (country == null)
                Debug.LogWarning($"WorldMapBuilder: 국가 에셋을 찾지 못했습니다. ({regions[i].country})");

            element.FindPropertyRelative("country").objectReferenceValue = country;
            element.FindPropertyRelative("terrain").objectReferenceValue = regions[i].terrain;
            WriteVectors(element.FindPropertyRelative("outline"), regions[i].outline);
        }

        // 넘을 수 없는 산맥
        // 직렬화 배열에 새로 추가된 요소는 C# 필드 초기값이 아닌 0 으로 채워지므로 모든 값을 직접 넣는다.
        var mountains = so.FindProperty("mountainRanges");
        mountains.arraySize = 1;
        var range = mountains.GetArrayElementAtIndex(0);
        range.FindPropertyRelative("name").stringValue = "넘을 수 없는 산맥";
        WriteVectors(range.FindPropertyRelative("ridge"), Outline(ImpassableRidge));
        range.FindPropertyRelative("halfWidth").floatValue = ImpassableHalfWidth;
        range.FindPropertyRelative("peakHeight").floatValue = ImpassablePeakHeight;
        range.FindPropertyRelative("peakFrequency").floatValue = ImpassablePeakFrequency;

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(definition);
        return definition;
    }

    /// <summary>
    /// Vector2 배열 직렬화 속성에 값을 채운다.
    /// </summary>
    static void WriteVectors(SerializedProperty property, Vector2[] values)
    {
        property.arraySize = values.Length;
        for (var i = 0; i < values.Length; i++)
            property.GetArrayElementAtIndex(i).vector2Value = values[i];
    }

    /// <summary>
    /// 점(Vector2)과 점 배열(Vector2[])을 순서대로 이어 붙인 뒤 지도 좌표로 바꾼 꼭짓점 목록을 만든다.
    /// </summary>
    /// <param name="parts">Vector2 또는 Vector2[] 를 섞어 전달 (참고 지도 픽셀 좌표)</param>
    static Vector2[] Outline(params object[] parts)
    {
        var pixels = new List<Vector2>();
        foreach (var part in parts)
        {
            switch (part)
            {
                case Vector2 point:
                    pixels.Add(point);
                    break;
                case IEnumerable<Vector2> points:
                    pixels.AddRange(points);
                    break;
            }
        }

        return pixels.Select(ToMap).ToArray();
    }

    /// <summary>
    /// 국경선을 반대 방향으로 따라갈 때 쓰는 역순 배열.
    /// </summary>
    static Vector2[] Reverse(Vector2[] points) => points.Reverse().ToArray();

    /// <summary>
    /// 참고 지도 픽셀 좌표(y 아래쪽)를 지도 좌표(y 위쪽 = 북쪽)로 바꾼다.
    /// </summary>
    static Vector2 ToMap(Vector2 pixel)
    {
        return new Vector2(
            (pixel.x - MapPixelOrigin.x) / PixelsPerUnit,
            -(pixel.y - MapPixelOrigin.y) / PixelsPerUnit);
    }

    // ═════════════════════════ 공통 ═════════════════════════

    /// <summary>
    /// "Assets/A/B" 형태의 폴더가 없으면 상위부터 차례로 만든다.
    /// </summary>
    static void EnsureFolder(string folder)
    {
        if (string.IsNullOrEmpty(folder) || AssetDatabase.IsValidFolder(folder))
            return;

        var parent = Path.GetDirectoryName(folder)?.Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
    }
}
