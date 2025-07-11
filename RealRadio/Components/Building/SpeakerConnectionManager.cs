using System;
using RealRadio.Components.Building.Buildables;
using ScheduleOne;
using ScheduleOne.DevUtilities;
using ScheduleOne.EntityFramework;
using ScheduleOne.PlayerScripts;
using ScheduleOne.UI;
using UnityEngine;

namespace RealRadio.Components.Building;

public class SpeakerConnectionManager : Singleton<SpeakerConnectionManager>
{
    public event Action<Speaker, Buildables.Radio>? SpeakerConnected;

    private bool editModeEnabled;
    private Action? finishedCallback;
    private RaycastHit[] hits = new RaycastHit[1];
    private GameObject? HoveredObject
    {
        get => hoveredObject;
        set
        {
            if (hoveredObject == value)
                return;

            hoveredObject = value;
            OnHoveredObjectChanged();
        }
    }

    private BuildableItem? HoveredBuildableItem
    {
        get => hoveredBuildableItem;
        set
        {
            if (hoveredBuildableItem == value)
                return;

            hoveredBuildableItem = value;
            OnHoveredBuildableItemChanged();
        }
    }

    private BuildableItem? SelectedBuildableItem
    {
        get => selectedBuildableItem;
        set
        {
            if (selectedBuildableItem == value)
                return;

            selectedBuildableItem = value;
            OnSelectedBuildableItemChanged();
        }
    }

    private Speaker? SelectedSpeaker => SelectedBuildableItem as Speaker;
    private Buildables.Radio? SelectedRadio => SelectedBuildableItem as Buildables.Radio ?? SelectedSpeaker?.Master;

    private Buildables.Radio? HoveredRadio => HoveredBuildableItem as Buildables.Radio ?? HoveredSpeaker?.Master;
    private Speaker? HoveredSpeaker => HoveredBuildableItem as Speaker;

    private GameObject? hoveredObject;
    private BuildableItem? hoveredBuildableItem;
    private BuildableItem? selectedBuildableItem;

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

    public void EnableEditMode(BuildableItem? initialSelectedItem = null, Action<Speaker, Buildables.Radio>? connectedCallback = null)
    {
        if (initialSelectedItem != null && initialSelectedItem is not Speaker or Buildables.Radio)
            throw new ArgumentException($"{nameof(initialSelectedItem)} ({initialSelectedItem}) must be a {nameof(Speaker)} or {nameof(Buildables.Radio)}");

        if (editModeEnabled)
            return;

        editModeEnabled = true;
        SelectedBuildableItem = initialSelectedItem;

        SpeakerConnected += connectedCallback;
        finishedCallback += OnFinished;
        void OnFinished()
        {
            SpeakerConnected -= connectedCallback;
            finishedCallback -= OnFinished;
            finishedCallback?.Invoke();
        }

        PlayerCamera.Instance.AddActiveUIElement(name);
    }

    public void DisableEditMode()
    {
        if (!editModeEnabled)
            return;

        editModeEnabled = false;
        SelectedBuildableItem = null;
        HoveredBuildableItem = null;
        HoveredObject = null;
        PlayerCamera.Instance.RemoveActiveUIElement(name);
        HUD.Instance.HideTopScreenText();

        finishedCallback?.Invoke();
    }

    void Update()
    {
        if (!editModeEnabled)
            return;

        if (Physics.RaycastNonAlloc(PlayerCamera.Instance.transform.position, PlayerCamera.Instance.transform.forward, hits, maxDistance: 2f, Layers.Default.ToLayerMask()) == 0)
            return;

        ref RaycastHit hit = ref hits[0];

        HoveredObject = hit.collider?.gameObject;

        if (!GameInput.GetButtonDown(GameInput.ButtonCode.PrimaryClick))
            return;

        if (SelectedBuildableItem == null)
        {
            SelectedBuildableItem = (BuildableItem?)HoveredSpeaker ?? HoveredRadio;
        }
        else
        {
            if (SelectedSpeaker != null && HoveredRadio != null)
            {
                SelectedSpeaker.SetMaster(HoveredRadio);
                SpeakerConnected?.Invoke(SelectedSpeaker, HoveredRadio);
                SelectedBuildableItem = null;
            }
            else if (SelectedRadio != null && HoveredSpeaker != null)
            {
                HoveredSpeaker.SetMaster(SelectedRadio);
                SpeakerConnected?.Invoke(HoveredSpeaker, SelectedRadio);
                SelectedBuildableItem = null;
            }
        }
    }

    void OnHoveredObjectChanged()
    {
        if (HoveredObject == null)
        {
            HoveredBuildableItem = null;
            return;
        }

        var buildableItem = HoveredObject?.GetComponentInParent<BuildableItem>();
        HoveredBuildableItem = buildableItem;
    }

    private void OnHoveredBuildableItemChanged()
    {
    }

    private void OnSelectedBuildableItemChanged()
    {
        if (editModeEnabled)
            HUD.Instance.ShowTopScreenText(GetHudText());
    }

    private string GetHudText()
    {
        if (SelectedBuildableItem == null)
        {
            return "Select a speaker or radio to connect";
        }
        else
        {
            if (SelectedSpeaker != null)
                return $"Select a radio or another connected speaker to connect to {GetItemName(SelectedSpeaker)}";

            if (SelectedRadio != null)
                return $"Select a speaker to connect to {GetItemName(SelectedRadio)}";
        }

        return "Unreachable";
    }

    private string GetItemName(BuildableItem item)
    {
        return item.ItemInstance.Definition.Name;
    }
}
