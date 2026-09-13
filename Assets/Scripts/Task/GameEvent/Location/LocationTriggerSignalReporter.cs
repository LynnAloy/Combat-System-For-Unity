using GameDefinitions;
using UnityEngine;

namespace GameplayEvents.Location
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public sealed class LocationTriggerSignalReporter : MonoBehaviour
    {
        [SerializeField] private LocationEnteredSignalSO locationEnteredSignal;
        [SerializeField] private LocationDefinitionSO location;
        [SerializeField] private string visitorTag = "Player";

        private void OnTriggerEnter(Collider other)
        {
            GameObject visitor = other.transform.root.gameObject;

            if (!visitor.CompareTag(visitorTag))
            {
                return;
            }

            if (locationEnteredSignal == null)
            {
                Debug.LogError("Location entered signal is not assigned.", this);
                return;
            }

            if (location == null)
            {
                Debug.LogError("Location definition is not assigned.", this);
                return;
            }

            LocationEnteredEvent payload = new LocationEnteredEvent(
                location,
                visitor,
                gameObject);

            GameEventHub.Publish(locationEnteredSignal, payload);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            Collider trigger = GetComponent<Collider>();

            if (trigger != null && !trigger.isTrigger)
            {
                Debug.LogWarning(
                    "LocationTriggerSignalReporter requires a Trigger collider.",
                    this);
            }
        }
#endif
    }
}