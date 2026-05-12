using UnityEngine;
using System;

public class Obelisk : Objective
{
    [SerializeField] private GameObject disabledObject;
    [SerializeField] private GameObject defaultObject;
    [SerializeField] private GameObject collectedObject;

    private void Awake()
    {
        isEnabled = false;
        disabledObject.SetActive(true);
        defaultObject.SetActive(false);
        collectedObject.SetActive(false);
    }
    public void Activate()
    {
        isEnabled = true;
        disabledObject.SetActive(false);
        defaultObject.SetActive(true);
    }
    protected override void HandleObjectiveClosed()
    {
        defaultObject.SetActive(false);
        collectedObject.SetActive(true);
    }
}