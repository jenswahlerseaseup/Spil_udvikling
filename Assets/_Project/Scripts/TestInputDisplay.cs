using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
#endif

public sealed class TestInputDisplay : MonoBehaviour
{
    private Vector2 move;

    private void Update()
    {
#if ENABLE_INPUT_SYSTEM
        var keyboard = Keyboard.current;
        if (keyboard == null)
        {
            move = Vector2.zero;
            return;
        }

        move = new Vector2(
            ReadAxis(keyboard.aKey, keyboard.dKey, keyboard.leftArrowKey, keyboard.rightArrowKey),
            ReadAxis(keyboard.sKey, keyboard.wKey, keyboard.downArrowKey, keyboard.upArrowKey));
#else
        move = Vector2.zero;
#endif
    }

    private void OnGUI()
    {
        GUI.Label(new Rect(24, 24, 520, 32), "Unity 2D setup test scene");
        GUI.Label(new Rect(24, 52, 520, 32), "Press WASD or arrow keys. Move vector: " + move);
#if ENABLE_INPUT_SYSTEM
        GUI.Label(new Rect(24, 80, 520, 32), "Input System package is active.");
#else
        GUI.Label(new Rect(24, 80, 720, 32), "Input System package installed, but Active Input Handling still needs switching in Project Settings.");
#endif
    }

#if ENABLE_INPUT_SYSTEM
    private static float ReadAxis(KeyControl negative, KeyControl positive, KeyControl negativeAlt, KeyControl positiveAlt)
    {
        var value = 0f;
        if (negative.isPressed || negativeAlt.isPressed)
        {
            value -= 1f;
        }

        if (positive.isPressed || positiveAlt.isPressed)
        {
            value += 1f;
        }

        return value;
    }
#endif
}
