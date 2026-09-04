using UnityEngine;

/// <summary>
/// 씬 이동 중 선택 정보를 안전하게 전달하는 런타임 데이터 저장소.
/// 
/// 왜 필요한가?
/// - PlayerPrefs는 "저장용"이다.
/// - Title -> Loading -> Game 사이의 즉시 전달값까지 PlayerPrefs에만 의존하면,
///   이전 실행의 선택값이 남거나 저장 타이밍이 꼬였을 때 잘못된 캐릭터/맵이 적용될 수 있다.
/// 
/// 이 클래스의 역할:
/// 1. 이번 실행에서 선택한 캐릭터 인덱스 저장
/// 2. 이번 실행에서 선택한 맵 인덱스 저장
/// 3. Loading 씬을 거쳐도 static 데이터는 유지되므로 Game 씬에서 안전하게 읽을 수 있다.
/// 
/// 주의:
/// - 게임을 완전히 종료하면 static 값은 사라진다.
/// - 그래서 PlayerPrefs는 백업/마지막 선택 저장용으로 계속 같이 사용한다.
/// </summary>
public static class GameSessionData
{
    /// <summary>
    /// 이번 런타임에서 유효한 선택값이 있는지 여부.
    /// false이면 GameBootstrap은 PlayerPrefs 값을 fallback으로 사용한다.
    /// </summary>
    public static bool HasSelection { get; private set; }

    /// <summary>
    /// 선택된 캐릭터 인덱스.
    /// TitleManager.characters 배열 순서와 GameBootstrap.characters 배열 순서가 같아야 한다.
    /// </summary>
    public static int SelectedCharacterIndex { get; private set; }

    /// <summary>
    /// 선택된 맵 인덱스.
    /// TitleManager.maps 배열 순서와 GameBootstrap.maps 배열 순서가 같아야 한다.
    /// </summary>
    public static int SelectedMapIndex { get; private set; }

    /// <summary>
    /// TitleManager에서 게임 시작 직전에 호출한다.
    /// </summary>
    public static void SetSelection(int characterIndex, int mapIndex)
    {
        SelectedCharacterIndex = Mathf.Max(0, characterIndex);
        SelectedMapIndex = Mathf.Max(0, mapIndex);
        HasSelection = true;
    }

    /// <summary>
    /// 테스트나 타이틀 초기화가 필요할 때 선택값을 비운다.
    /// 보통은 굳이 호출하지 않아도 된다.
    /// </summary>
    public static void Clear()
    {
        HasSelection = false;
        SelectedCharacterIndex = 0;
        SelectedMapIndex = 0;
    }
}