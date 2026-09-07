using UnityEngine;

public interface IInteractable
{
    Transform InteractionPoint { get; }

    bool CanInteract(GameObject interactor);

    string GetInteractionPrompt(GameObject interactor);

    void Interact(GameObject interactor);
}