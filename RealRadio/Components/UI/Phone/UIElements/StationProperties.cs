using System;
using RealRadio.Data;
using UnityEngine.UIElements;

namespace RealRadio.Components.UI.Phone.UIElements;

public class StationProperties
{
    public RadioStation? Station
    {
        get => station;
        set
        {
            if (station == value)
                return;

            station = value;
            stationChanged?.Invoke();
        }
    }

    public bool ReadOnly
    {
        get => readOnly;
        set
        {
            if (readOnly == value)
                return;

            readOnly = value;
            readOnlyChanged?.Invoke();
        }
    }

    private TextField nameField;
    private EnumField typeField;
    private TextField abbreviationField;
    private Toggle canBePlayedByNPCsToggle;

    private RadioStation? station;
    private bool readOnly;

    private Action? stationChanged;
    private Action? readOnlyChanged;

    public StationProperties(VisualElement root)
    {
        nameField = root.Query<TextField>(name: "Name").First() ?? throw new InvalidOperationException("Could not find name TextField ui element");
        stationChanged += () => nameField.text = Station?.Name ?? string.Empty;
        readOnlyChanged += () => nameField.SetEnabled(!ReadOnly);

        typeField = root.Query<EnumField>(name: "Type").First() ?? throw new InvalidOperationException("Could not find type EnumField ui element");
        stationChanged += () => typeField.value = Station?.Type ?? 0;
        readOnlyChanged += () => typeField.SetEnabled(!ReadOnly);

        abbreviationField = root.Query<TextField>(name: "Abbreviation").First() ?? throw new InvalidOperationException("Could not find abbreviation TextField ui element");
        stationChanged += () => abbreviationField.text = Station?.Abbreviation ?? string.Empty;
        readOnlyChanged += () => abbreviationField.SetEnabled(!ReadOnly);

        canBePlayedByNPCsToggle = root.Query<Toggle>(name: "CanBePlayedByNPCs").First() ?? throw new InvalidOperationException("Could not find can be played by NPCs Toggle ui element");
        stationChanged += () => canBePlayedByNPCsToggle.value = Station?.CanBePlayedByNPCs ?? false;
        readOnlyChanged += () => canBePlayedByNPCsToggle.SetEnabled(!ReadOnly);
    }
}
