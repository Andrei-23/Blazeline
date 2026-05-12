using UnityEngine;

public class BonusSpeed : BaseBonus
{
    protected override void OnPickup()
    {
        PlayerBuffManager.Instance.speedBonusCount += 1;
        Destroy(gameObject);
    }
}
