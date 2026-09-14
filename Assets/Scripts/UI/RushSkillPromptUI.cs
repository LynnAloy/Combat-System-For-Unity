using UnityEngine;

[DefaultExecutionOrder(100)]
[DisallowMultipleComponent]
public sealed class RushSkillPromptUI : MonoBehaviour
{
    [SerializeField] private PlayerRushSkill rushSkill;
    [SerializeField] private Canvas rootCanvas;
    [SerializeField] private RectTransform promptRoot;
    [SerializeField] private Camera worldCamera;
    [SerializeField] private Vector3 offsetFromHead = new(0f, 0.35f, 0f);
    [SerializeField, Min(0f)] private float fallbackHeight = 2f;

    private EnemyController currentTarget;
    private Transform currentHead;

    private void Awake()
    {
        if (rootCanvas == null)
        {
            rootCanvas = GetComponent<Canvas>();
        }

        if (worldCamera == null)
        {
            worldCamera = Camera.main;
        }

        SetVisible(false);
    }

    private void LateUpdate()
    {
        if (rushSkill == null ||
            promptRoot == null ||
            rootCanvas == null ||
            worldCamera == null)
        {
            SetVisible(false);
            return;
        }

        if (!rushSkill.TryGetAvailableTarget(out EnemyController target))
        {
            SetVisible(false);
            return;
        }

        if (target != currentTarget)
        {
            currentTarget = target;
            currentHead = ResolveHead(target);
        }

        Vector3 worldPosition = currentHead != null
            ? currentHead.position + offsetFromHead
            : target.transform.position + Vector3.up * fallbackHeight;

        Vector3 screenPosition = worldCamera.WorldToScreenPoint(worldPosition);

        if (screenPosition.z <= 0f ||
            screenPosition.x < 0f ||
            screenPosition.x > Screen.width ||
            screenPosition.y < 0f ||
            screenPosition.y > Screen.height)
        {
            SetVisible(false);
            return;
        }

        if (!TrySetScreenPosition(screenPosition))
        {
            SetVisible(false);
            return;
        }

        SetVisible(true);
    }

    private Transform ResolveHead(EnemyController target)
    {
        Animator targetAnimator = target.Animator;

        if (targetAnimator == null || !targetAnimator.isHuman)
        {
            return null;
        }

        return targetAnimator.GetBoneTransform(HumanBodyBones.Head);
    }

    private bool TrySetScreenPosition(Vector3 screenPosition)
    {
        if (rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            promptRoot.position = screenPosition;
            return true;
        }

        RectTransform canvasRect = (RectTransform)rootCanvas.transform;
        Camera canvasCamera = rootCanvas.worldCamera != null
            ? rootCanvas.worldCamera
            : worldCamera;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                screenPosition,
                canvasCamera,
                out Vector2 localPoint))
        {
            return false;
        }

        promptRoot.anchoredPosition = localPoint;
        return true;
    }

    private void SetVisible(bool visible)
    {
        if (promptRoot != null &&
            promptRoot.gameObject.activeSelf != visible)
        {
            promptRoot.gameObject.SetActive(visible);
        }
    }
}