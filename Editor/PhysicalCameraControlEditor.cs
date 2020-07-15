using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PhysicalCameraControl))]
public class PhysicalCameraControllerEditor : Editor
{
    static readonly string[] ShutterSpeedStrings = new[]
        {"1/1000", "1/500", "1/250", "1/125", "1/60", "1/30", "1/15", "1/8", "1/4", "1/2", "1", "2", "4", "8"};

    static readonly float[] ShutterSpeeds = new[]
    {
        1 / 1000.0f, 1 / 500.0f, 1 / 250.0f, 1 / 125.0f, 1 / 60.0f, 1 / 30.0f, 1 / 15.0f, 1 / 8.0f, 1 / 4.0f, 1 / 2.0f,
        1, 2, 4, 8
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

        if (cameraController.IsUsingDollyZoom && !cameraController.HasFocusObject)
        {
            EditorGUILayout.LabelField("WARNING: Using dolly zoom without a focus object.");
        }
        
        if (!cameraController.IsUsingPhysicalProperties)
        {
            EditorGUILayout.LabelField("NOT USING PHYSICAL PROPERTIES! Click \"Link FOV to Physical Camera\" above.");
        }
        
        bool shouldCheckExposureAndDepthOfField = false;
        if (!cameraController.IsUsingExposure)
        {
            EditorGUILayout.LabelField(
                "NOT USING EXPOSURE! Add an exposure volume override with the mode \"Use Physical Camera\".");
            shouldCheckExposureAndDepthOfField = true;
        }

        if (cameraController.IsUsingExposure && !cameraController.IsUsingPhysicalExposure)
        {
            EditorGUILayout.LabelField(
                "NOT USING PHYSICAL EXPOSURE! Add an exposure volume override with the mode \"Use Physical Camera\".");
            shouldCheckExposureAndDepthOfField = true;
        }

        if (cameraController.IsUsingDepthOfField && !cameraController.IsUsingPhysicalDepthOfField)
        {
            EditorGUILayout.LabelField(
                "NOT USING PHYSICAL DEPTH OF FIELD! Change the Depth of Field focus mode to \"Use Physical Camera\".");
            shouldCheckExposureAndDepthOfField = true;
        }

        if (shouldCheckExposureAndDepthOfField)
        {
            if (GUILayout.Button("Check")) cameraController.CheckForPhysicalExposureAndDepthOfField();
        }


        EditorGUILayout.LabelField("Focus Distance", $"{cameraController.FocusDistance.ToString("F2")}");
        EditorGUILayout.LabelField("Actual aperture (diameter, area)",
            $"{cameraController.ActualAperture.ToString("F2")}mm, {cameraController.ApertureArea.ToString("F2")}mm^2");
        //EditorGUILayout.LabelField("Sensor diagonal", $"{cameraController.SensorDiagonal.ToString("F2")}mm");
        EditorGUILayout.LabelField("FOV (horizontal, vertical)",
            $"{cameraController.HorizontalFOV.ToString("F2")}, {cameraController.VerticalFOV.ToString("F2")}");

        EditorGUILayout.LabelField("ISO", cameraController.ISO.ToString());
        EditorGUILayout.BeginHorizontal();
        for (int i = 0; i < ISOs.Length; i++)
        {
            if (GUILayout.Button(ISOStrings[i])) cameraController.ISO = ISOs[i];
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.LabelField($"Shutter Speed (sec,1/sec)", $"{cameraController.ShutterSpeed.ToString("F2")}, {(1/cameraController.ShutterSpeed).ToString("F2")}");
        EditorGUILayout.BeginHorizontal();
        for (int i = 0; i < ShutterSpeeds.Length; i++)
        {
            if (GUILayout.Button(ShutterSpeedStrings[i])) cameraController.ShutterSpeed = ShutterSpeeds[i];
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.LabelField("F/Stop");
        cameraController.FStop = EditorGUILayout.Slider(cameraController.FStop, 1f, 32);

        EditorGUILayout.BeginHorizontal();
        for (int i = 0; i < FStops.Length; i++)
        {
            if (GUILayout.Button(FStopStrings[i])) cameraController.FStop = FStops[i];
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.LabelField("Focal Length");
        cameraController.FocalLength = EditorGUILayout.Slider(cameraController.FocalLength, 2, 500);
        EditorGUILayout.BeginHorizontal();
        for (int i = 0; i < FocalLengths.Length; i++)
        {
            if (GUILayout.Button(FocalLengthStrings[i])) cameraController.FocalLength = FocalLengths[i];
        }

        EditorGUILayout.EndHorizontal();

        // Make sure changes get reflected in the Camera component.
        EditorUtility.SetDirty(cameraController.Camera);
    }
}