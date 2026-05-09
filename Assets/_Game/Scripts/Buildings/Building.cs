using UnityEngine;

public class Building : MonoBehaviour
{
    public BuildingData data;
    private GameObject spawnedDrone;

    public void Initialize(BuildingData buildingData)
    {
        data = buildingData;
        if (data.droneData != null)
            SpawnDrone();
        if (data.passiveRatePerMinute > 0f)
            ResourceManager.Instance.AddRate(data.passiveResourceType, data.passiveRatePerMinute);
    }

    void OnDestroy()
    {
        if (data != null && data.passiveRatePerMinute > 0f)
            ResourceManager.Instance.RemoveRate(data.passiveResourceType, data.passiveRatePerMinute);
        if (spawnedDrone != null)
            Destroy(spawnedDrone);
    }

    void SpawnDrone()
    {
        ResourceNode target = FindNearestNode(data.droneData.resourceType);
        if (target == null)
        {
            Debug.LogWarning($"No ResourceNode of type {data.droneData.resourceType} found for {data.buildingName}");
            return;
        }

        GameObject droneObj = data.droneData.prefab != null
            ? Instantiate(data.droneData.prefab)
            : GameObject.CreatePrimitive(PrimitiveType.Sphere);

        droneObj.transform.position = transform.position + Vector3.up * 0.5f;
        droneObj.transform.localScale = Vector3.one * 0.3f;
        droneObj.name = "Drone_" + data.buildingName;
        spawnedDrone = droneObj;

        Drone drone = droneObj.AddComponent<Drone>();
        drone.data = data.droneData;

        GameObject homeGO = new GameObject("HomePoint");
        homeGO.transform.position = transform.position + Vector3.up * 0.5f;
        homeGO.transform.SetParent(transform);

        GameObject targetGO = new GameObject("TargetPoint");
        targetGO.transform.position = target.transform.position;
        targetGO.transform.SetParent(target.transform);

        drone.homePoint = homeGO.transform;
        drone.targetPoint = targetGO.transform;
    }

    ResourceNode FindNearestNode(ResourceType type)
    {
        ResourceNode[] nodes = FindObjectsByType<ResourceNode>(FindObjectsSortMode.None);
        ResourceNode nearest = null;
        float minDist = float.MaxValue;
        foreach (var node in nodes)
        {
            if (node.resourceType != type) continue;
            float dist = Vector3.Distance(transform.position, node.transform.position);
            if (dist < minDist) { minDist = dist; nearest = node; }
        }
        return nearest;
    }
}
