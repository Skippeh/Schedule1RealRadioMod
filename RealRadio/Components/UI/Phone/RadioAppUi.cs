using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace RealRadio.Components.UI.Phone;

[RequireComponent(typeof(UIDocument))]
public class RadioAppUi : MonoBehaviour
{
    [Header("Style")]
    [SerializeField]
    private float backgroundScrollSpeed;

    private Vector2 backgroundMoveDirection;

    private UIDocument document = null!;
    private VisualElement root = null!;

    private void Awake()
    {
        document = GetComponent<UIDocument>() ?? throw new InvalidOperationException("No UIDocument component found on game object");
    }

    void OnEnable()
    {
        root = document.rootVisualElement.Query(name: "Root").First() ?? throw new InvalidOperationException("Could not find root ui element");

        RandomizeBackgroundParameters();
    }

    private void RandomizeBackgroundParameters()
    {
        root.style.backgroundPositionX = new StyleBackgroundPosition(new BackgroundPosition(BackgroundPositionKeyword.Left, new Length(UnityEngine.Random.Range(-1000f, 1000f))));
        root.style.backgroundPositionY = new StyleBackgroundPosition(new BackgroundPosition(BackgroundPositionKeyword.Top, new Length(UnityEngine.Random.Range(-1000f, 1000f))));

        float direction = UnityEngine.Random.Range(0f, 360f);
        backgroundMoveDirection = new Vector2(Mathf.Cos(direction * Mathf.Deg2Rad), Mathf.Sin(direction * Mathf.Deg2Rad)).normalized;
    }

    private void Update()
    {
        UpdateBackgroundPosition();
    }

    private void UpdateBackgroundPosition()
    {
        float x = root.resolvedStyle.backgroundPositionX.offset.m_Value;
        float y = root.resolvedStyle.backgroundPositionY.offset.m_Value;

        root.style.backgroundPositionX = new StyleBackgroundPosition(new BackgroundPosition(BackgroundPositionKeyword.Left, new Length(x + (backgroundScrollSpeed * Time.unscaledDeltaTime * backgroundMoveDirection.x))));
        root.style.backgroundPositionY = new StyleBackgroundPosition(new BackgroundPosition(BackgroundPositionKeyword.Top, new Length(y + (backgroundScrollSpeed * Time.unscaledDeltaTime * backgroundMoveDirection.y))));
    }
}
