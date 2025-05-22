using System;
using ScheduleOne.DevUtilities;
using ScheduleOne.UI;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

namespace RealRadio.Components.UI.Phone;

public abstract class UITKApp<T> : App<UITKApp<T>> where T : PlayerSingleton<UITKApp<T>>
{
    protected RectTransform rectTransform => (RectTransform)transform;

    [SerializeField]
    private RawImage renderTextureTarget = null!;

    private Camera overlayCamera = null!;

    public override void Awake()
    {
        base.Awake();

        if (renderTextureTarget == null)
            throw new InvalidOperationException("RenderTextureTarget is null");

        overlayCamera = Camera.main.transform.Find("OverlayCamera")?.GetComponent<Camera>() ?? throw new InvalidOperationException("No OverlayCamera found");

        var uiDocument = GetComponentInChildren<UIDocument>() ?? throw new InvalidOperationException("No UIDocument component found on game object or children");
        uiDocument.panelSettings.SetScreenToPanelSpaceFunction(ScreenToPanelSpace);

        if (Orientation == EOrientation.Vertical)
            rectTransform.localRotation = Quaternion.Euler(0, 0, 90);
    }

    private Vector2 ScreenToPanelSpace(Vector2 vector)
    {
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(renderTextureTarget.rectTransform, Input.mousePosition, overlayCamera, out vector))
        {
            return new Vector2(float.NaN, float.NaN);
        }

        // UITK Y is inverted compared to UGUI
        vector.y = renderTextureTarget.rectTransform.rect.height - vector.y;

        return vector;
    }
}
