using System;
using System.Linq;
using ScheduleOne;
using ScheduleOne.Audio;
using ScheduleOne.PlayerScripts;
using UnityEngine;

namespace RealRadio.Components.WorldUI;

[RequireComponent(typeof(Collider))]
public class Button : MonoBehaviour
{
    public event Action? CursorDown;
    public event Action? CursorUp;

    public Vector3 ClickOffset = -Vector3.forward * 0.01f;

    public AudioSourceController? CursorDownSound;
    public AudioSourceController? CursorUpSound;

    private bool cursorIsPushedDown;

    private RaycastHit[] hits = new RaycastHit[2];
    private Collider collider = null!;

    private Vector3 initialPosition;

    void Awake()
    {
        collider = GetComponent<Collider>();
        initialPosition = transform.localPosition;

        CursorDown += OnCursorDown;
        CursorUp += OnCursorUp;
    }

    private void OnCursorDown()
    {
        transform.localPosition = initialPosition + ClickOffset;

        CursorUpSound?.Stop();
        CursorDownSound?.Play();
    }

    private void OnCursorUp()
    {
        transform.localPosition = initialPosition;

        CursorDownSound?.Stop();
        CursorUpSound?.Play();
    }

    private void Update()
    {
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

        if (cursorIsPushedDown && !GameInput.GetButton(GameInput.ButtonCode.PrimaryClick))
            cursorIsPushedDown = false;
    }

    private bool TestRayHit()
    {
        Ray ray = PlayerCamera.Instance.Camera.ScreenPointToRay(Input.mousePosition);

        if (Physics.RaycastNonAlloc(ray, hits, maxDistance: 1f, Layers.Default.ToLayerMask()) == 0)
            return false;

        return hits.Any(hit => hit.collider == collider);
    }
}
