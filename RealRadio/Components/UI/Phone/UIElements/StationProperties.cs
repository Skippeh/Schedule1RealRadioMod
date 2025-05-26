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

    private TextField nameInput;
    private EnumField typeField;

    private RadioStation? station;
    private bool readOnly;

    private Action? stationChanged;
    private Action? readOnlyChanged;

    public StationProperties(VisualElement root)
    {
        nameInput = root.Query<TextField>(name: "Name").First() ?? throw new InvalidOperationException("Could not find name TextField ui element");
        stationChanged += () => nameInput.text = Station?.Name ?? string.Empty;
        readOnlyChanged += () => nameInput.SetEnabled(!ReadOnly);

        typeField = root.Query<EnumField>(name: "Type").First() ?? throw new InvalidOperationException("Could not find type EnumField ui element");
        stationChanged += () => typeField.value = Station?.Type ?? 0;
        readOnlyChanged += () => typeField.SetEnabled(!ReadOnly);
    }
}
