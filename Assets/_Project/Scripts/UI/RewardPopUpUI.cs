//Used Claude Assistant for code assistance with pop up values

using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using TMPro;

public class RewardPopUpUI : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
   [SerializeField] private CanvasGroup canvasGroup;
   [SerializeField] private RectTransform popUpTransform;
   [SerializeField] private Image rewardIcon;
   [SerializeField] private TMP_Text rewardText;

   [Header("Animation")]
   [SerializeField] private float popUpDuration = 0.25f;
   [SerializeField] private float displayDuration = 1.5f;
   [SerializeField] private float fadeDuration = 0.4f;

   private Coroutine activeRoutine;

   private void Awake()
    {
        canvasGroup.alpha = 0f;
        popUpTransform.localScale = Vector3.zero;
    }
    public void Show(Sprite icon, string message)
    {
        rewardIcon.sprite = icon;
        rewardText.text = message;

        if(activeRoutine != null)
            StopCoroutine(activeRoutine);
        activeRoutine = StartCoroutine(PopUpRoutine());
    }

    private IEnumerator PopUpRoutine()
    {
        canvasGroup.alpha = 1;

        float elapsed = 0;
        while(elapsed < popUpDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / popUpDuration);
            float scale = 1 + 0.25f * Mathf.Sin(t * Mathf.PI);
            popUpTransform.localScale = Vector3.one * Mathf.Lerp(0.6f, scale, t);
            yield return null;
        }
        popUpTransform.localScale = Vector3.one;
        yield return new WaitForSeconds(displayDuration);

        elapsed = 0;
        while(elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = 1 - Mathf.Clamp01(elapsed / fadeDuration);
            yield return null;
        }
        canvasGroup.alpha = 0;
        popUpTransform.localScale = Vector3.zero;
        activeRoutine = null;
    }
    }

