using UnityEngine;
using System;

public abstract class Objective : MonoBehaviour
{

    public enum ObjectiveType
    {
        Portal,
        Obelisk
    }

    [SerializeField] private ObjectiveType objectiveType;
    // [SerializeField] private GameObject objectToActivate;

    public static event Action<Objective> AnyObjectiveClosed;

    public bool isClosed;
    public bool isEnabled = true;

    [HideInInspector] public int id;

    public ObjectiveType Type => objectiveType;

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryClose(other.gameObject);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryClose(collision.gameObject);
    }

    private void TryClose(GameObject collisionObject)
    {
        if (!isEnabled || isClosed || !IsPlayer(collisionObject))
            return;

        Close();
    }

    private void Close()
    {
        isClosed = true;

        AnyObjectiveClosed?.Invoke(this);
        
        HandleObjectiveClosed();
    }

    // public void Activate()
    // {
    //     objectToActivate.SetActive(true);
    // }

    protected abstract void HandleObjectiveClosed();

    private bool IsPlayer(GameObject obj)
    {
        return obj != null &&
               (obj.GetComponent<PlayerMovement>() != null || obj.CompareTag("Player"));
    }
}