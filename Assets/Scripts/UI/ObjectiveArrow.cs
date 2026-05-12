using UnityEngine;
using UnityEngine.UI;

public class ObjectiveArrow : MonoBehaviour
{   
    [SerializeField] private bool isPortalArrow;
    [SerializeField] private Image image;
    [SerializeField] private Transform rotationPoint;

    public void SetAlpha(float alpha)
    {
        Color c = Color.white;
        c.a = alpha;
        image.color = c;
    }
    public void SetRotation(float angle)
    {
        rotationPoint.rotation = Quaternion.Euler(0, 0, angle - 90f);
    }
}
