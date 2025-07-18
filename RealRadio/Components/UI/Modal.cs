using System;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using ScheduleOne;
using ScheduleOne.DevUtilities;
using UnityEngine;
using UnityEngine.UIElements;

namespace RealRadio.Components.UI;

public class Modal : Singleton<Modal>
{
    [field: SerializeField]
    public VisualTreeAsset ModalTemplate { get; private set; } = null!;

    [field: SerializeField]
    public VisualTreeAsset MessageContentTemplate { get; private set; } = null!;

    public override void Awake()
    {
        base.Awake();

        if (ModalTemplate == null)
            throw new InvalidOperationException("ModalTemplate is null");
    }

    public ModalInstance ShowModal(
        string title,
        string message,
        VisualElement context,
        string? confirmText = "OK",
        string? cancelText = null,
        ModalInstance.ConfirmedDelegate? onConfirm = null,
        Action<ModalInstance>? onCancel = null,
        Action<ModalInstance>? onClosed = null
    )
    {
        return ShowModal(MessageContentTemplate, SetupContent, context, title, confirmText, cancelText, onConfirm, onCancel, onClosed);

        void SetupContent(ModalInstance instance)
        {
            var messageLabel = instance.Content.Query<Label>(name: "Message").First() ?? throw new InvalidOperationException("Could not find message label ui element");
            messageLabel.text = message;
        }
    }

    public ModalInstance ShowModal(
        VisualTreeAsset contentTemplate,
        Action<ModalInstance> setupContent,
        VisualElement context,
        string? title = null,
        string? confirmText = null,
        string? cancelText = null,
        ModalInstance.ConfirmedDelegate? onConfirm = null,
        Action<ModalInstance>? onCancel = null,
        Action<ModalInstance>? onClosed = null
    )
    {
        var modalElement = ModalTemplate.Instantiate();
        GetRootOfContext(context).Add(modalElement);

        var modal = new ModalInstance(modalElement, contentTemplate.Instantiate())
        {
            Title = title,
            ConfirmText = confirmText,
            CancelText = cancelText,
        };

        setupContent(modal);

        modal.Confirmed += onConfirm;
        modal.Canceled += onCancel;
        modal.Closed += onClosed;

        StartCoroutine(AddVisibleClassNextFrame(modalElement.Query(name: "ModalRoot").First() ?? throw new InvalidOperationException("Could not find modal root ui element")));

        return modal;
    }

    internal object ShowModal(object validatePlaylistModalAsset, Action<VisualElement> setupContent, VisualElement root, string title, string cancelText, Action onCancel, Action onClosed)
    {
        throw new NotImplementedException();
    }

    private IEnumerator AddVisibleClassNextFrame(VisualElement element)
    {
        yield return null;
        element.AddToClassList("visible");
    }

    [return: NotNullIfNotNull(nameof(context))]
    private VisualElement? GetRootOfContext(VisualElement? context)
    {
        if (context == null)
            return null;

        return context.GetRoot();
    }
}

public class ModalInstance
{
    public string? Title
    {
        get => title;
        set
        {
            if (value == title)
                return;

            title = value;
            titleLabel.text = value ?? string.Empty;

            if (value == null)
                titleContainer.style.display = DisplayStyle.None;
            else
                titleContainer.style.display = DisplayStyle.Flex;
        }
    }

    public VisualElement Content
    {
        get => content;
        set
        {
            if (value == content)
                return;

            if (content != null)
                contentRoot.Remove(content);

            content = value;

            if (content != null)
                contentRoot.Add(content);
        }
    }

    public string? ConfirmText
    {
        get => confirmText;
        set
        {
            if (value == confirmText)
                return;

            confirmText = value;
            confirmButton.text = value ?? string.Empty;

            if (value == null)
                confirmButton.style.display = DisplayStyle.None;
            else
                confirmButton.style.display = DisplayStyle.Flex;
        }
    }

    public string? CancelText
    {
        get => cancelText;
        set
        {
            if (value == cancelText)
                return;

            cancelText = value;
            cancelButton.text = value ?? string.Empty;

            if (value == null)
                cancelButton.style.display = DisplayStyle.None;
            else
                cancelButton.style.display = DisplayStyle.Flex;
        }
    }

    public bool ConfirmButtonEnabled
    {
        get => confirmButton.enabledSelf;
        set
        {
            if (value == confirmButton.enabledSelf)
                return;

            confirmButton.SetEnabled(value);
        }
    }

    public delegate void ConfirmedDelegate(ModalInstance instance, ref bool preventClose);

    public event ConfirmedDelegate? Confirmed;
    public event Action<ModalInstance>? Canceled;
    public event Action<ModalInstance>? Closed;

    private string? title;
    private VisualElement content = null!;
    private string? confirmText;
    private string? cancelText;

    private VisualElement root;
    private VisualElement modalRoot;
    private VisualElement titleContainer;
    private Label titleLabel;
    private VisualElement contentRoot;
    private Button confirmButton;
    private Button cancelButton;

    public ModalInstance(VisualElement root, VisualElement content)
    {
        this.root = root;

        modalRoot = root.Query(name: "ModalRoot").First() ?? throw new InvalidOperationException("Could not find modal root ui element");
        titleContainer = root.Query(name: "TitleContainer").First() ?? throw new InvalidOperationException("Could not find title container ui element");
        titleLabel = titleContainer.Query<Label>(name: "Title").First() ?? throw new InvalidOperationException("Could not find title label ui element");
        contentRoot = root.Query(name: "Content").First() ?? throw new InvalidOperationException("Could not find content container ui element");
        confirmButton = root.Query<Button>(name: "ConfirmButton").First() ?? throw new InvalidOperationException("Could not find confirm button ui element");
        cancelButton = root.Query<Button>(name: "CancelButton").First() ?? throw new InvalidOperationException("Could not find cancel button ui element");

        cancelText = cancelButton.text;
        confirmText = confirmButton.text;
        modalRoot.RemoveFromClassList("visible"); // this is added by default in editor to make it visible, but at runtime the initial state should be hidden

        confirmButton.RegisterCallback<ClickEvent>(OnConfirmButtonClicked);
        cancelButton.RegisterCallback<ClickEvent>(OnCancelButtonClicked);
        root.RegisterCallback<PointerUpEvent>(OnPointerUp);

        confirmButton.Focus();

        Content = content;
    }

    private void OnConfirmButtonClicked(ClickEvent evt)
    {
        Close(confirmed: true);
    }

    private void OnCancelButtonClicked(ClickEvent evt)
    {
        Close(confirmed: false);
    }

    private void OnPointerUp(PointerUpEvent evt)
    {
        if (evt.target is not VisualElement element || element.m_Name != "ModalRoot")
            return;

        if (evt.button == 0)
            Close(confirmed: false);
    }

    public void Close(bool confirmed)
    {
        if (confirmed)
        {
            bool preventClose = false;
            Confirmed?.Invoke(this, ref preventClose);

            if (preventClose)
                return;
        }
        else
            Canceled?.Invoke(this);

        root.RemoveFromHierarchy();
        GameInput.IsTyping = false;
        Closed?.Invoke(this);
    }
}
