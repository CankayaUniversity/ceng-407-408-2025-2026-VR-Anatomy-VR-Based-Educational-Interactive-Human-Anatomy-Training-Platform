using UnityEngine;

public class KeepXRInsideRoom : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform xrOriginRoot;
    [SerializeField] private Transform head;
    [SerializeField] private BoxCollider roomBounds;

    [Header("Settings")]
    [SerializeField] private float margin = 0.15f;

    private void LateUpdate()
    {
        if (xrOriginRoot == null || head == null || roomBounds == null)
            return;

        Vector3 localHeadPosition = roomBounds.transform.InverseTransformPoint(head.position);

        Vector3 center = roomBounds.center;
        Vector3 halfSize = roomBounds.size * 0.5f;

        float minX = center.x - halfSize.x + margin;
        float maxX = center.x + halfSize.x - margin;
        float minZ = center.z - halfSize.z + margin;
        float maxZ = center.z + halfSize.z - margin;

        float clampedX = Mathf.Clamp(localHeadPosition.x, minX, maxX);
        float clampedZ = Mathf.Clamp(localHeadPosition.z, minZ, maxZ);

        Vector3 correctedLocalHeadPosition = new Vector3(
            clampedX,
            localHeadPosition.y,
            clampedZ
        );

        Vector3 correctedWorldHeadPosition =
            roomBounds.transform.TransformPoint(correctedLocalHeadPosition);

        Vector3 correction = correctedWorldHeadPosition - head.position;

        correction.y = 0f;

        if (correction.sqrMagnitude > 0.0001f)
        {
            xrOriginRoot.position += correction;
        }
    }
}