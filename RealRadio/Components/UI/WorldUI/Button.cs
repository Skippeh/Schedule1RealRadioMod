using System;
using System.Linq;
using ScheduleOne;
using ScheduleOne.PlayerScripts;
using UnityEngine;

namespace RealRadio.Components.WorldUI;

[RequireComponent(typeof(Collider))]
public class Button : MonoBehaviour
{
    public event Action? CursorDown;
    public event Action? CursorUp;

    private bool cursorIsPushedDown;

    private RaycastHit[] hits = new RaycastHit[2];
    private Collider collider = null!;

    void Awake()
    {
        collider = GetComponent<Collider>();
    }

    private void Update()
    {
        if (cursorIsPushedDown && !GameInput.GetButton(GameInput.ButtonCode.PrimaryClick))
            cursorIsPushedDown = false;

        if (GameInput.GetButtonDown(GameInput.ButtonCode.PrimaryClick))
        {
            if (!TestRayHit())
                return;

            CursorDown?.Invoke();
            cursorIsPushedDown = true;
        }
        else if (GameInput.GetButtonUp(GameInput.ButtonCode.PrimaryClick))
        {
            if (cursorIsPushedDown)
                CursorUp?.Invoke();

            cursorIsPushedDown = false;
        }
    }

    private bool TestRayHit()
    {
        Ray ray = PlayerCamera.Instance.Camera.ScreenPointToRay(Input.mousePosition);

        if (Physics.RaycastNonAlloc(ray, hits, maxDistance: 1f, Layers.Default.ToLayerMask()) == 0)
            return false;

        return hits.Any(hit => hit.collider == collider);
    }
}
