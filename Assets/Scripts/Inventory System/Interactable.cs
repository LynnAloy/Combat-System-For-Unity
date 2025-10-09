using UnityEngine;


public class Interactable : MonoBehaviour
{

    public float radius = 3f;
    public Transform interactionTransform;
    //bool isFocus = false;   
    Transform player;       

    bool hasInteracted = false; 

    void Update()
    {
        float distance = Vector3.Distance(player.position, interactionTransform.position);
        if (!hasInteracted && distance <= radius)
        {
            hasInteracted = true;
            Interact();
        }
    }

    /*
    public void OnFocused(Transform playerTransform)
    {
        isFocus = true;
        hasInteracted = false;
        player = playerTransform;
    }

    // Called when the object is no longer focused
    public void OnDefocused()
    {
        isFocus = false;
        hasInteracted = false;
        player = null;
    }
    */

    public virtual void Interact()
    {

    }

    /*
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(interactionTransform.position, radius);
    }
    */
}
