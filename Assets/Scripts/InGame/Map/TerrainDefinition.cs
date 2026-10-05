using UnityEngine;

/// <summary>
/// 지형 한 종류(예: 평야, 메마른 땅)의 표시 정보를 담는 데이터 에셋.
/// 지도 영역은 이 에셋을 참조해 3D 지형(Unity Terrain)에 어떤 레이어로 칠해질지 결정한다.
/// 이후 지형별 이동력·생산력 같은 게임 규칙이 필요해지면 이 에셋에 필드를 추가한다.
/// </summary>
[CreateAssetMenu(menuName = "VictoriaFantasia/Map/Terrain Definition", fileName = "TerrainDefinition")]
public class TerrainDefinition : ScriptableObject
{
    [Tooltip("UI 등에 표시할 지형 이름")]
    [SerializeField] string displayName;

    [Tooltip("Unity Terrain 에 칠할 레이어 (텍스처와 타일 크기는 레이어 에셋에서 조절)")]
    [SerializeField] TerrainLayer terrainLayer;

    /// <summary>UI 에 표시할 지형 이름</summary>
    public string DisplayName => displayName;

    /// <summary>3D 지형에 칠할 레이어</summary>
    public TerrainLayer TerrainLayer => terrainLayer;
}
