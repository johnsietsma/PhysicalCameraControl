using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

/// <summary>
/// Controls of the HDRP Physical Camera.
/// This controller provides feedback and controls like dolly zoom and exposure locking.
// Reference: http://www.uscoles.com/fstop.htm
// Reference: https://www.scantips.com/lights/fieldofviewmath.html
/// </summary>
[ExecuteInEditMode]
[RequireComponent(typeof(HDAdditionalCameraData))]
public class PhysicalCameraControl : MonoBehaviour
{
    public Camera Camera => m_Camera;

    /// <summary>
    /// Is this camera using physical properties so that FoV is tied to focal length.
    /// </summary>
    public bool IsUsingPhysicalProperties => m_Camera.usePhysicalProperties;

    /// <summary>
    /// Is there an Exposure volume override present and active.
    /// </summary>
    public bool IsUsingExposure { get; private set; }

    /// <summary>
    /// Is the Exposure volume override using the physical camera properties.
    /// </summary>
    public bool IsUsingPhysicalExposure { get; private set; }

    /// <summary>
    /// Is there a depth of field post-processing volume override present and active.
    /// </summary>
    public bool IsUsingDepthOfField { get; private set; }

    /// <summary>
    /// Is the depth of field volume override using the physical camera properties.
    /// </summary>
    public bool IsUsingPhysicalDepthOfField { get; private set; }

    /// <summary>
    /// The aperture width, rather then the f/stop. The physical camera refers to f/stop as aperture.
    /// </summary>
    public float ActualAperture => m_Camera.focalLength / m_AdditionalCameraData.aperture;

    /// <summary>
    /// The surface area of the aperture. Can be used to exposure calculations. 
    /// </summary>
    public float ApertureArea => CalculateApertureArea(ActualAperture);

    public bool IsUsingDollyZoom => m_DollyZoom;

    public bool HasFocusObject => m_FocusObject != null;

    public int ISO
    {
        get => m_AdditionalCameraData.iso;
        set => m_AdditionalCameraData.iso = value;
    }

    public float ShutterSpeed
    {
        get => m_AdditionalCameraData.shutterSpeed;
        set => SetShutterSpeed(value);
    }

    /// <summary>
    /// The f/stop or aperture of the lens (as opposed to the aperture width).
    /// </summary>
    public float FStop
    {
        get => m_AdditionalCameraData.aperture;
        set => SetFStop(value);
    }

    /// <summary>
    /// The distance between the sensor and the lens.
    /// </summary>
    public float FocalLength
    {
        get => m_Camera.focalLength;
        set => SetFocalLength(value);
    }

    /// <summary>
    /// The object that defines the focal plane.
    /// </summary>
    public Transform FocusObject
    {
        get => m_FocusObject;
        set => SetFocusObject(value);
    }

    /// <summary>
    /// The distance from the lens to the focus plane in the scene. Or 0 if there is no focus object/plane.
    /// </summary>
    public float FocusDistance => Vector3.Distance(m_FocalPlane.ClosestPointOnPlane(transform.position), transform.position);

    /// <summary>
    /// The diagonal length of the sensor.
    /// </summary>
    public float SensorDiagonal => m_Camera.sensorSize.magnitude;

    /// <summary>
    /// The field of view based on the sensor size and focal length of the camera. This will be different to camera
    /// component's field of view if the camera is not linked to the physical properties.
    /// </summary>
    public float HorizontalFOV => CalculateFieldOfView(m_Camera.sensorSize.x);

    public float VerticalFOV => CalculateFieldOfView(m_Camera.sensorSize.y);

    [Tooltip(
        "If either the f/stop or shutter speed is changed the other is automatically adjusted to keep the same exposure.")]
    [SerializeField]
    bool m_LockExposure = default;

    [Tooltip(
        "As the focal length is changed move the camera towards the focus object so that it keeps the same screen size.")]
    [SerializeField]
    bool m_DollyZoom = default;

    Transform m_FocusObject = default;
    Camera m_Camera;
    HDPhysicalCamera m_AdditionalCameraData;
    Plane m_FocalPlane = new Plane();

    void OnEnable()
    {
        m_Camera = GetComponent<Camera>();
        m_AdditionalCameraData = GetComponent<HDAdditionalCameraData>().physicalParameters;

        CheckForPhysicalExposureAndDepthOfField();
    }

