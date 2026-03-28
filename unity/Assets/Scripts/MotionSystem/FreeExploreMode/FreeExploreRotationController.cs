using UnityEngine;

public class FreeExploreRotationController : MonoBehaviour
{
    [SerializeField] private Transform targetRoot;
    [SerializeField] private float rotationStep = 15f;

    private bool canRotate = false;

    public void EnableRotation()
    {
        canRotate = true;
    }

    public void DisableRotation()
    {
        canRotate = false;
    }

    public void RotateLeft()
    {
        if (!canRotate || targetRoot == null) return;
        targetRoot.Rotate(0f, -rotationStep, 0f, Space.World);
    }

    public void RotateRight()
    {
        if (!canRotate || targetRoot == null) return;
        targetRoot.Rotate(0f, rotationStep, 0f, Space.World);
    }
}