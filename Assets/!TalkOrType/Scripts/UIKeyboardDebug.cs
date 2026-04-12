using UnityEngine;
using Zenject;

public class UIThemeDebugInput : MonoBehaviour
{
    [Inject] private UIManager ui;

    private void Update()
    {
        // базовые цвета
        if (Input.GetKeyDown(KeyCode.Alpha1))
            ui.SetAccent(Color.red);

        if (Input.GetKeyDown(KeyCode.Alpha2))
            ui.SetAccent(Color.green);

        if (Input.GetKeyDown(KeyCode.Alpha3))
            ui.SetAccent(Color.blue);

        // темы
        if (Input.GetKeyDown(KeyCode.Q))
        {
            ui.SetTheme(
                Color.cyan,
                new Color(0.05f, 0.05f, 0.05f),
                Color.white
            );
        }

        if (Input.GetKeyDown(KeyCode.W))
        {
            ui.SetTheme(
                new Color(0.2f, 0.5f, 1f),
                Color.white,
                Color.black
            );
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            ui.SetTheme(
                new Color(1f, 0f, 1f),
                Color.black,
                Color.white
            );
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            ui.SetAccent(Random.ColorHSV());
        }

        if (Input.GetKeyDown(KeyCode.T))
        {
            ui.Modify(u =>
            {
                u.SetAccent(Random.ColorHSV(), false);
                u.SetBackground(Random.ColorHSV(), false);
            });
        }
    }
}