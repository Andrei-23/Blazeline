using System;
using System.Collections.Generic;
using UnityEngine;

public class ObjectiveManager  : MonoBehaviour
{
    private const string PortalIdPrefix = "portal_";
    private const string PortalDisplayNamePrefix = "Portal ";
    private const string ObeliskIdPrefix = "obelisk_";
    private const string ObeliskDisplayNamePrefix = "Obelisk ";

    // [Serializable]
    // public struct MinuteSpawnConfig
    // {
    //     public int minute;
    //     public int count;
    // }

    [Header("Objective Settings")]
    [SerializeField] private Transform portalParent;
    [SerializeField] private Transform obeliskParent;


    // Order: 0m(initial), 1m, 2m ...
    [Header("Timer Spawns")]
    [SerializeField] private List<int> portalSpawnAmount;
    
    [SerializeField] private List<float> obeliskSpawnRatio;

    [Header ("References")]
    [SerializeField] private MapDataManager mapDataManager;

    // private readonly List<Objective> activeObjectives = new List<Objective>();
    // private readonly List<Vector2> objectivePositions = new List<Vector2>();

    private List<Portal> generatedPortals = new();
    private List<Obelisk> generatedObelisks = new();

    private int lastActivatedPortalId = -1;
    private int lastActivatedObeliskId = -1;

    private void OnEnable()
    {
        Objective.AnyObjectiveClosed += OnObjectiveClosed;
        GameTimeManager.OnTimerMinutesChanged += OnTimerMinutesChanged;
    }
    private void OnDisable()
    {
        Objective.AnyObjectiveClosed -= OnObjectiveClosed;
        GameTimeManager.OnTimerMinutesChanged -= OnTimerMinutesChanged;
    }

    private void Start()
    {
        SpawnInitialization();
    }

    private void Update()
    {
        float maxDanger = 0f;
        foreach(Portal portal in generatedPortals)
        {
            if(portal != null && portal.isEnabled && !portal.isClosed)
            {
                maxDanger = Math.Max(maxDanger, portal.GetDangerLevel());
            }
        }
        PlayerBuffManager.Instance.SetCurrentDangerLevel(maxDanger);
    }
    public void SpawnInitialization()
    {
        for (int i = 0; i < portalParent.childCount; i++)
        {
            GameObject child = portalParent.GetChild(i).gameObject;
            child.SetActive(false);
            Portal portal = child?.GetComponent<Portal>();
            if (child != null)
            {
                generatedPortals.Add(portal);
                portal.id = i;
            }
        }
        for (int i = 0; i < obeliskParent.childCount; i++)
        {
            GameObject child = obeliskParent.GetChild(i).gameObject;
            // child.SetActive(false);
            Obelisk obelisk = child?.GetComponent<Obelisk>();
            if (child != null)
            {
                generatedObelisks.Add(obelisk);
                obelisk.id = i;
            }
        }
        
        for (int i = 0; i < portalSpawnAmount[0]; i++)
        {
            ActivateNextPortal();
        }
        
        for (int i = 0; i < obeliskSpawnRatio[0] * generatedObelisks.Count; i++)
        {
            ActivateNextObelisk();
        }
    }

    private void ActivateNextPortal()
    {
        lastActivatedPortalId += 1;
        ActivateObjective(true, lastActivatedPortalId);
    }
    private void ActivateNextObelisk()
    {
        lastActivatedObeliskId += 1;
        ActivateObjective(false, lastActivatedObeliskId);
    }
    
    private void ActivateObjective(bool isPortal, int id)
    {
        Vector3 pos;
        if(isPortal){
            generatedPortals[id].gameObject.SetActive(true);
            pos = generatedPortals[id].transform.position;
            generatedPortals[id].UpdateSpawnTime();
        }
        else{
            generatedObelisks[id].Activate();
            pos = generatedObelisks[id].transform.position;
        }
        AddIcon(isPortal, id, pos);
    }

    private void OnObjectiveClosed(Objective objective)
    {
        switch (objective.Type)
        {
            case Objective.ObjectiveType.Portal:
                Debug.Log("Portal closed");
                RemoveIcon(true, objective.id);
                break;

            case Objective.ObjectiveType.Obelisk:
                Debug.Log("Obelisk closed");
                DeactivateObeliskIcon(objective.id);
                break;
        }
    }
    private void AddIcon(bool isPortal, int id, Vector3 worldPosition)
    {
        if (mapDataManager == null)
        {
            return;
        }

        string iconId = GetIconId(id, isPortal);
        string iconName = GetIconName(id, isPortal);

        MapDataManager.MapObjectData data;
        if (isPortal)
        {
            data = new MapDataManager.PortalMapObjectData(
                iconId,
                iconName,
                worldPosition,
                System.DateTime.UtcNow
            );
        }
        else
        {
            data = new MapDataManager.ObeliskMapObjectData(
                iconId,
                iconName,
                worldPosition,
                true
            );
        }
        
        if (!mapDataManager.AddIcon(data))
        {
            mapDataManager.UpdateIconPosition(iconId, worldPosition);
        }
    }

    private void DeactivateObeliskIcon(int id)
    {
        string iconId = GetIconId(id, false);
        if (mapDataManager == null)
        {
            return;
        }

        mapDataManager.UpdateObeliskIconActive(iconId, false);
    }

    
    private void RemoveIcon(bool isPortal, int id)
    {
        string iconId = GetIconId(id, isPortal);
        if (mapDataManager == null)
        {
            return;
        }

        mapDataManager.RemoveIcon(iconId);
    }

    private static string GetIconId(int index, bool isPortal)
    {
        if (isPortal)
            return $"{PortalIdPrefix}{index}";
        else
            return $"{ObeliskIdPrefix}{index}";
    }
    
    private static string GetIconName(int index, bool isPortal)
    {
        if (isPortal)
            return $"{PortalDisplayNamePrefix}{index}";
        else
            return $"{ObeliskDisplayNamePrefix}{index}";
    }

    public List<Portal> GetActivePortals()
    {
        List<Portal> result = new();
        foreach(Portal portal in generatedPortals)
        {
            if(portal != null && !portal.isClosed && portal.gameObject.activeSelf)
            {
                result.Add(portal);
            }
        }
        return result;
    }
    
    public List<Obelisk> GetActiveObelisks()
    {
        List<Obelisk> result = new();
        foreach(Obelisk obelisk in generatedObelisks)
        {
            if(obelisk != null && !obelisk.isClosed && obelisk.isEnabled)
            {
                result.Add(obelisk);
            }
        }
        return result;
    }

    private void OnTimerMinutesChanged(int cur, int _)
    {
        if(cur == 0) return; // it is done independently

        if(cur < portalSpawnAmount.Count)
        {
            for(int i = 0; i < portalSpawnAmount[cur]; i++)
            {
                ActivateNextPortal();
            }
        }
        if(cur < obeliskSpawnRatio.Count)
        {
            while((lastActivatedObeliskId + 1) < generatedObelisks.Count * obeliskSpawnRatio[cur])
            {
                ActivateNextObelisk();
            }
        }
    }
}
