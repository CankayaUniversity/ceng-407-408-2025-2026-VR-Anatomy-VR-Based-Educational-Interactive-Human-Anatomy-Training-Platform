using UnityEngine;

public class RotateUnit_Inspection : MonoBehaviour
{
    public enum RotationAxis { X, Y, Z }

    [Header("Speed Settings")]
    [Range(10f, 200f)]
    public float rotationSpeed = 60f;

    [Header("Axis Configuration")]
    public RotationAxis selectedAxis = RotationAxis.Y;

    private bool _isRotating = false;
    private float _directionModifier = 0f;
    private Quaternion _initialLocalRotation;

    void Awake()
    {
        _initialLocalRotation = transform.localRotation;
    }

    void Update()
    {
        if (_isRotating)
        {
            ExecuteRotation();
        }
    }

    public void StartRotating(float direction)
    {
        _directionModifier = direction;
        _isRotating = true;
    }

    public void StopRotating()
    {
        _isRotating = false;
        _directionModifier = 0f;
    }

    public void ResetToInitialRotation()
    {
        StopRotating();
        transform.localRotation = _initialLocalRotation;
    }

    private void ExecuteRotation()
    {
        Vector3 rotationAmount = Vector3.zero;
        float calculatedStep = rotationSpeed * _directionModifier * Time.deltaTime;

        switch (selectedAxis)
        {
            case RotationAxis.X:
                rotationAmount.x = calculatedStep;
                break;
            case RotationAxis.Y:
                rotationAmount.y = calculatedStep;
                break;
            case RotationAxis.Z:
                rotationAmount.z = calculatedStep;
                break;
        }

        transform.Rotate(rotationAmount, Space.Self);
    }
}