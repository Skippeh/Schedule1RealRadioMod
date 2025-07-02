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
    public event Action? Click;

    public Vector3 ClickOffset = -Vector3.forward * 0.01f;

    public AudioSourceController? CursorDownSound;
    public AudioSourceController? CursorUpSound;

    private bool cursorIsPushedDown;

    private RaycastHit[] hits = new RaycastHit[8];
    private Collider collider = null!;

    private Vector3 initialPosition;
    private Vector3 targetPosition;
    private float lerpSpeed;

    void Awake()
    {
        collider = GetComponent<Collider>();
        initialPosition = transform.localPosition;
        targetPosition = initialPosition;

        CursorDown += OnCursorDown;
        CursorUp += OnCursorUp;
    }

    private void OnCursorDown()
    {
        targetPosition = transform.localPosition + ClickOffset;
        lerpSpeed = 80f;

        CursorUpSound?.Stop();
        CursorDownSound?.Play();
    }

    private void OnCursorUp()
    {
        targetPosition = initialPosition;
        lerpSpeed = 20f;

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
            {
                cursorIsPushedDown = false;
                CursorUp?.Invoke();

                if (TestRayHit())
                    Click?.Invoke();
            }
        }

        if (cursorIsPushedDown && !GameInput.GetButton(GameInput.ButtonCode.PrimaryClick))
        {
            cursorIsPushedDown = false;
            CursorUp?.Invoke();
        }

        transform.localPosition = Vector3.Lerp(transform.localPosition, targetPosition, Time.deltaTime * lerpSpeed);
    }

    private bool TestRayHit()
    {
        Ray ray = PlayerCamera.Instance.Camera.ScreenPointToRay(Input.mousePosition);

        int numHits = Physics.RaycastNonAlloc(ray, hits, maxDistance: 1f, Layers.Default.ToLayerMask());

        if (numHits == 0)
            return false;

        return hits.Take(numHits).Any(hit => hit.collider == collider);
    }
}
