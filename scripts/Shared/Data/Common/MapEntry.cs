using UnityEngine;

/// <summary>
/// 타이틀 씬과 게임 씬에서 공통으로 사용하는 맵 데이터 클래스.
/// 
/// 역할:
/// - 타이틀 씬에서는 맵 카드 UI에 표시할 이름, 이미지, 튜토리얼 여부를 제공한다.
/// - 게임 씬에서는 실제 생성할 맵 프리팹, 스폰 데이터, 제한 시간을 제공한다.
/// 
/// 중요:
/// - TitleManager의 maps 배열 순서와 GameBootstrap의 maps 배열 순서는 반드시 같아야 한다.
/// - PlayerPrefs에 저장되는 SelectedMapIndex는 이 배열의 인덱스를 기준으로 한다.
/// </summary>
[System.Serializable]
public class MapEntry
{
    [Header("ID")]
    [Tooltip("저장, 해금, 클리어 기록 등에 사용할 맵 고유 ID")]
    public string id;

    [Header("UI")]
    [Tooltip("타이틀 화면에 표시할 맵 이름")]
    public string displayName;

    [Tooltip("타이틀 화면에 표시할 맵 미리보기 이미지")]
    public Sprite preview;

    [Header("Tutorial")]
    [Tooltip("튜토리얼 맵이면 true")]
    public bool isTutorialMap;

    [Header("Game Time")]
    [Tooltip("이 맵의 제한 시간. 이 시간이 지나면 GameManager가 오버타임 상태로 진입한다.")]
    public float maxGameTime = 180f;

    [Header("Map Prefab")]
    [Tooltip("Tilemap, MapBoundary, PlayerSpawnPoint 등을 포함한 맵 프리팹")]
    public GameObject mapPrefab;

    [Header("Spawn Data")]
    [Tooltip("이 맵에서 사용할 적 스폰 데이터")]
    public SpawnDataSet spawnDataSet;
}