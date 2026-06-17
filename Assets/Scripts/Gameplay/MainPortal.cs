using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class MainPortal : MonoBehaviour
{
    [SerializeField] private ObjectiveManager objectiveManager;

    private void Awake()
    {
        // if (objectiveManager == null)
        //     objectiveManager = FindFirstObjectByType<ObjectiveManager>();

        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null)
            collider.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryWin(other.gameObject);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryWin(collision.gameObject);
    }

    private void TryWin(GameObject collisionObject)
    {
        if (!IsPlayer(collisionObject))
            return;

        if (GameplayManager.Instance != null && GameplayManager.Instance.IsGameOver)
            return;

        if (objectiveManager == null || !objectiveManager.CanWin())
            return;

        GameplayManager.Instance?.TriggerWin();
    }

    private static bool IsPlayer(GameObject obj)
    {
        return obj != null &&
               (obj.GetComponent<PlayerMovement>() != null || obj.CompareTag("Player"));
    }

    public void SetWinnableColor()
    {
        gameObject.GetComponent<SpriteRenderer>().color = Color.white;
    }
}
