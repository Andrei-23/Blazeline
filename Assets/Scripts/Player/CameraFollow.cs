using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("References")]
    // [SerializeField] private Transform player;
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private PlayerMovement playerMovement;

    [Header("Offset")]
    [SerializeField] private float velocityMultiplier = 0.25f;
    [SerializeField] private float movementDirectionMultiplier = 2.5f;
    [SerializeField] private float maxVelocityOffsetSpeed = 5f;
    [SerializeField] private float baseCameraSize = 16f;

    [Header("Smoothing")]
    [SerializeField] private float followSpeed = 4f;

    private void Awake()
    {

    }

    private float GetCameraSize(){
        return playerCamera.orthographicSize;
    }
    private void LateUpdate()
    {
        if (playerMovement == null)
        {
            return;
        }

        Vector2 velocity = playerMovement.GetVelocity();
        Vector2 movementDirection = playerMovement.GetMovementDirection().normalized;

        float speed = velocity.magnitude;
        if(speed < 0.001f){
            velocity = Vector2.zero;
        }
        else{
            float maxSpeed = maxVelocityOffsetSpeed;
            if(speed > maxSpeed){
                speed = maxSpeed;
            }
            else{
                float x = speed / maxSpeed;
                speed *= 1f - (x - 1f) * (x - 1f);
            }
            velocity = velocity.normalized * speed;            
        }

        Vector2 weightedOffset = velocity * velocityMultiplier + movementDirection * movementDirectionMultiplier;
        weightedOffset *= baseCameraSize / GetCameraSize();

        Vector3 targetPosition = new Vector3(weightedOffset.x, weightedOffset.y, 0f);
        targetPosition.z = transform.position.z;

        float t = 1f - Mathf.Exp(-followSpeed * Time.deltaTime);
        cameraTransform.localPosition = Vector3.Lerp(transform.localPosition, targetPosition, t);
    }
}
