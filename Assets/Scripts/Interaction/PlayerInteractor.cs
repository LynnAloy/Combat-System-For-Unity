using UnityEngine;

public sealed class PlayerInteractor : MonoBehaviour
{
    [Header("Detection")]
    [SerializeField] private Transform detectionOrigin;
    [SerializeField] private Vector3 detectionOffset = new(0f, 1f, 0.75f);
    [SerializeField, Min(0.1f)] private float interactionRadius = 2f;
    [SerializeField, Range(1f, 360f)] private float maximumViewAngle = 140f;
    [SerializeField] private LayerMask interactionLayers = ~0;

    [Header("Input")]
    [SerializeField] private KeyCode interactionKey = KeyCode.E;

    [Header("UI")]
    [SerializeField] private InteractionPromptUI promptUI;

    private readonly Collider[] hitBuffer = new Collider[16];
    private IInteractable currentInteractable;

    private void Update()
    {
        if (PauseManager.IsPaused)
        {
            SetCurrentInteractable(null);
            return;
        }

        FindBestInteractable();

        if (currentInteractable == null || !Input.GetKeyDown(interactionKey))
        {
            return;
        }

        if (currentInteractable.CanInteract(gameObject))
        {
            currentInteractable.Interact(gameObject);
        }
    }

    private void FindBestInteractable()
    {
        Vector3 center = GetDetectionCenter();
        int hitCount = Physics.OverlapSphereNonAlloc(
            center,
            interactionRadius,
            hitBuffer,
            interactionLayers,
            QueryTriggerInteraction.Collide);

        IInteractable bestInteractable = null;
        float bestDistanceSquared = float.MaxValue;

        for (int i = 0; i < hitCount; i++)
        {
            Collider hitCollider = hitBuffer[i];

            if (hitCollider == null)
            {
                continue;
            }

            IInteractable candidate = hitCollider.GetComponentInParent<IInteractable>();

            if (candidate == null || !candidate.CanInteract(gameObject))
            {
                continue;
            }

            Transform interactionPoint = candidate.InteractionPoint;
            Vector3 point = interactionPoint != null
                ? interactionPoint.position
                : hitCollider.transform.position;

            if (!IsInsideViewAngle(point))
            {
                continue;
            }

            float distanceSquared = (point - center).sqrMagnitude;

            if (distanceSquared >= bestDistanceSquared)
            {
                continue;
            }

            bestInteractable = candidate;
            bestDistanceSquared = distanceSquared;
        }

        SetCurrentInteractable(bestInteractable);
    }

    private bool IsInsideViewAngle(Vector3 targetPosition)
    {
        Vector3 originPosition = detectionOrigin != null
            ? detectionOrigin.position
            : transform.position;

        Vector3 direction = Vector3.ProjectOnPlane(
            targetPosition - originPosition,
            Vector3.up);

        if (direction.sqrMagnitude <= NumericGuard.MinDenominator)
        {
            return true;
        }

        float angle = Vector3.Angle(transform.forward, direction);
        return angle <= maximumViewAngle * 0.5f;
    }

    private Vector3 GetDetectionCenter()
    {
        Transform origin = detectionOrigin != null ? detectionOrigin : transform;
        return origin.TransformPoint(detectionOffset);
    }

    private void SetCurrentInteractable(IInteractable interactable)
    {
        currentInteractable = interactable;

        if (promptUI == null)
        {
            return;
        }

        if (currentInteractable == null)
        {
            promptUI.Hide();
            return;
        }

        string prompt = currentInteractable.GetInteractionPrompt(gameObject);
        promptUI.Show($"[{interactionKey}] {prompt}");
    }

    private void OnDisable()
    {
        currentInteractable = null;
        promptUI?.Hide();
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(GetDetectionCenter(), interactionRadius);
    }
#endif
}