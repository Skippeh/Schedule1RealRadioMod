using System;
using System.Collections.Generic;
using System.Linq;
using RealRadio.Components.UI;
using RealRadio.Events;
using ScheduleOne.Interaction;
using ScheduleOne.UI;
using UnityEngine;
using UnityEngine.Events;

namespace RealRadio.Components.Building;

public class InteractableOptions : MonoBehaviour
{
    public const float MaxHoldTimeBeforeShowOptions = 0.2f;

    /// <summary>
    /// The interactable object to listen for interactions on.
    /// If it's null the first interactable object found in self or children will be used.
    /// </summary>
    public InteractableObject InteractableObject = null!;

    /// <summary>
    /// Called when the interactable object is interacted with.
    /// The string is the id of the option that was selected.
    /// </summary>
    public UnityAction<string>? OnInteract;

    /// <summary>
    /// Called when the interactable object is interacted with.
    ///
    /// The option is the default option (if any), and the event ref data is the interaction text to show.
    /// </summary>
    public Action<InteractableOption?, EventRefData<string>>? OnUpdateInteractionText;

    /// <summary>
    /// The options to show when the interactable object is interacted with.
    /// The first option is always the default one that is chosen if the player does not hold interact to select a different option.
    /// </summary>
    [field: SerializeField] public List<InteractableOption> Options { get; set; } = [];

    private float? heldTime;
    private bool showingOptions;
    private InteractableOption? selectedOption;

    private void Start()
    {
        if (!InteractableObject)
        {
            InteractableObject = GetComponentInChildren<InteractableObject>() ?? throw new InvalidOperationException("No InteractableObject component found in self or children");
        }

        InteractableObject.onInteractStart.AddListener(OnInteractStart);
        InteractableObject.onInteractEnd.AddListener(OnInteractEnd);

        PauseMenu.Instance.onPause += OnGamePause;

        UpdateInteractionText();
    }

    private void OnDestroy()
    {
        PauseMenu.Instance.onPause -= OnGamePause;
    }

    private void OnInteractStart()
    {
        heldTime = 0;
    }

    private void OnInteractEnd()
    {
        if (heldTime < MaxHoldTimeBeforeShowOptions)
            selectedOption = Options.FirstOrDefault();

        heldTime = null;
        OnHideOptions();

        if (selectedOption == null)
            return;

        if (selectedOption.Id == null)
        {
            Logger.LogWarning($"Selected option '{selectedOption.Name}' has no id, skipping interaction");
            return;
        }

        OnInteract?.Invoke(selectedOption.Id);
        selectedOption = null;

        UpdateInteractionText();
    }

    private void OnGamePause()
    {
        if (showingOptions)
            OnHideOptions();
    }

    public virtual void OnShowOptions()
    {
        if (RadialMenu.Instance.Show(Options, onOptionSelected: OnOptionSelected, onUpdateInteractionText: OnUpdateInteractionText))
            showingOptions = true;
        else
        {
            Logger.LogWarning("Radial menu is already open");
        }
    }

    public virtual void OnHideOptions()
    {
        if (showingOptions)
        {
            showingOptions = false;
            RadialMenu.Instance.Hide();
        }
    }

    private void UpdateInteractionText()
    {
        if (!InteractableObject)
            return;

        var option = Options.FirstOrDefault();
        var eventRefData = new EventRefData<string>(option?.Name);

        OnUpdateInteractionText?.Invoke(option, eventRefData);
        InteractableObject.SetMessage(eventRefData.Value);
    }

    private void Update()
    {
        if (heldTime.HasValue)
        {
            heldTime += Time.deltaTime;

            if (heldTime >= MaxHoldTimeBeforeShowOptions && !showingOptions)
            {
                OnShowOptions();
            }
        }
    }

    private void OnOptionSelected(InteractableOption option)
    {
        if (InteractionManager.Instance.InteractedObject != InteractableObject)
            return;

        selectedOption = option;
        InteractionManager.Instance.InteractedObject.EndInteract();
        InteractionManager.Instance.InteractedObject = null;
    }
}

[Serializable]
[CreateAssetMenu(fileName = "Option", menuName = "RealRadio/Interactable Options/Option")]
public class InteractableOption : ScriptableObject
{
    public string? Id;
    public string? Name;
    public string? Description;
    public string? Abbreviation;
    public Sprite? Sprite;
    public Color? BackgroundColor;
    public bool RoundedBackground;
    public Color? TextColor;

    /// <summary>
    /// Creates an option. Meant to be used from code at runtime.
    /// </summary>
    public static InteractableOption CreateOption(string id, string name, string? description = null, Sprite? sprite = null, string? abbreviation = null, Color? backgroundColor = null, bool roundedBackground = false, Color? textColor = null)
    {
        var result = CreateInstance<InteractableOption>();
        result.Id = id;
        result.Name = name;
        result.Description = description;
        result.Abbreviation = abbreviation;
        result.Sprite = sprite;
        result.BackgroundColor = backgroundColor;
        result.RoundedBackground = roundedBackground;
        result.TextColor = textColor;
        return result;
    }
}
