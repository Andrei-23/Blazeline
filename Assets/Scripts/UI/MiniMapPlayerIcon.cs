using UnityEngine;
using UnityEngine.UI;

public class MiniMapPlayerIcon : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private float angleOffsetDegrees;

    private float lastAngleDegrees;
    private Vector2 lastDirection = Vector2.zero;

    private void Awake()
    {
        if (iconImage == null)
        {
            iconImage = GetComponent<Image>();
        }
    }

    private void Update()
    {
        if (iconImage == null)
        {
            return;
        }

        if (UIMenuChanger.Instance != null && UIMenuChanger.Instance.IsMenuOpen)
        {
            return;
        }

        Vector2 direction = GetDirection();
        if (direction.sqrMagnitude >= 0.0001f)
        {
            lastAngleDegrees = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + angleOffsetDegrees;
        }

        iconImage.rectTransform.localRotation = Quaternion.Euler(0f, 0f, lastAngleDegrees);
    }

    private Vector2 GetDirection()
    {
        if (playerMovement != null)
        {
            lastDirection = playerMovement.GetVelocity();
        }

        return lastDirection;
    }
}
