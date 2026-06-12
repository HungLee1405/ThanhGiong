using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DayTransitionUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject transitionPanel;
    public Image blackScreen;
    public TMP_Text dayText;

    [Header("Settings")]
    public float fadeDuration = 1f;
    public float textHoldDuration = 1.2f;

    private void Start()
    {
        if (transitionPanel != null)
        {
            transitionPanel.SetActive(true);
        }

        SetAlpha(0f);

        if (dayText != null)
        {
            dayText.text = "";
        }
    }

    public IEnumerator PlayDayTransition(int newDay)
    {
        if (transitionPanel != null)
        {
            transitionPanel.SetActive(true);
        }

        if (dayText != null)
        {
            dayText.text = "";
        }

        yield return Fade(0f, 1f);

        if (dayText != null)
        {
            dayText.text = "Ngày " + newDay;
        }

        yield return new WaitForSeconds(textHoldDuration);

        yield return Fade(1f, 0f);

        if (dayText != null)
        {
            dayText.text = "";
        }
    }

    private IEnumerator Fade(float fromAlpha, float toAlpha)
    {
        float timer = 0f;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;

            float t = timer / fadeDuration;
            float alpha = Mathf.Lerp(fromAlpha, toAlpha, t);

            SetAlpha(alpha);

            yield return null;
        }

        SetAlpha(toAlpha);
    }

    private void SetAlpha(float alpha)
    {
        if (blackScreen != null)
        {
            Color color = blackScreen.color;
            color.a = alpha;
            blackScreen.color = color;
        }

        if (dayText != null)
        {
            Color textColor = dayText.color;
            textColor.a = alpha;
            dayText.color = textColor;
        }
    }
}