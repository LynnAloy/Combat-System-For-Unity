using System.Collections.Generic;
using UnityEngine;

namespace FlexibleTaskSystem
{
    [DisallowMultipleComponent]
    public sealed class TaskNavigationAnchor : MonoBehaviour
    {
        [SerializeField] private ScriptableObject definition;
        [SerializeField] private Transform navigationPoint;

        private static readonly Dictionary<
            ScriptableObject,
            List<TaskNavigationAnchor>> Anchors = new();

        private ScriptableObject registeredDefinition;

        public Transform NavigationPoint
        {
            get
            {
                return navigationPoint != null
                    ? navigationPoint
                    : transform;
            }
        }

        private void OnEnable()
        {
            Register();
        }

        private void OnDisable()
        {
            Unregister();
        }

        private void Register()
        {
            if (definition == null)
            {
                Debug.LogWarning(
                    "Task navigation definition is not assigned.",
                    this);

                return;
            }

            registeredDefinition = definition;

            if (!Anchors.TryGetValue(
                    registeredDefinition,
                    out List<TaskNavigationAnchor> anchors))
            {
                anchors = new List<TaskNavigationAnchor>();
                Anchors.Add(registeredDefinition, anchors);
            }

            if (!anchors.Contains(this))
            {
                anchors.Add(this);
            }
        }

        private void Unregister()
        {
            if (registeredDefinition == null ||
                !Anchors.TryGetValue(
                    registeredDefinition,
                    out List<TaskNavigationAnchor> anchors))
            {
                return;
            }

            anchors.Remove(this);

            if (anchors.Count == 0)
            {
                Anchors.Remove(registeredDefinition);
            }

            registeredDefinition = null;
        }

        public static bool TryResolve(
            ScriptableObject targetDefinition,
            Vector3 origin,
            out Transform navigationPoint)
        {
            navigationPoint = null;

            if (targetDefinition == null ||
                !Anchors.TryGetValue(
                    targetDefinition,
                    out List<TaskNavigationAnchor> anchors))
            {
                return false;
            }

            float closestDistance = float.PositiveInfinity;

            for (int i = anchors.Count - 1; i >= 0; i--)
            {
                TaskNavigationAnchor anchor = anchors[i];

                if (anchor == null)
                {
                    anchors.RemoveAt(i);
                    continue;
                }

                if (!anchor.isActiveAndEnabled)
                {
                    continue;
                }

                Transform candidate = anchor.NavigationPoint;
                float distance = (candidate.position - origin).sqrMagnitude;

                if (distance >= closestDistance)
                {
                    continue;
                }

                closestDistance = distance;
                navigationPoint = candidate;
            }

            return navigationPoint != null;
        }

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRegistry()
        {
            Anchors.Clear();
        }
    }
}