using Mediapipe;
using Mediapipe.Unity;
using System.Collections.Generic;
using UnityEngine;



public class MediaPipeHeadTracker : MonoBehaviour
{
    public Quaternion HeadRotation { get; private set; }
    private Quaternion _targetRot;

    [Header("Configuration")]
    public float smoothing = 10f;
    public float sensitivity = 1.5f;

    [Header("Physical Setup (Estimated)")]
    public float estimatedIPD = 0.065f;
    public float webcamFov = 60f;
    public Vector3 HeadPosition { get; private set; }

    private Vector3 _targetPos;

    public void UpdateHeadPosition(List<Mediapipe.Tasks.Components.Containers.NormalizedLandmark> landmarks)
    {
        if (landmarks == null || landmarks.Count == 0)
        {
            Debug.LogWarning("No landmarks received or list is empty.");
            return;
        }
        if (landmarks == null || landmarks.Count == 0) return;

        var nose = landmarks[1];
        var leftEye = landmarks[33];
        var rightEye = landmarks[263];

        float x = -(nose.x - 0.5f) * sensitivity;
        float y = (nose.y - 0.5f) * sensitivity;

        float eyeDistNorm = Vector2.Distance(new Vector2(leftEye.x, leftEye.y), new Vector2(rightEye.x, rightEye.y));

        float focalLen = 1.0f / Mathf.Tan(webcamFov * 0.5f * Mathf.Deg2Rad);
        float zDepth = (estimatedIPD * focalLen) / eyeDistNorm;

        zDepth = Mathf.Clamp(zDepth, 0.2f, 1.5f);
        Debug.Log($"Landmarks updating! Nose X: {landmarks[1].x}, Y: {landmarks[1].y}");
        _targetPos = new Vector3(x, y, -zDepth);
    }

    public void UpdateHeadRotation(List<float> mpMatrix)
    {
        if (mpMatrix == null || mpMatrix.Count < 16)
        {
            Debug.LogWarning("Invalid rotation matrix received.");
            return;
        }

        Matrix4x4 unityMatrix = new Matrix4x4();

        // Column 0 (X-axis basis)
        unityMatrix.m00 = mpMatrix[0]; unityMatrix.m10 = mpMatrix[1]; unityMatrix.m20 = mpMatrix[2];

        // Column 1 (Y-axis basis)
        unityMatrix.m01 = mpMatrix[4]; unityMatrix.m11 = mpMatrix[5]; unityMatrix.m21 = mpMatrix[6];

        // Column 2 (Z-axis basis)
        unityMatrix.m02 = mpMatrix[8]; unityMatrix.m12 = mpMatrix[9]; unityMatrix.m22 = mpMatrix[10];

        // Column 3 (Translation) and Row 3 (Perspective)
        unityMatrix.m33 = 1;
        Quaternion targetRotation = unityMatrix.rotation;

        Vector3 euler = targetRotation.eulerAngles;

        euler.x = -euler.x;
        euler.y = -euler.y;
        euler.z = -euler.z;

        targetRotation = Quaternion.Euler(euler);
        _targetRot = targetRotation;
    }

    void Update()
    {
        HeadPosition = Vector3.Lerp(HeadPosition, _targetPos, Time.deltaTime * smoothing);
        HeadRotation = Quaternion.Slerp(HeadRotation, _targetRot, Time.deltaTime * smoothing);
        if (HeadPosition.sqrMagnitude > 0.0001f)
        {
            Debug.Log($"[TRACKER OUTPUT] Pos: {HeadPosition} | Rot: {HeadRotation.eulerAngles}");
        }
    }
}