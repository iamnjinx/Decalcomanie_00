using UnityEngine;

public class PaperBoard : MonoBehaviour
{
    [SerializeField] private Transform lowerLeft;
    [SerializeField] private Transform upperLeft;
    [SerializeField] private Transform lowerRight;
    [SerializeField] private Transform upperRight;

    public Transform LowerLeft => lowerLeft;
    public Transform UpperLeft => upperLeft;
    public Transform LowerRight => lowerRight;
    public Transform UpperRight => upperRight;
}
