using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class BuildingSelector : MonoBehaviour
{
    public static BuildingSelector Instance { get; private set; }

    private InputAction clickAction;
    private InputAction pointerAction;
    private Building selectedBuilding;

    void Awake()
    {
        Instance = this;

        clickAction = new InputAction("Select", InputActionType.Button);
        clickAction.AddBinding("<Mouse>/leftButton");
        clickAction.AddBinding("<Touchscreen>/primaryTouch/press");

        pointerAction = new InputAction("PointerPos", InputActionType.Value, binding: "<Mouse>/position");
        pointerAction.AddBinding("<Touchscreen>/primaryTouch/position");

        clickAction.Enable();
        pointerAction.Enable();
    }

    void OnDestroy()
    {
        clickAction.Disable();
        pointerAction.Disable();
    }

    void Update()
    {
        if (!clickAction.WasPressedThisFrame()) return;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        Vector2 screenPos = pointerAction.ReadValue<Vector2>();
        Ray ray = Camera.main.ScreenPointToRay(screenPos);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            Building building = hit.collider.GetComponentInParent<Building>();
            if (building != null)
            {
                BuildingPopup.Instance.Show(building);
                return;
            }
        }

        BuildingPopup.Instance.Hide();
    }
}
