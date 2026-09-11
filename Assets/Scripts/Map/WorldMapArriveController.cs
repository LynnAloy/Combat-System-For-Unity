using UnityEngine;

[DisallowMultipleComponent]
public sealed class WorldMapArriveController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;

    [SerializeField] private WorldMapNavigationController navigationController;

    [Header("Arrival")]
    [SerializeField, Min(0.1f)] private float arrivalDistance = 2.5f;

    [SerializeField]
    private bool ignoreHeight = true;

    private void Update()
    {
        if (player == null ||
            navigationController == null ||
            !navigationController.HasDestination)
        {
            return;
        }

        Vector3 playerPosition = player.position;
        Vector3 destination = navigationController.Destination;

        if (ignoreHeight)
        {
            playerPosition.y = 0f;
            destination.y = 0f;
        }

        float squareDistance = (playerPosition - destination).sqrMagnitude;

        if (squareDistance >
            arrivalDistance * arrivalDistance)
        {
            return;
        }

        navigationController.ClearDestination();
    }

    private void OnValidate()
    {
        arrivalDistance = Mathf.Max(0.1f, arrivalDistance);
    }
}