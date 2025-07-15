using System;
using System.Diagnostics.CodeAnalysis;
using RealRadio.Assets;
using RealRadio.Components.Building.Buildables;
using ScheduleOne;
using ScheduleOne.DevUtilities;
using ScheduleOne.EntityFramework;
using ScheduleOne.ItemFramework;
using ScheduleOne.PlayerScripts;
using ScheduleOne.UI;
using ScheduleOne.UI.Compass;
using UnityEngine;
using UnityEngine.UIElements;

namespace RealRadio.Components.Building;

public class SpeakerConnectionManager : Singleton<SpeakerConnectionManager>
{
    public event Action<Speaker, Buildables.Radio>? SpeakerConnected;
    public event Action<BuildableItem?>? SelectedItemChanged;
    public event Action<BuildableItem?>? HoveredItemChanged;

    public float MaxConnectionDistance;
    public Color MaxDistanceColor;
    public Color MinDistanceColor;

    [SerializeField] private UIDocument activeUi = null!;
    private VisualElement distanceContainer = null!;
    private Label distanceLabel = null!;

    public bool EditModeEnabled { get; private set; }

    private Action? finishedCallback;
    private RaycastHit[] hits = new RaycastHit[4];
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

            var oldValue = selectedBuildableItem;
            selectedBuildableItem = value;
            OnSelectedBuildableItemChanged(oldValue);
        }
    }

    private Speaker? SelectedSpeaker => SelectedBuildableItem as Speaker;
    private Buildables.Radio? SelectedRadio => SelectedBuildableItem as Buildables.Radio ?? SelectedSpeaker?.Master;

    private Buildables.Radio? HoveredRadio => HoveredBuildableItem as Buildables.Radio ?? HoveredSpeaker?.Master;
    private Speaker? HoveredSpeaker => HoveredBuildableItem as Speaker;

    private GameObject? hoveredObject;
    private BuildableItem? hoveredBuildableItem;
    private BuildableItem? selectedBuildableItem;
    private GameObject selectionArrow = null!;
    private Animator? arrowAnimator;
    private int spawnAnimationHash;

    public override void Awake()
    {
        base.Awake();

        selectionArrow = Instantiate(AssetRegistry.Instance!.Prefabs.SelectionArrow);
        selectionArrow.SetActive(false);
        arrowAnimator = selectionArrow.GetComponentInChildren<Animator>();

        spawnAnimationHash = Animator.StringToHash("Base Layer.SpawnAnimation");

        if (activeUi == null)
            throw new InvalidOperationException("actuveUi is null");
    }

    private void AssignUiElements()
    {
        distanceContainer = activeUi.rootVisualElement.Query<VisualElement>(name: "DistanceContainer").First() ?? throw new InvalidOperationException("Could not find distance container ui element");
        distanceLabel = distanceContainer.Query<Label>(name: "Distance").First() ?? throw new InvalidOperationException("Could not find distance label ui element");
    }

    void OnEnable()
    {
        GameInput.RegisterExitListener(OnExitInput);
    }

    void OnDisable()
    {
        GameInput.DeregisterExitListener(OnExitInput);
        StopEditMode();
    }

    private void OnExitInput(ExitAction exitAction)
    {
        if (!EditModeEnabled || exitAction.Used)
            return;

        exitAction.Used = exitAction.exitType is ExitType.Escape or ExitType.RightClick;

        if (exitAction.exitType == ExitType.Escape)
            StopEditMode();
        else if (exitAction.exitType == ExitType.RightClick)
            SelectedBuildableItem = null;
    }

    public void StartEditMode(BuildableItem? initialSelectedItem = null, Action<Speaker, Buildables.Radio>? connectedCallback = null)
    {
        if (initialSelectedItem != null && initialSelectedItem is not Speaker or Buildables.Radio)
            throw new ArgumentException($"{nameof(initialSelectedItem)} ({initialSelectedItem}) must be a {nameof(Speaker)} or {nameof(Buildables.Radio)}");

        if (EditModeEnabled)
            return;

        EditModeEnabled = true;
        SelectedBuildableItem = initialSelectedItem;

        SpeakerConnected += connectedCallback;
        finishedCallback += OnFinished;
        void OnFinished()
        {
            SpeakerConnected -= connectedCallback;
            finishedCallback -= OnFinished;
        }

        PlayerCamera.Instance.AddActiveUIElement(name);
        CompassManager.Instance.SetVisible(false);
        PlayerInventory.Instance.SetInventoryEnabled(false);
        activeUi.gameObject.SetActive(true);
        AssignUiElements();
    }

    public void StopEditMode()
    {
        if (!EditModeEnabled)
            return;

        EditModeEnabled = false;
        SelectedBuildableItem = null;
        HoveredBuildableItem = null;
        HoveredObject = null;
        PlayerCamera.Instance.RemoveActiveUIElement(name);
        HUD.Instance.HideTopScreenText();
        CompassManager.Instance.SetVisible(true);
        PlayerInventory.Instance.SetInventoryEnabled(true);
        activeUi.gameObject.SetActive(false);

        finishedCallback?.Invoke();
    }

    void Update()
    {
        if (!EditModeEnabled)
            return;

        UpdateHoveredObject();
        UpdateUI();
    }

    private void UpdateHoveredObject()
    {
        int numHits = Physics.RaycastNonAlloc(PlayerCamera.Instance.transform.position, PlayerCamera.Instance.transform.forward, hits, maxDistance: 4f, Layers.Default.ToLayerMask());

        if (numHits == 0)
        {
            HoveredObject = null;
            return;
        }

        bool found = false;

        for (int i = 0; i < numHits; ++i)
        {
            if (hits[i].collider.GetComponentInParent<BuildableItem>() != null)
            {
                HoveredObject = hits[i].collider.gameObject;
                found = true;
                break;
            }
        }

        if (!found)
        {
            HoveredObject = null;
            return;
        }

        if (!GameInput.GetButtonDown(GameInput.ButtonCode.PrimaryClick))
            return;

        if (SelectedBuildableItem == null)
        {
            SelectedBuildableItem = (BuildableItem?)HoveredSpeaker ?? HoveredRadio;
        }
        else if (IsValidConnectionTarget(HoveredBuildableItem))
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

    private void UpdateUI()
    {
        bool distanceVisible = false;
        float remainingDistance = 0;
        Color distanceColor = Color.white;

        try
        {
            if (SelectedBuildableItem == null)
                return;

            Vector3 targetPosition = HoveredObject?.transform.position ?? PlayerCamera.Instance.transform.position;
            remainingDistance = Mathf.Max(0, MaxConnectionDistance - Vector3.Distance(SelectedBuildableItem.transform.position, targetPosition));

            if (remainingDistance < 10f)
            {
                distanceVisible = true;
                distanceColor = Color.Lerp(MinDistanceColor, MaxDistanceColor, remainingDistance / 10f);
            }
        }
        finally
        {
            distanceContainer.visible = distanceVisible;

            if (distanceVisible)
            {
                distanceLabel.text = remainingDistance.ToString("0.0");
                distanceLabel.style.color = distanceColor;
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
        ShowArrowOnObjectIfValid(HoveredBuildableItem);
        HoveredItemChanged?.Invoke(HoveredBuildableItem);
    }

    private void ShowArrowOnObjectIfValid(BuildableItem? item)
    {
        if (IsValidConnectionTarget(item))
        {
            selectionArrow.SetActive(true);
            Vector3 position = GetTopPosition(item);
            selectionArrow.transform.position = position;

            arrowAnimator?.Play(spawnAnimationHash);
        }
        else
        {
            selectionArrow.SetActive(false);
        }
    }

    private Vector3 GetTopPosition(BuildableItem item)
    {
        var arrowTransform = item.GetComponent<SpeakerConnectionArrowTransform>()?.GetTransform();

        if (arrowTransform != null)
        {
            return arrowTransform.transform.position;
        }

        Vector3 position = item.BoundingCollider.bounds.center;
        BuildableItem prefab = ((BuildableItemDefinition)item.ItemInstance.Definition).BuiltItem;
        Vector3 size = Vector3.Scale(prefab.BoundingCollider.size, prefab.BoundingCollider.transform.lossyScale);
        position.y += size.y / 2f * item.transform.lossyScale.y;
        return position;
    }

    private void OnSelectedBuildableItemChanged(BuildableItem? prev)
    {
        if (EditModeEnabled)
            HUD.Instance.ShowTopScreenText(GetHudText());

        ShowArrowOnObjectIfValid(HoveredBuildableItem);

        if (prev != null && prev is OffGridItem prevOffGridItem)
            prevOffGridItem.BeforeDestroy -= OnSelectedItemDestroying;

        if (SelectedBuildableItem != null && SelectedBuildableItem is OffGridItem nextOffGridItem)
            nextOffGridItem.BeforeDestroy += OnSelectedItemDestroying;

        SelectedItemChanged?.Invoke(SelectedBuildableItem);
    }

    private void OnSelectedItemDestroying()
    {
        SelectedBuildableItem = null;
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
            {
                string connectedText = "connected";

                if (SelectedSpeaker.Master != null)
                    connectedText += " or disconnected";

                return $"Select a radio or another {connectedText} speaker to connect to {GetItemName(SelectedSpeaker)}";
            }

            if (SelectedRadio != null)
                return $"Select a speaker to connect to {GetItemName(SelectedRadio)}";
        }

        return "Unreachable";
    }

    private string GetItemName(BuildableItem item)
    {
        return item.ItemInstance.Definition.Name;
    }

    private bool IsValidConnectionTarget([NotNullWhen(true)] BuildableItem? item)
    {
        if (item == null)
            return false;

        if (SelectedBuildableItem != null && item == SelectedBuildableItem)
            return false;

        if (SelectedBuildableItem == null)
            return item is Buildables.Radio or Speaker;

        if (MaxConnectionDistance > 0 && Vector3.Distance(SelectedBuildableItem.transform.position, item.transform.position) > MaxConnectionDistance)
            return false;

        bool selectedIsSpeaker = SelectedSpeaker != null;

        if (selectedIsSpeaker)
        {
            if (item is Buildables.Radio)
                return true;

            if (item is Speaker speaker)
            {
                // Allow connecting two speakers as long as atleast one is connected
                return speaker.Master != null || SelectedSpeaker?.Master != null;
            }
        }

        return item is Speaker;
    }
}

public class SpeakerConnectionArrowTransform : MonoBehaviour
{
    [SerializeField]
    private Transform? overrideTransform;

    public Transform GetTransform()
    {
        return overrideTransform ?? transform;
    }
}
