using UnityEngine;

/// <summary>
/// 승리/패배 결과 UI
/// - titles[0] = Lose 화면
/// - titles[1] = Win 화면
/// </summary>
public class ResultUI : MonoBehaviour
{
    [SerializeField] private GameObject[] titles;

    public void ShowLose()
    {
        if (titles == null || titles.Length == 0) return;
        if (titles.Length > 0 && titles[0] != null) titles[0].SetActive(true);
        if (titles.Length > 1 && titles[1] != null) titles[1].SetActive(false);
        gameObject.SetActive(true);
    }

    public void ShowWin()
    {
        if (titles == null || titles.Length == 0) return;
        if (titles.Length > 0 && titles[0] != null) titles[0].SetActive(false);
        if (titles.Length > 1 && titles[1] != null) titles[1].SetActive(true);
        gameObject.SetActive(true);
    }

    public void HideAll()
    {
        if (titles != null)
        {
            foreach (var t in titles)
                if (t != null) t.SetActive(false);
        }
        gameObject.SetActive(false);
    }
}