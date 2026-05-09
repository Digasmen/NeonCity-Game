using UnityEngine;

public class TestInput : MonoBehaviour
{
    public BuildingData testBuilding;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.B))
            BuildingPlacer.Instance.StartPlacement(testBuilding);
    }
}
