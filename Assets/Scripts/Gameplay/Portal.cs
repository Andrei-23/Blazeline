using UnityEngine;
using System;

public class Portal : Objective
{   
    protected override void HandleObjectiveClosed()
    {
        Destroy(gameObject);
    }
}