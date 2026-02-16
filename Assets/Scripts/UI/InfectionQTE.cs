using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;
using UnityEngine.Windows;
using System.Collections;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class InfectionQTE : MonoBehaviour
{
    [Header("QTE parameters")]
    public int requiredPresses = 10;        // cuántas pulsaciones necesita el jugador
    public float qteDuration = 3f;          // tiempo total del QTE

    [Header("UI")]
    public GameObject qtePanel;             // panel root del QTE
    public Image progressBar;               // barra que muestra progreso
    public Image qteButtonImage;
    public Sprite qteButtonSprite;
    public Sprite qteButtonSpritePressed;

    [Header("Results")]
    public UnityEvent OnQTESuccess;
    public UnityEvent OnQTEFail;

    int currentPresses;
    float remainingTime;
    bool qteActive;

    Infectable targetInfectable;

    void Awake()
    {
        HideUI();
    }

    void Update()
    {
        if (!qteActive) return;

        // Input: aceptar InputSystem or classic
        bool pressed = false;


        var myInput = UnifiedInput.Instance;
        if (myInput.WasPressedThisFrame(myInput.meeleInput)) pressed = true;

        if (pressed)
        {
            if (qteButtonImage != null)
            {
                qteButtonImage.sprite = qteButtonSpritePressed;
                StartCoroutine(ResetSprite());
            }
            currentPresses++;
            UpdateUIProgress();
            if (currentPresses >= requiredPresses)
            {
                Succeed();
                return;
            }
        }

        remainingTime -= Time.deltaTime;
        UpdateUITime();

        if (remainingTime <= 0f)
        {
            Fail();
        }
    }

    void UpdateUIProgress()
    {

    }

    void UpdateUITime()
    {
        if (progressBar) progressBar.fillAmount = Mathf.Clamp01(remainingTime / qteDuration);
    }

    private IEnumerator ResetSprite()
    {
        yield return new WaitForSeconds(0.1f);
        qteButtonImage.sprite = qteButtonSprite;
    }
    public void StartQTE(Infectable target, int required, float duration)
    {
        if (target == null) return;
        targetInfectable = target;
        requiredPresses = required;
        qteDuration = duration;

        currentPresses = 0;
        remainingTime = qteDuration;
        qteActive = true;
        ShowUI();
        UpdateUIProgress();
        UpdateUITime();
    }

    public void CancelQTE()
    {
        qteActive = false;
        HideUI();
    }

    void Succeed()
    {
        qteActive = false;
        HideUI();
        OnQTESuccess?.Invoke();

        // Al tener éxito, aplicamos probabilidad de que NO se infecte (si quieres)
        // Por convención: éxito evita infección. Si quieres que éxito reduzca la probabilidad, cámbialo.
    }

    void Fail()
    {
        qteActive = false;
        HideUI();
        OnQTEFail?.Invoke();

        // Si falla, aplicamos infección al target (usamos la probabilidad base del Infectable)
        if (targetInfectable != null)
        {
            float chance = targetInfectable.baseInfectionChance;
            targetInfectable.TryApplyInfection(chance, -1f);
        }
    }

    void ShowUI()
    {
        if (qtePanel) qtePanel.SetActive(true);
    }

    void HideUI()
    {
        if (qtePanel) qtePanel.SetActive(false);
    }
}
