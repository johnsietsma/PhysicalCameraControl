using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PhysicalCameraControl))]
public class PhysicalCameraControllerEditor : Editor
{
    static readonly string[] ShutterSpeedStrings = new[]
        {"8", "4", "2", "1", "1/2", "1/4", "1/8", "1/15", "1/30", "1/60", "1/125", "1/250", "1/500", "1/1000"};

    static readonly float[] ShutterSpeeds = new[]
    {
        8, 4, 2, 1, 1 / 2.0f, 1 / 4.0f, 1 / 8.0f, 1 / 15.0f, 1 / 30.0f, 1 / 60.0f, 1 / 125.0f, 1 / 250.0f, 1 / 500.0f,
        1 / 1000.0f
    };

    static readonly string[] ISOStrings = new[] {"100", "200", "400", "800", "1600", "3200", "6400"};
    static readonly int[] ISOs = new[] {100, 200, 400, 800, 1600, 3200, 6400};

    static readonly string[] FStopStrings = new[] {"1.0", "1.4", "2.0", "2.8", "4", "5.6", "8", "11", "16", "22"};
    static readonly float[] FStops = new[] {1.0f, 1.4f, 2.0f, 2.8f, 4, 5.6f, 8, 11, 16, 22};

    static readonly string[] FocalLengthStrings = new[] {"18", "24", "35", "55", "85", "105", "135", "200", "300"};
    static readonly int[] FocalLengths = new[] {18, 24, 35, 55, 85, 105, 135, 200, 300};

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        var cameraController = target as PhysicalCameraControl;
        if (!cameraController.IsUsingPhysicalProperties)
        {
            EditorGUILayout.LabelField("NOT USING PHYSICAL PROPERTIES! Click \"Link FOV to Physical Camera\" above.");
        }

        if (!cameraController.IsUsingPhysicalExposure)
        {
            EditorGUILayout.LabelField(
                "NOT USING PHYSICAL EXPOSURE! Add an exposure volume override with \"Use Physical Camera\".");
            if (GUILayout.Button("Check")) cameraController.CheckForPhysicalExposure();
        }

        EditorGUILayout.LabelField("Actual Aperture", $"{cameraController.ActualAperture.ToString("F2")}mm");

        EditorGUILayout.LabelField("ISO");
        EditorGUILayout.BeginHorizontal();
        for (int i = 0; i < ISOs.Length; i++)
        {
            if (GUILayout.Button(ISOStrings[i])) cameraController.ISO = ISOs[i];
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.LabelField("Shutter Speed");
        EditorGUILayout.BeginHorizontal();
        for (int i = 0; i < ShutterSpeeds.Length; i++)
        {
            if (GUILayout.Button(ShutterSpeedStrings[i])) cameraController.ShutterSpeed = ShutterSpeeds[i];
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.LabelField("F/Stop");
        EditorGUILayout.BeginHorizontal();
        for (int i = 0; i < FStops.Length; i++)
        {
            if (GUILayout.Button(FStopStrings[i])) cameraController.FStop = FStops[i];
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.LabelField("Focal Length");
        EditorGUILayout.BeginHorizontal();
        for (int i = 0; i < FocalLengths.Length; i++)
        {
            if (GUILayout.Button(FocalLengthStrings[i])) cameraController.FocalLength = FocalLengths[i];
        }

        EditorGUILayout.EndHorizontal();
    }
}