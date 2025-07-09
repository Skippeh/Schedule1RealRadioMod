using System;
using RealRadio.Components.Building.Buildables;
using ScheduleOne;
using ScheduleOne.DevUtilities;
using ScheduleOne.PlayerScripts;
using UnityEngine;

namespace RealRadio.Components.Building;

public class SpeakerConnectionManager : Singleton<SpeakerConnectionManager>
{
    public event Action<Speaker, Buildables.Radio>? SpeakerConnected;
    public event Action<Speaker, Buildables.Radio>? SpeakerDisconnected;

    private bool editModeEnabled;
    private GameObject? source;
    private Action? finishedCallback;

    void OnEnable()
    {
        GameInput.RegisterExitListener(OnExitInput);
    }

    void OnDisable()
    {
        GameInput.DeregisterExitListener(OnExitInput);
    }

    private void OnExitInput(ExitAction exitAction)
    {
        if (!editModeEnabled || exitAction.Used || exitAction.exitType != ExitType.Escape)
            return;

        exitAction.Used = true;
        DisableEditMode();
    }

    public void EnableEditMode(GameObject? initialSource = null, Action? finishedCallback = null)
    {
        if (editModeEnabled)
            return;

        editModeEnabled = true;
        source = initialSource;

        this.finishedCallback += OnFinished;
        void OnFinished()
        {
            this.finishedCallback -= OnFinished;
            finishedCallback?.Invoke();
        }

        PlayerCamera.Instance.AddActiveUIElement(name);
    }

    public void DisableEditMode()
    {
        if (!editModeEnabled)
            return;

        editModeEnabled = false;
        source = null;
        PlayerCamera.Instance.RemoveActiveUIElement(name);

        finishedCallback?.Invoke();
    }

    void Update()
    {
        if (!editModeEnabled)
            return;
    }
}
