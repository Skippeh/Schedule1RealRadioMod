using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace RealRadio.Components.UI.WorldUI;

public class UITKWorldUI : MonoBehaviour
{
    private UIDocument uiDocument = null!;

    void Awake()
    {
        uiDocument = GetComponentInChildren<UIDocument>() ?? throw new InvalidOperationException("No UIDocument component found on game object or children");
        uiDocument.panelSettings.SetScreenToPanelSpaceFunction(ScreenToPanelSpace);
    }

    private Vector2 ScreenToPanelSpace(Vector2 screenPosition)
    {
        return new Vector2(float.NaN, float.NaN);
    }
}
