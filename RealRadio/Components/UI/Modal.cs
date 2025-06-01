using System;
using ScheduleOne.DevUtilities;
using UnityEngine;
using UnityEngine.UIElements;

namespace RealRadio.Components.UI;

public class Modal : Singleton<Modal>
{
    [field: SerializeField]
    public VisualTreeAsset ModalTemplate { get; private set; } = null!;

    public override void Awake()
    {
        base.Awake();

        if (ModalTemplate == null)
            throw new InvalidOperationException("ModalTemplate is null");
    }
}
