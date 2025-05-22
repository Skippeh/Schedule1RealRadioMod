using System;
using UnityEngine;

namespace RealRadio.Components.UI.Phone;

public class RadioApp : UITKApp<RadioApp>
{
    [Header("References")]
    [SerializeField]
    private RadioAppUi ui = null!;

    public override void Awake()
    {
        base.Awake();

        if (ui == null)
            throw new InvalidOperationException("UI is null");
    }
}
