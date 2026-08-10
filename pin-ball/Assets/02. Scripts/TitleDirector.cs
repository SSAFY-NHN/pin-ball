using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections;

public class TitleDirector : MonoBehaviour
{
    [SerializeField] private GameObject logo;
    [SerializeField] private GameObject startButton;

    private void Start()
    {
        logo.transform.localScale = Vector3.zero;
        logo.SetActive(false);
        startButton.SetActive(false);
        startButton.GetComponent<Button>().onClick.AddListener(OnClickStart);

        StartCoroutine(ShowUIRoutine());
    }

    private IEnumerator ShowUIRoutine()
    {
        logo.SetActive(true);

        // 시작 후 총 1초가 될 때까지 대기
        yield return new WaitForSeconds(2f);

        // Scale 0 -> 1, 살짝 오버슈트되며 바운스
        logo.transform
            .DOScale(new Vector3(0.65f, 0.65f, 0.65f), 0.5f)
            .SetEase(Ease.OutBack);

        yield return new WaitForSeconds(1f);
        startButton.SetActive(true);
    }

    private void OnClickStart()
    {
        App.Get<SceneManager>().Load(ESceneName.Game);
    }
}
