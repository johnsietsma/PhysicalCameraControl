using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

[ExecuteInEditMode]
[RequireComponent(typeof(HDAdditionalCameraData))]
public class PhysicalCameraControl : MonoBehaviour
{
    public Camera Camera => m_Camera;
    public bool IsUsingPhysicalProperties => m_Camera.usePhysicalProperties;
    public bool IsUsingPhysicalExposure { get; private set; }
    public float ActualAperture => m_Camera.focalLength / m_AdditionalCameraData.aperture;
    public float ApertureArea => CalculateApertureArea(ActualAperture);
    
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

    public float FStop
    {
        get => m_AdditionalCameraData.aperture;
        set => SetFStop(value);
    }

    public float FocalLength
    {
        get => m_Camera.focalLength;
        set => SetFocalLength(value);
    }

    public float FocusDistance => m_FocusObject ? Vector3.Distance(transform.position, m_FocusObject.position) : 0;

    public float SensorDiagonal => m_Camera.sensorSize.magnitude;

    public float FOV => CalculateFOV(SensorDiagonal);
    public float HorizontalFOV => CalculateFOV(m_Camera.sensorSize.x);
    public float VerticalFOV => CalculateFOV(m_Camera.sensorSize.y);


    [SerializeField] bool m_DollyZoom = default;
    [SerializeField] bool m_LockExposure = default;
    [SerializeField] Transform m_FocusObject = default;

    Camera m_Camera;
    HDPhysicalCamera m_AdditionalCameraData;

    void OnEnable()
    {
        m_Camera = GetComponent<Camera>();
        m_AdditionalCameraData = GetComponent<HDAdditionalCameraData>().physicalParameters;

        CheckForPhysicalExposure();
    }

    public void CheckForPhysicalExposure()
    {
        IsUsingPhysicalExposure = false;

        var volumes = FindObjectsOfType<Volume>();
        foreach (var volume in volumes)
        {
            var profile = volume.profile;
            if (!IsUsingPhysicalExposure && profile.Has(typeof(Exposure)))
            {
                if (profile.TryGet(typeof(Exposure), out Exposure exp) && exp.active)
                {
                    IsUsingPhysicalExposure = exp.mode == ExposureMode.UsePhysicalCamera;
                }
            }
        }
    }

    void SetFocalLength(float newFocalLength)
    {
        if (Mathf.Approximately(m_Camera.focalLength, newFocalLength)) return;
        
        if (m_DollyZoom && m_FocusObject)
        {
            // Use the ration of old and new camera focal length and old focus distance to find the new focus distance
            // Focal length is the distance from sensor to lens. Focal distance is the distance to the scene object we want
            // to keep at the same screen size.
            var lengthRatio = newFocalLength / FocalLength;
            var newDistance = lengthRatio * FocusDistance;
            var distanceDelta = newDistance / FocusDistance;
            var moveVector = m_FocusObject.position - transform.position;
            transform.position = m_FocusObject.position - moveVector * distanceDelta;
        }

        m_Camera.focalLength = newFocalLength;
    }

    void SetShutterSpeed(float newShutterSpeed)
    {
        if (Mathf.Approximately(m_AdditionalCameraData.shutterSpeed, newShutterSpeed)) return;

        if (m_LockExposure)
        {
            var shutterSpeedRatio = ShutterSpeed / newShutterSpeed;
            var newArea = ApertureArea * shutterSpeedRatio;
            m_AdditionalCameraData.aperture = FocalLength / CalculateApertureFromArea(newArea); // Because aperture is actually f/stops!
        }

        m_AdditionalCameraData.shutterSpeed = newShutterSpeed;
    }
    
    void SetFStop(float newFStop)
    {
        var newAperture = FocalLength / newFStop;
        if (Mathf.Approximately(m_AdditionalCameraData.shutterSpeed, newAperture)) return;

        if (m_LockExposure)
        {
            var newApertureArea = CalculateApertureArea(newAperture);
            var apertureAreaRatio = ApertureArea / newApertureArea;
            m_AdditionalCameraData.shutterSpeed = ShutterSpeed * apertureAreaRatio;
        }

        m_AdditionalCameraData.aperture = newFStop;
    }

    // Reference: https://www.scantips.com/lights/fieldofviewmath.html
    float CalculateFOV(float size)
    {
        return Mathf.Rad2Deg * 2 * Mathf.Atan2(size, 2 * FocalLength);
    }

    float CalculateFocalLength(float fov)
    {
        return (m_Camera.sensorSize.x / 2) / Mathf.Tan(fov * Mathf.Deg2Rad / 2);
    }

    float CalculateApertureFromArea(float area)
    {
        return Mathf.Sqrt(area / Mathf.PI) * 2;
    }
    
    float CalculateApertureArea(float aperture)
    {
        return (float)Math.Pow(aperture / 2,2) * Mathf.PI;
    }

}