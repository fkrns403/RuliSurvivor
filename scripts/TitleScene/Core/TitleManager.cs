using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 타이틀 화면 전체 관리.
/// 
/// 역할:
/// - 캐릭터 선택
/// - 맵 카드 자동 생성
/// - 맵 선택 시 Tutorial 여부 확인
/// - GameSessionData에 선택 정보 저장
/// - Loading 씬을 통해 Game 씬으로 이동
/// 
/// 이번 수정 핵심:
/// - SelectMap 내부에 상세 로그 추가
/// - TutorialPopup이 null이거나 비활성/세팅 실패여도 게임 시작 가능
/// - startRequested / tutorialPopupOpen 상태가 꼬였을 때 원인을 Console에서 바로 확인 가능
/// - MapCard 프리팹 생성 후 Setup을 확실히 호출
/// </summary>
public class TitleManager : MonoBehaviour
{
    private const string KEY_CHAR = "SelectedCharacterIndex";
    private const string KEY_MAP = "SelectedMapIndex";

    [Header("게임 씬 이름")]
    [SerializeField] private string gameSceneName = "Game";

    [Header("캐릭터 / 맵 데이터")]
    [SerializeField] private CharacterEntry[] characters;
    [SerializeField] private MapEntry[] maps;

    [Header("캐릭터 정보 팝업 UI")]
    [SerializeField] private GameObject characterInfoPopupRoot;
    [SerializeField] private Image characterPortrait;
    [SerializeField] private Image characterIcon;
    [SerializeField] private Text characterNameText;
    [SerializeField] private Text passiveText;
    [SerializeField] private Text weaponText;

    [Header("맵 카드 생성 UI")]
    [SerializeField] private Transform mapCardRoot;
    [SerializeField] private MapCardUI mapCardPrefab;

    [Header("튜토리얼 팝업")]
    [SerializeField] private TutorialPopup tutorialPopup;

    [Header("선택 강조")]
    [SerializeField] private GameObject[] characterSelectedMarks;

    [Header("Debug")]
    [SerializeField] private bool verboseLog = true;

    private readonly List<MapCardUI> mapCards = new List<MapCardUI>();

    private int selectedCharacterIndex;
    private int selectedMapIndex;

    private bool startRequested;
    private bool tutorialPopupOpen;

    private void Awake()
    {
        selectedCharacterIndex = LoadSafeIndex(KEY_CHAR, characters);
        selectedMapIndex = LoadSafeIndex(KEY_MAP, maps);

        startRequested = false;
        tutorialPopupOpen = false;
    }

    private void Start()
    {
        Time.timeScale = 1f;

        if (SettingsManager.Instance != null)
            SettingsManager.Instance.Apply();

        if (AudioManager.instance != null)
            AudioManager.instance.PlayBGM(true);

        BuildMapCards();
        ApplyCharacterUI(selectedCharacterIndex);
        ApplyCharacterMarks();
        ApplyMapCardSelection();

        if (characterInfoPopupRoot != null)
            characterInfoPopupRoot.SetActive(false);
    }

    private int LoadSafeIndex<T>(string key, T[] array)
    {
        if (array == null || array.Length == 0)
            return 0;

        return Mathf.Clamp(PlayerPrefs.GetInt(key, 0), 0, array.Length - 1);
    }

    private void BuildMapCards()
    {
        mapCards.Clear();

        if (mapCardRoot == null)
        {
            Debug.LogError("TitleManager: Map Card Root가 비어 있습니다.", this);
            return;
        }

        if (mapCardPrefab == null)
        {
            Debug.LogError("TitleManager: Map Card Prefab이 비어 있습니다.", this);
            return;
        }

        if (maps == null || maps.Length == 0)
        {
            Debug.LogError("TitleManager: maps 배열이 비어 있습니다.", this);
            return;
        }

        for (int i = mapCardRoot.childCount - 1; i >= 0; i--)
            Destroy(mapCardRoot.GetChild(i).gameObject);

        for (int i = 0; i < maps.Length; i++)
        {
            MapEntry data = maps[i];

            if (data == null)
            {
                Debug.LogWarning($"TitleManager: maps[{i}]가 null입니다.", this);
                continue;
            }

            MapCardUI card = Instantiate(mapCardPrefab, mapCardRoot);
            card.gameObject.SetActive(true);
            card.Setup(this, i, data);

            mapCards.Add(card);

            Log($"맵 카드 생성 완료 / index={i}, name={data.displayName}, tutorial={data.isTutorialMap}");
        }
    }

    public void SelectCharacter(int index)
    {
        if (startRequested)
            return;

        if (characters == null || characters.Length == 0)
            return;

        if (index < 0 || index >= characters.Length)
            return;

        CharacterEntry entry = characters[index];

        if (entry == null)
            return;

        selectedCharacterIndex = index;

        PlayerPrefs.SetInt(KEY_CHAR, selectedCharacterIndex);
        PlayerPrefs.Save();

        ApplyCharacterUI(index);
        ApplyCharacterMarks();

        if (characterInfoPopupRoot != null)
            characterInfoPopupRoot.SetActive(true);

        if (AudioManager.instance != null)
            AudioManager.instance.PlaySfx(AudioManager.Sfx.Select);
    }

    public void CloseCharacterPopup()
    {
        if (characterInfoPopupRoot != null)
            characterInfoPopupRoot.SetActive(false);
    }

