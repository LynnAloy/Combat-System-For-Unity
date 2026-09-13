using GameDefinitions;
using UnityEngine;

namespace GameplayEvents.Location
{
    public readonly struct LocationEnteredEvent
    {
        public LocationDefinitionSO Location { get; }
        public GameObject Visitor { get; }
        public GameObject Source { get; }

        public LocationEnteredEvent(LocationDefinitionSO location, GameObject visitor, GameObject source)
        {
            Location = location;
            Visitor = visitor;
            Source = source;
        }
    }
}