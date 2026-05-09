using UnityEngine;

public class Drone : MonoBehaviour
{
    public enum State { MovingToTarget, Collecting, ReturningHome, Depositing }

    [Header("Config")]
    public DroneData data;
    public Transform homePoint;
    public Transform targetPoint;

    [Header("State")]
    public State currentState = State.MovingToTarget;
    public float carriedAmount = 0f;

    private float actionTimer = 0f;
    private const float actionDuration = 0.5f;

    void Update()
    {
        switch (currentState)
        {
            case State.MovingToTarget:
                MoveTowards(targetPoint.position);
                if (ReachedPoint(targetPoint.position))
                {
                    currentState = State.Collecting;
                    actionTimer = actionDuration;
                }
                break;

            case State.Collecting:
                actionTimer -= Time.deltaTime;
                if (actionTimer <= 0f)
                {
                    carriedAmount = data.carryCapacity;
                    currentState = State.ReturningHome;
                }
                break;

            case State.ReturningHome:
                MoveTowards(homePoint.position);
                if (ReachedPoint(homePoint.position))
                {
                    currentState = State.Depositing;
                    actionTimer = actionDuration;
                }
                break;

            case State.Depositing:
                actionTimer -= Time.deltaTime;
                if (actionTimer <= 0f)
                {
                    ResourceManager.Instance.Add(data.resourceType, carriedAmount);
                    carriedAmount = 0f;
                    currentState = State.MovingToTarget;
                }
                break;
        }
    }

    void MoveTowards(Vector3 destination)
    {
        Vector3 target = new Vector3(destination.x, transform.position.y, destination.z);
        transform.position = Vector3.MoveTowards(transform.position, target, data.moveSpeed * Time.deltaTime);
    }

    bool ReachedPoint(Vector3 point)
    {
        return Vector2.Distance(
            new Vector2(transform.position.x, transform.position.z),
            new Vector2(point.x, point.z)) < 0.1f;
    }
}
