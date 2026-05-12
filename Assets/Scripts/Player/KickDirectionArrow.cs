using UnityEngine;

/// <summary>
/// Visual arrow that previews kick/jump direction.
/// Listens to PlayerMovement's kick vector updates and rotates/scales accordingly.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class KickDirectionArrow : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;

    [Header("Visual Settings")]
    [Tooltip("If true, shows kick initial (clamped aim) when non-zero. If false, shows movement direction only while kick initial is zero.")]
    [SerializeField] private bool isKickDirection = true;

    [Tooltip("Base local scale applied to the arrow before length scaling.")]
    [SerializeField] private Vector3 baseLocalScale = Vector3.one;

    [Tooltip("Minimum magnitude required to show the arrow.")]
    [SerializeField] private float showThreshold = 0.001f;

    [Header("")]
    [SerializeField] private float minLength = 5f;
    [SerializeField] private float maxLength = 15f;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        // Ensure we start from a known scale (for non-drawMode sprites).
        transform.localScale = baseLocalScale;
    }

    private void OnEnable()
    {
        PlayerMovement.KickVectorsUpdated += OnKickVectorsUpdated;
    }

    private void OnDisable()
    {
        PlayerMovement.KickVectorsUpdated -= OnKickVectorsUpdated;
    }

    private void OnKickVectorsUpdated(Vector2 kickInitial, Vector2 _, Vector2 movementDirection)
    {
        if (isKickDirection)
        {
            SetDirection(kickInitial);
        }
        else
        {
            SetDirection(movementDirection);
        }
    }

    /// <summary>
    /// Sets the arrow direction (world-space) and updates rotation/scale.
    /// Pass Vector2.zero to hide the arrow.
    /// </summary>
    public void SetDirection(Vector2 direction)
    {
        if (direction.sqrMagnitude < showThreshold * showThreshold)
        {
            if (spriteRenderer != null) spriteRenderer.enabled = false;
            return;
        }

        if (spriteRenderer != null) spriteRenderer.enabled = true;

        // Rotate arrow to point along direction.
        // Assuming arrow sprite points UP (+Y) by default, calculate angle from +Y axis.
        // Negate x to fix horizontal flip.
        float angleDeg = Mathf.Atan2(-direction.x, direction.y) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angleDeg);

        // Length based on magnitude (you can customize formula in GetLenght).
        float length = GetLenght(direction.magnitude);

        // Guard against NaN / Infinity coming from custom formulas.
        if (!float.IsFinite(length) || length <= 0f)
        {
            if (spriteRenderer != null) spriteRenderer.enabled = false;
            return;
        }

        // If the sprite uses Draw Mode (Sliced/Tiled), control its "height" via size.y.
        if (spriteRenderer != null &&
            (spriteRenderer.drawMode == SpriteDrawMode.Sliced || spriteRenderer.drawMode == SpriteDrawMode.Tiled))
        {
            var size = spriteRenderer.size;
            size.y = length;
            spriteRenderer.size = size;
        }
        else
        {
            // Fallback for simple sprites: scale along local Y while keeping baseLocalScale as the baseline.
            transform.localScale = new Vector3(baseLocalScale.x, baseLocalScale.y * length, baseLocalScale.z);
        }
    }

    /// <summary>
    /// Arrow length mapping from direction magnitude.
    /// </summary>
    public float GetLenght(float magnitude)
    {
        if (magnitude <= 0f){
            return minLength;
        }
        float a = 1f - 1f / (0.5f * magnitude + 1);
        return Mathf.Lerp(minLength, maxLength, a);
    }
}

