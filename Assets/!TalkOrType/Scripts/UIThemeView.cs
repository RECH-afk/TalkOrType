using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Zenject;
using DG.Tweening;

public class UIThemeView : MonoBehaviour
{
    public enum ThemeType
    {
        Background,
        Accent,
        Text,
        InputField
    }

    [Header("Type")]
    [SerializeField] private ThemeType type;

    [Header("Animation")]
    [SerializeField] private float duration = 0.25f;
    [SerializeField] private Ease ease = Ease.OutQuad;

    private UIManager ui;

    private Graphic graphic;
    private TMP_Text text;
    private TMP_InputField input;
    private Image inputBackground;

    private Tween tween;

    // ---------------------------
    // INIT
    // ---------------------------
    [Inject]
    public void Construct(UIManager manager)
    {
        ui = manager;
    }

    private void Awake()
    {
        TryGetComponent(out graphic);
        TryGetComponent(out text);
        TryGetComponent(out input);

        if (input != null)
        {
            inputBackground = input.GetComponent<Image>();

            if (inputBackground == null)
                inputBackground = input.GetComponentInParent<Image>();
        }
    }

    private void OnEnable()
    {
        ApplyInstant();

        if (ui != null)
            ui.OnThemeChanged += ApplyAnimated;
    }

    private void OnDisable()
    {
        if (ui != null)
            ui.OnThemeChanged -= ApplyAnimated;

        tween?.Kill();
    }

    // ---------------------------
    // APPLY INSTANT
    // ---------------------------
    private void ApplyInstant()
    {
        if (ui == null) return;

        switch (type)
        {
            case ThemeType.Background:
                if (graphic != null)
                    graphic.color = ui.BackgroundColor;
                break;

            case ThemeType.Accent:
                if (graphic != null)
                    graphic.color = ui.AccentColor;
                break;

            case ThemeType.Text:
                if (text != null)
                    text.color = ui.TextColor;
                break;

            case ThemeType.InputField:
                ApplyInputInstant();
                break;
        }
    }

    // ---------------------------
    // APPLY ANIMATED
    // ---------------------------
    private void ApplyAnimated()
    {
        if (ui == null) return;

        tween?.Kill();

        switch (type)
        {
            case ThemeType.Background:
                AnimateGraphic(ui.BackgroundColor);
                break;

            case ThemeType.Accent:
                AnimateGraphic(ui.AccentColor);
                break;

            case ThemeType.Text:
                AnimateText(ui.TextColor);
                break;

            case ThemeType.InputField:
                AnimateInput();
                break;
        }
    }

    // ---------------------------
    // GRAPHICS
    // ---------------------------
    private void AnimateGraphic(Color target)
    {
        if (graphic == null) return;

        tween = graphic
            .DOColor(target, duration)
            .SetEase(ease);
    }

    private void AnimateText(Color target)
    {
        if (text == null) return;

        tween = text
            .DOColor(target, duration)
            .SetEase(ease);
    }

    // ---------------------------
    // INPUT FIELD
    // ---------------------------
    private void ApplyInputInstant()
    {
        if (input == null) return;

        Color bg = ui.AccentColor;
        Color textColor = GetReadableTextColor(bg);

        // 🔥 фон = accent
        if (inputBackground != null)
            inputBackground.color = bg;

        // текст
        if (input.textComponent != null)
            input.textComponent.color = textColor;

        // placeholder
        if (input.placeholder is TMP_Text placeholder)
        {
            placeholder.color = new Color(
                textColor.r,
                textColor.g,
                textColor.b,
                0.5f);
        }

        // caret
        input.caretColor = textColor;

        // selection
        input.selectionColor = new Color(
            textColor.r,
            textColor.g,
            textColor.b,
            0.3f);
    }

    private void AnimateInput()
    {
        if (input == null) return;

        Color bg = ui.AccentColor;
        Color textColor = GetReadableTextColor(bg);

        // фон
        if (inputBackground != null)
        {
            inputBackground
                .DOColor(bg, duration)
                .SetEase(ease);
        }

        // текст
        if (input.textComponent != null)
        {
            input.textComponent
                .DOColor(textColor, duration)
                .SetEase(ease);
        }

        // placeholder
        if (input.placeholder is TMP_Text placeholder)
        {
            Color target = new Color(
                textColor.r,
                textColor.g,
                textColor.b,
                0.5f);

            placeholder
                .DOColor(target, duration)
                .SetEase(ease);
        }

        // caret (без tween)
        input.caretColor = textColor;

        // selection
        input.selectionColor = new Color(
            textColor.r,
            textColor.g,
            textColor.b,
            0.3f);
    }

    // ---------------------------
    // UTILS
    // ---------------------------
    private Color GetReadableTextColor(Color bg)
    {
        float luminance = (0.299f * bg.r + 0.587f * bg.g + 0.114f * bg.b);
        return luminance > 0.5f ? Color.black : Color.white;
    }
}