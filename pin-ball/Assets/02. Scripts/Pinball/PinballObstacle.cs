using UnityEngine;

public enum EPinballObstacle
{
    SmallPin,
    BigBumper
}

public class PinballObstacle : MonoBehaviour
{
    [SerializeField] private EPinballObstacle type;

    public EPinballObstacle Type => type;
}
