using UnityEngine;

public class BaseBonus : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        // if (!IsPlayer(other))
        //     return;

        OnPickup();
    }

    protected virtual void OnPickup() { }

    // private static bool IsPlayer(Collider2D other)
    // {
    //     if (other == null)
    //         return false;

    //     GameObject go = other.gameObject;
    //     return go.GetComponent<PlayerMovement>() != null || go.CompareTag("Player");
    // }
}