    /// <summary>
    /// Check the the exposure and depth of field volume overrides are present and using the physical camera.
    /// </summary>
    public void CheckForPhysicalExposureAndDepthOfField()
    {
        IsUsingExposure = false;
        IsUsingPhysicalExposure = false;
        IsUsingDepthOfField = false;
        IsUsingPhysicalDepthOfField = false;

        var volumes = FindObjectsOfType<Volume>();
        foreach (var volume in volumes)
        {
            var profile = volume.profile;
            if (!IsUsingPhysicalExposure && profile.Has(typeof(Exposure)))
            {
                if (profile.TryGet(typeof(Exposure), out Exposure exp) && exp.active)
                {
                    IsUsingExposure = true;
                    IsUsingPhysicalExposure = exp.mode == ExposureMode.UsePhysicalCamera;
                }
            }

            if (!IsUsingPhysicalDepthOfField && profile.Has(typeof(DepthOfField)))
            {
                if (profile.TryGet(typeof(DepthOfField), out DepthOfField dof) && dof.active)
                {
                    IsUsingDepthOfField = true;
                    IsUsingPhysicalDepthOfField = dof.focusMode == DepthOfFieldMode.UsePhysicalCamera;
                }
            }
        }
    }

    /// <summary>
    /// Set the focal length of the camera. If dolly zoom is on then this will also chance the camera position.
    /// </summary>
    /// <param name="newFocalLength"></param>
    void SetFocalLength(float newFocalLength)
    {
        if (Mathf.Approximately(m_Camera.focalLength, newFocalLength)) return;

        if (m_DollyZoom && HasFocusObject)
        {
            // Use the ratio of old and new camera focal length and old focus distance to find the new focus distance.
            // Focal length is the distance from sensor to lens. Focal distance is the distance to the focal plane.
            var lengthRatio = newFocalLength / FocalLength;
            var newDistance = lengthRatio * FocusDistance;
            transform.position += transform.forward * (FocusDistance-newDistance);
        }

        m_Camera.focalLength = newFocalLength;
    }

    /// <summary>
    /// Set the shutter speed. This will also adjust the aperture of the exposure is locked.
    /// </summary>
    void SetShutterSpeed(float newShutterSpeed)
    {
        if (Mathf.Approximately(m_AdditionalCameraData.shutterSpeed, newShutterSpeed)) return;

        if (m_LockExposure)
        {
            // Scale the aperture area by the change in light level of the new shutter speed
            var shutterSpeedRatio = ShutterSpeed / newShutterSpeed;
            var newArea = ApertureArea * shutterSpeedRatio;
            m_AdditionalCameraData.aperture =
                FocalLength / CalculateApertureFromArea(newArea); // Because aperture is actually f/stops!
        }

        m_AdditionalCameraData.shutterSpeed = newShutterSpeed;
    }

    /// <summary>
    /// Set the F/Stop. This will also set the shutter speed if the exposure is locked.
    /// </summary>
    void SetFStop(float newFStop)
    {
        var newAperture = FocalLength / newFStop;
        if (Mathf.Approximately(m_AdditionalCameraData.aperture, newAperture)) return;

        if (m_LockExposure)
        {
            // Scale the shutter speed by the change in light level of the new aperture
            var newApertureArea = CalculateApertureArea(newAperture);
            var apertureAreaRatio = ApertureArea / newApertureArea;
            m_AdditionalCameraData.shutterSpeed = ShutterSpeed * apertureAreaRatio;
        }

        m_AdditionalCameraData.aperture = newFStop;
    }

    void SetFocusObject(Transform focusObject)
    {
        if (focusObject != null)
        {
            m_FocusObject = focusObject;
            m_FocalPlane = new Plane(-transform.forward, m_FocusObject.position);
        }
    }

    /// <summary>
    /// Calculate the field of view based on sensor size and the current focal length.
    /// </summary>
    float CalculateFieldOfView(float sensorSize)
    {
        return Mathf.Rad2Deg * 2 * Mathf.Atan2(sensorSize, 2 * FocalLength);
    }

    float CalculateFocalLength(float fieldOfView)
    {
        return (m_Camera.sensorSize.x / 2) / Mathf.Tan(fieldOfView * Mathf.Deg2Rad / 2);
    }

    /// <summary>
    /// Calculate the aperture width from the given aperture area.
    /// </summary>
    float CalculateApertureFromArea(float area)
    {
        return Mathf.Sqrt(area / Mathf.PI) * 2;
    }

    /// <summary>
    /// Calculate the aperture area based on the given aperture width.
    /// </summary>
    float CalculateApertureArea(float aperture)
    {
        return (float) Math.Pow(aperture / 2, 2) * Mathf.PI;
    }
}