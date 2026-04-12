using UnityEngine;
using System;

public class UIManager
{
    public Color AccentColor { get; private set; }
    public Color BackgroundColor { get; private set; }
    public Color TextColor { get; private set; }

    public event Action OnThemeChanged;

    private bool suppressNotify;

    public UIManager(Color accent, Color background, Color text)
    {
        AccentColor = accent;
        BackgroundColor = background;
        TextColor = text;
    }

    // ---------------------------
    // SINGLE SET
    // ---------------------------
    public void SetAccent(Color color, bool notify = true)
    {
        if (AccentColor == color) return;
        AccentColor = color;

        if (notify) Notify();
    }

    public void SetBackground(Color color, bool notify = true)
    {
        if (BackgroundColor == color) return;
        BackgroundColor = color;

        if (notify) Notify();
    }

    public void SetText(Color color, bool notify = true)
    {
        if (TextColor == color) return;
        TextColor = color;

        if (notify) Notify();
    }

    // ---------------------------
    // BATCH
    // ---------------------------
    public void SetTheme(Color accent, Color background, Color text)
    {
        suppressNotify = true;

        SetAccent(accent, false);
        SetBackground(background, false);
        SetText(text, false);

        suppressNotify = false;
        Notify();
    }

    public void Modify(Action<UIManager> modifier)
    {
        suppressNotify = true;
        modifier(this);
        suppressNotify = false;

        Notify();
    }

    private void Notify()
    {
        if (suppressNotify) return;
        OnThemeChanged?.Invoke();
    }
}