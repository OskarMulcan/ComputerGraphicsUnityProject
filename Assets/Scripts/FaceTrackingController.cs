using Mediapipe;
using Mediapipe.Unity;
using System.Collections.Generic;
using UnityEngine;



public class MediaPipeHeadTracker : MonoBehaviour
{
    public Quaternion HeadRotation { get; private set; }
    private Quaternion _targetRot;

    [Header("Configuration")]
    public float smoothing = 2f;
    public float sensitivity = 2f;
    public float z_sensitivity = 2f;

    [Header("Physical Setup (Estimated)")]
    public float estimatedIPD = 0.065f;
    public float webcamFov = 70f;
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
        var leftEye = landmarks[468];
        var rightEye = landmarks[473];

        float x = -(nose.x - 0.5f) * sensitivity;
        float y = (nose.y - 0.5f) * sensitivity;

        float eyeDistNorm = Vector2.Distance(new Vector2(leftEye.x, leftEye.y), new Vector2(rightEye.x, rightEye.y));
        

        float focalLen = 1.0f / Mathf.Tan(webcamFov * 0.5f * Mathf.Deg2Rad);
        float zDepth = -(estimatedIPD * focalLen) / eyeDistNorm * z_sensitivity;

        //zDepth = Mathf.Clamp(zDepth, 0.2f, 8f);
        //zDepth = Mathf.Clamp(zDepth, -0f, -0.2f);

        Debug.Log($"Landmarks updating! Nose X: {landmarks[1].x}, Y: {landmarks[1].y}");
        _targetPos = new Vector3(x, y, zDepth*20);
    }

    public float rotationWeight = 0.5f;
    public void UpdateHeadRotation(Matrix4x4 unityMatrix)
    {
        // We no longer need to check for null or build the matrix manually!
        // The plugin has already done the heavy lifting.

        // 1. Extract the rotation directly from the Unity Matrix
        Quaternion targetRotation = unityMatrix.rotation;

        // 2. Apply the Coordinate System Correction
        // We still need to flip the axes because head tracking often feels "inverted" 
        // without these negations (depending on if it's a mirror view or direct view).
        Vector3 euler = targetRotation.eulerAngles;

        euler.x = -euler.x; // Pitch
        euler.y = -euler.y; // Yaw
        euler.z = -euler.z; // Roll

        targetRotation = Quaternion.Euler(euler);
        targetRotation = Quaternion.Slerp(Quaternion.identity, targetRotation, rotationWeight);


        // 3. Set the target for smoothing
        _targetRot = targetRotation;
    }

    void Update()
    {
        HeadPosition = Vector3.Lerp(HeadPosition, _targetPos, Time.deltaTime * smoothing);
        //HeadRotation = Quaternion.Slerp(HeadRotation, _targetRot, Time.deltaTime * smoothing);
        if (HeadPosition.sqrMagnitude > 0.0001f)
        {
            Debug.Log($"[TRACKER OUTPUT] Pos: {HeadPosition} | Rot: {HeadRotation.eulerAngles}");
        }
    }
}
