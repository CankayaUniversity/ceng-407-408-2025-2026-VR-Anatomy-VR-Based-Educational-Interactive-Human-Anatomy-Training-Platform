using UnityEngine;
using UnityEngine.InputSystem;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.Utilities;
#endif

public class ForceLeftPoseActions : MonoBehaviour
{
    [Header("Assign these from XRI Default Input Actions")]
    public InputActionReference position;
    public InputActionReference rotation;
    public InputActionReference trackingState;

    void Awake()
    {
        // 1) Find the Tracked Pose Driver component (type name varies across Unity/XRI versions)
        var tpd =
            (Component)GetComponent("TrackedPoseDriver") ??
            (Component)GetComponent("UnityEngine.InputSystem.XR.TrackedPoseDriver") ??
            (Component)GetComponent("UnityEngine.XR.Interaction.Toolkit.Inputs.TrackedPoseDriver");

        if (tpd == null)
        {
            Debug.LogError("[ForceLeftPoseActions] Tracked Pose Driver not found on this GameObject.", this);
            return;
        }

        // 2) Convert InputActionReference -> InputActionProperty (newer API)
        var posProp = new InputActionProperty(position);
        var rotProp = new InputActionProperty(rotation);
        var stateProp = new InputActionProperty(trackingState);

        // 3) Assign via reflection so it works across different class definitions
        SetFieldOrProperty(tpd, "positionInput", posProp);
        SetFieldOrProperty(tpd, "rotationInput", rotProp);
        SetFieldOrProperty(tpd, "trackingStateInput", stateProp);

        Debug.Log("[ForceLeftPoseActions] Pose actions assigned on Awake.", this);
    }

    private static void SetFieldOrProperty(Component target, string name, InputActionProperty value)
    {
        var type = target.GetType();

        // Try property first
        var prop = type.GetProperty(name);
        if (prop != null && prop.CanWrite && prop.PropertyType == typeof(InputActionProperty))
        {
            prop.SetValue(target, value);
            return;
        }

        // Then field
        var field = type.GetField(name);
        if (field != null && field.FieldType == typeof(InputActionProperty))
        {
            field.SetValue(target, value);
            return;
        }

        Debug.LogWarning($"[ForceLeftPoseActions] Could not set '{name}'. Field/Property not found or type mismatch on {type.FullName}.", target);
    }
}
