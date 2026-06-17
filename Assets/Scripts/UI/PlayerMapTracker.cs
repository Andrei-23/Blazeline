using UnityEngine;

public class PlayerMapTracker : MonoBehaviour
{
    [SerializeField] private MapDataManager mapDataManager;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private float angleOffsetDegrees;

    private float lastAngleDegrees;

    private void Awake()
    {
    }

    private void Update()
    {
        if (mapDataManager == null)
        {
            return;
        }

        Vector2 direction = GetDirection();
        if (direction.sqrMagnitude >= 0.0001f)
        {
            lastAngleDegrees = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + angleOffsetDegrees;
        }

        mapDataManager.SetPlayerState(transform.position, lastAngleDegrees);
    }

    private Vector2 GetDirection()
    {
        if (playerMovement != null)
        {
            return playerMovement.GetVelocity();
        }

        return Vector2.zero;
    }
}
