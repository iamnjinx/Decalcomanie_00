using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FirstPersonInteraction : MonoBehaviour
{
    public float interactionDistance = 10f;
    public LayerMask interactableLayer;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            Interact();
        }
    }

    void Interact()
    {
        Ray ray = new Ray(transform.position, transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactionDistance, interactableLayer))
        {
            IClickable interactable = hit.collider.GetComponent<IClickable>();
            if (interactable != null)
            {
                interactable.OnClick();
            }
        }
    }

    void OnDrawGizmos()
    {
        Ray ray = new Ray(transform.position, transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactionDistance, interactableLayer))
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, hit.point);
            Gizmos.DrawSphere(hit.point, 0.1f);
        }
        else
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, transform.position + transform.forward * interactionDistance);
        }
    }
}
