using System;
using System.Collections.Generic;
using ScheduleOne;
using ScheduleOne.DevUtilities;
using ScheduleOne.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.UIElements;

namespace RealRadio.Components.UI.Phone;

public abstract class UITKApp<T> : App<UITKApp<T>> where T : PlayerSingleton<UITKApp<T>>
{
    protected RectTransform rectTransform => (RectTransform)transform;

    [SerializeField]
    private RawImage renderTextureTarget = null!;

    private Camera overlayCamera = null!;
    private UIDocument uiDocument = null!;
    private Vector2 scale;

    private static readonly KeyCode[] keysToPrevent = [
        KeyCode.Escape,
        KeyCode.Tab,
    ];

    public override void Awake()
    {
        base.Awake();

        if (renderTextureTarget == null)
            throw new InvalidOperationException("RenderTextureTarget is null");

        overlayCamera = Camera.main.transform.Find("OverlayCamera")?.GetComponent<Camera>() ?? throw new InvalidOperationException("No OverlayCamera found");

        uiDocument = GetComponentInChildren<UIDocument>() ?? throw new InvalidOperationException("No UIDocument component found on game object or children");
        uiDocument.panelSettings.SetScreenToPanelSpaceFunction(ScreenToPanelSpace);

        // Calculate scale of renderTextureTarget relative to the ui size
        Vector2 baseSize = rectTransform.rect.size;
        Vector2 textureSize = new Vector2(renderTextureTarget.texture.width, renderTextureTarget.texture.height);
        scale = textureSize / baseSize;

        if (Orientation == EOrientation.Vertical)
            rectTransform.localRotation = Quaternion.Euler(0, 0, 90);

        ScheduleOne.UI.Phone.Phone.Instance.onPhoneClosed += OnPhoneClosed;
    }

    private void OnPhoneClosed()
    {
        uiDocument.rootVisualElement.SetEnabled(false);
        uiDocument.rootVisualElement.Blur();
    }

    public override void OnPhoneOpened()
    {
        base.OnPhoneOpened();
        uiDocument.rootVisualElement.SetEnabled(true);
    }

    void OnEnable()
    {
        var root = uiDocument.rootVisualElement.GetRoot();
        root.RegisterCallback<FocusEvent>(OnFocus, TrickleDown.TrickleDown);
        root.RegisterCallback<BlurEvent>(OnBlur, TrickleDown.TrickleDown);
        root.RegisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);
    }

    private Vector2 ScreenToPanelSpace(Vector2 vector)
    {
        if (!renderTextureTarget.gameObject.activeInHierarchy)
            return new Vector2(float.NaN, float.NaN);

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(renderTextureTarget.rectTransform, Input.mousePosition, overlayCamera, out vector))
        {
            return new Vector2(float.NaN, float.NaN);
        }

        // UITK Y is inverted compared to UGUI
        vector.y = renderTextureTarget.rectTransform.rect.height - vector.y;

        vector *= scale;

        return vector;
    }

    private void OnFocus(FocusEvent evt)
    {
        if (evt.target is TextElement)
        {
            GameInput.IsTyping = true;
        }
    }

    private void OnBlur(BlurEvent evt)
    {
        if (evt.target is TextElement)
        {
            GameInput.IsTyping = false;
        }
    }

    private void OnKeyDown(KeyDownEvent evt)
    {
        if (GameInput.IsTyping)
            return;

        foreach (var key in keysToPrevent)
        {
            if (evt.keyCode == key)
            {
                Plugin.Logger.LogInfo($"Preventing key: {key}");

                evt.PreventDefault();
                evt.StopPropagation();
                return;
            }
        }
    }
}