    private void ApplyCharacterUI(int index)
    {
        if (characters == null || characters.Length == 0)
            return;

        if (index < 0 || index >= characters.Length)
            return;

        CharacterEntry c = characters[index];

        if (c == null)
            return;

        if (characterPortrait != null)
            characterPortrait.sprite = c.portrait;

        if (characterIcon != null)
            characterIcon.sprite = c.icon;

        if (characterNameText != null)
            characterNameText.text = c.displayName;

        if (passiveText != null)
            passiveText.text = c.passiveDesc;

        if (weaponText != null)
            weaponText.text = c.weaponDesc;
    }

    private void ApplyCharacterMarks()
    {
        if (characterSelectedMarks == null)
            return;

        for (int i = 0; i < characterSelectedMarks.Length; i++)
        {
            if (characterSelectedMarks[i] == null)
                continue;

            characterSelectedMarks[i].SetActive(i == selectedCharacterIndex);
        }
    }

    public void SelectMap(int index)
    {
        Log($"SelectMap 호출됨 / index={index}");

        if (startRequested)
        {
            Debug.LogWarning("TitleManager: 이미 게임 시작 요청이 진행 중입니다.", this);
            return;
        }

        if (tutorialPopupOpen)
        {
            Debug.LogWarning("TitleManager: 튜토리얼 팝업 처리 중이라 맵 선택을 무시합니다.", this);
            return;
        }

        if (maps == null || maps.Length == 0)
        {
            Debug.LogError("TitleManager: maps 배열이 비어 있습니다.", this);
            return;
        }

        if (index < 0 || index >= maps.Length)
        {
            Debug.LogError($"TitleManager: 맵 index 범위 오류 / index={index}, maps.Length={maps.Length}", this);
            return;
        }

        MapEntry map = maps[index];

        if (map == null)
        {
            Debug.LogError($"TitleManager: maps[{index}]가 null입니다.", this);
            return;
        }

        selectedMapIndex = index;

        PlayerPrefs.SetInt(KEY_MAP, selectedMapIndex);
        PlayerPrefs.Save();

        ApplyMapCardSelection();

        Log($"선택된 맵 / index={index}, name={map.displayName}, tutorial={map.isTutorialMap}");

        if (AudioManager.instance != null)
            AudioManager.instance.PlaySfx(AudioManager.Sfx.Select);

        if (map.isTutorialMap)
        {
            TryOpenTutorialOrStartGame();
            return;
        }

        StartGameInternal();
    }

    private void TryOpenTutorialOrStartGame()
    {
        Log("튜토리얼 맵 분기 진입");

        if (tutorialPopup == null)
        {
            Debug.LogWarning("TitleManager: TutorialPopup이 연결되지 않아 바로 게임을 시작합니다.", this);
            StartGameInternal();
            return;
        }

        tutorialPopupOpen = true;

        Log("TutorialPopup.Show 호출");

        tutorialPopup.Show(() =>
        {
            Log("TutorialPopup 완료 콜백 실행");

            tutorialPopupOpen = false;
            StartGameInternal();
        });
    }

    private void ApplyMapCardSelection()
    {
        for (int i = 0; i < mapCards.Count; i++)
        {
            if (mapCards[i] == null)
                continue;

            mapCards[i].SetSelected(i == selectedMapIndex);
        }
    }

    private void SetMapCardsInteractable(bool interactable)
    {
        for (int i = 0; i < mapCards.Count; i++)
        {
            if (mapCards[i] == null)
                continue;

            mapCards[i].SetInteractable(interactable);
        }
    }

    private void StartGameInternal()
    {
        Log("StartGameInternal 호출됨");

        if (startRequested)
        {
            Debug.LogWarning("TitleManager: StartGameInternal이 중복 호출되었습니다.", this);
            return;
        }

        if (characters == null || characters.Length == 0)
        {
            Debug.LogError("TitleManager: characters 배열이 비어 있어 게임을 시작할 수 없습니다.", this);
            return;
        }

        if (maps == null || maps.Length == 0)
        {
            Debug.LogError("TitleManager: maps 배열이 비어 있어 게임을 시작할 수 없습니다.", this);
            return;
        }

        selectedCharacterIndex = Mathf.Clamp(selectedCharacterIndex, 0, characters.Length - 1);
        selectedMapIndex = Mathf.Clamp(selectedMapIndex, 0, maps.Length - 1);

        startRequested = true;
        SetMapCardsInteractable(false);

        GameSessionData.SetSelection(selectedCharacterIndex, selectedMapIndex);

        PlayerPrefs.SetInt(KEY_CHAR, selectedCharacterIndex);
        PlayerPrefs.SetInt(KEY_MAP, selectedMapIndex);
        PlayerPrefs.Save();

        Log($"게임 시작 정보 저장 완료 / character={selectedCharacterIndex}, map={selectedMapIndex}, scene={gameSceneName}");

        if (LoadingManager.Instance == null)
        {
            Debug.LogError("TitleManager: LoadingManager.Instance가 없습니다. Title 씬에 LoadingManager가 필요합니다.", this);
            startRequested = false;
            SetMapCardsInteractable(true);
            return;
        }

        LoadingManager.LoadScene(gameSceneName);
    }

    private void Log(string message)
    {
        if (!verboseLog)
            return;

        Debug.Log($"TitleManager: {message}", this);
    }
}