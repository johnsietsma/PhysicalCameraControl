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
        set => m_Camera.focalLength = value;
    }


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
                if(profile.TryGet(typeof(Exposure), out Exposure exp) && exp.active)
                {
                    IsUsingPhysicalExposure = exp.mode == ExposureMode.UsePhysicalCamera;
                }
            }
        }
    }
}