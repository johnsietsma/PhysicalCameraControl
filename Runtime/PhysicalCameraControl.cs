using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

[ExecuteInEditMode]
[RequireComponent(typeof(HDAdditionalCameraData))]
public class PhysicalCameraControl : MonoBehaviour
{
    public bool IsUsingPhysicalProperties => m_Camera.usePhysicalProperties;
    public bool IsUsingPhysicalExposure { get; private set; }
    public float ActualAperture => m_Camera.focalLength / m_CameraData.aperture;

    public int ISO
    {
        get => m_CameraData.iso;
        set => m_CameraData.iso = value;
    }

    public float ShutterSpeed
    {
        get => m_CameraData.shutterSpeed;
        set => m_CameraData.shutterSpeed = value;
    }

    public float FStop
    {
        get => m_CameraData.aperture;
        set => m_CameraData.aperture = value;
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
    [SerializeField] Transform m_FocusObject = default;

    Camera m_Camera;
    HDPhysicalCamera m_CameraData;

    void OnEnable()
    {
        m_Camera = GetComponent<Camera>();
        m_CameraData = GetComponent<HDAdditionalCameraData>().physicalParameters;

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

    void SetFocalLength(float focalLength)
    {
        if (Mathf.Approximately(m_Camera.focalLength, focalLength)) return;
        
        if (m_DollyZoom && m_FocusObject)
        {
            var lengthRatio = focalLength / FocalLength;
            var newDistance = lengthRatio * FocusDistance;
            var distanceDelta = newDistance / FocusDistance;
            var moveVector = m_FocusObject.position - transform.position;
            transform.position = m_FocusObject.position - moveVector * distanceDelta;
        }

        m_Camera.focalLength = focalLength;
    }

    // Reference: https://www.scantips.com/lights/fieldofviewmath.html
    float CalculateFOV(float size)
    {
        return Mathf.Rad2Deg * 2 * Mathf.Atan2(size, 2 * FocalLength);
    }

    public float CalculateFocalLength(float fov)
    {
        return (m_Camera.sensorSize.x / 2) / Mathf.Tan(fov * Mathf.Deg2Rad / 2);
    }
}