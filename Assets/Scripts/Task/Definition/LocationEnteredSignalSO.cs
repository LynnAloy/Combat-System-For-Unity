using UnityEngine;

namespace GameplayEvents.Location
{
    [CreateAssetMenu(fileName = "SIG_LocationEntered", menuName = "Gameplay Events/Location/Entered")]
    public sealed class LocationEnteredSignalSO :GameplaySignalSO<LocationEnteredEvent>
    {
    }
}