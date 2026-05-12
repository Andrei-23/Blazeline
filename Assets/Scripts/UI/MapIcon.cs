using UnityEngine;
using UnityEngine.UI;

public class MapIcon : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private RectTransform iconRectTransform;
    [SerializeField] private bool keepFixedScreenSize = true;
    [SerializeField] private float baseSize = 20f;

    private MapDataManager.MapObjectData mapObjectData;

    public string Id => mapObjectData?.Id;

    private void Awake()
    {
        if (iconRectTransform == null)
        {
            iconRectTransform = transform as RectTransform;
        }

        if (iconImage == null)
        {
            iconImage = GetComponent<Image>();
        }

        if (iconRectTransform != null)
        {
            iconRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            iconRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            iconRectTransform.pivot = new Vector2(0.5f, 0.5f);
        }
    }

    public void Initialize(MapDataManager.MapObjectData data, Sprite spriteOverride)
    {
        mapObjectData = data;
        gameObject.name = $"MapIcon_{data.DisplayName}";

        if (iconImage != null && spriteOverride != null)
        {
            iconImage.sprite = spriteOverride;
        }
    }

    public void SetAnchoredPosition(Vector2 anchoredPosition)
    {
        if (iconRectTransform == null)
        {
            return;
        }

        iconRectTransform.anchoredPosition = anchoredPosition;
    }

    public void SetZoom(float zoom)
    {
        if (!keepFixedScreenSize || iconRectTransform == null)
        {
            return;
        }

        float safeZoom = Mathf.Max(1f, zoom);
        // float scaledSize = baseSize / safeZoom;
        float scaledSize = safeZoom * baseSize;
        iconRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, scaledSize);
        iconRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, scaledSize);
    }
}
