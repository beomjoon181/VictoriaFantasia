using UnityEngine;

/// <summary>
/// 지도 위 국가 영역 하나를 나타내는 컴포넌트. (국경선 오브젝트의 부모에 붙는다)
/// 어떤 국가의 영역이고 어떤 지형이 입혀졌는지를 보관해, 이후 영역 클릭·하이라이트 같은 기능이 참조할 수 있게 한다.
/// </summary>
public class NationRegionView : MonoBehaviour
{
    [Tooltip("이 영역을 소유한 국가")]
    [SerializeField] CountryDefinition country;

    [Tooltip("이 영역에 입혀진 지형")]
    [SerializeField] TerrainDefinition terrain;

    /// <summary>영역 소유 국가</summary>
    public CountryDefinition Country => country;

    /// <summary>영역 지형</summary>
    public TerrainDefinition Terrain => terrain;

    /// <summary>
    /// 생성 직후 국가·지형 정보를 주입한다. (WorldMapGenerator 전용)
    /// </summary>
    public void Initialize(CountryDefinition owner, TerrainDefinition terrainType)
    {
        country = owner;
        terrain = terrainType;
    }
}
