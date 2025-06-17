using System;
using ScheduleOne;
using UnityEngine;

namespace RealRadio.Components.WorldUI;

[RequireComponent(typeof(Collider))]
public class Button : MonoBehaviour
{
    public event Action? CursorDown;
    public event Action? CursorUp;

    private bool cursorIsPushedDown;

    private void OnMouseDown()
    {
        CursorDown?.Invoke();
        cursorIsPushedDown = true;
    }

    private void OnMouseUp()
    {
        if (cursorIsPushedDown)
            CursorUp?.Invoke();

        cursorIsPushedDown = false;
    }

    private void Update()
    {
        if (cursorIsPushedDown && !GameInput.GetButton(GameInput.ButtonCode.PrimaryClick))
            cursorIsPushedDown = false;
    }
}
