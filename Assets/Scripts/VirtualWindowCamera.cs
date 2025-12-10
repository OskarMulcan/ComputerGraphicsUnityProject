using UnityEngine;

[ExecuteInEditMode]
public class VirtualWindowCamera : MonoBehaviour
{
    [Header("References")]
    public Transform screenPlane;
    public MediaPipeHeadTracker headTracker;

    [Header("Settings")]
    public bool drawGizmos = true;

    private Camera _cam;

    void Start()
    {
        _cam = GetComponent<Camera>();
    }

    
    void LateUpdate()
    {
        if (screenPlane == null || _cam == null || headTracker == null) return;

        Vector3 headWorldPos = headTracker.transform.position + headTracker.HeadPosition;

        if (headTracker != null)
        {
            Vector3 trackedPos = headTracker.HeadPosition;
            Quaternion trackedRot = headTracker.HeadRotation;

           
            if (trackedPos.sqrMagnitude > 0.0001f)
            {
                Debug.Log($"[CAMERA INPUT] Reading Pos: {trackedPos}");
            }
            Vector3 worldHeadPos = headTracker.transform.position + trackedPos;
            transform.position = worldHeadPos;
            transform.rotation = trackedRot;
            UpdateProjectionMatrix(transform.position);
        }
    }

    void UpdateProjectionMatrix(Vector3 headPos)
    {
        Vector3 bl = screenPlane.TransformPoint(new Vector3(-0.5f, -0.5f, 0));
        Vector3 br = screenPlane.TransformPoint(new Vector3(0.5f, -0.5f, 0));
        Vector3 tl = screenPlane.TransformPoint(new Vector3(-0.5f, 0.5f, 0));

        Vector3 vb = transform.InverseTransformPoint(bl);
        Vector3 vr = transform.InverseTransformPoint(br);
        Vector3 vt = transform.InverseTransformPoint(tl);

        float near = _cam.nearClipPlane;
        float far = _cam.farClipPlane;

        float d = -vb.z;
        if (Mathf.Abs(d) < 0.001f) d = 0.001f;
        float scale = near / d;

        float left = vb.x * scale;
        float right = vr.x * scale;
        float bottom = vb.y * scale;
        float top = vt.y * scale;

        Matrix4x4 m = Matrix4x4.Frustum(left, right, bottom, top, near, far);
        _cam.projectionMatrix = m;
    }

    void OnDrawGizmos()
    {
        if (!drawGizmos || screenPlane == null) return;

        Gizmos.color = Color.yellow;
        Vector3 bl = screenPlane.TransformPoint(new Vector3(-0.5f, -0.5f, 0));
        Vector3 br = screenPlane.TransformPoint(new Vector3(0.5f, -0.5f, 0));
        Vector3 tl = screenPlane.TransformPoint(new Vector3(-0.5f, 0.5f, 0));
        Vector3 tr = screenPlane.TransformPoint(new Vector3(0.5f, 0.5f, 0));

        Gizmos.DrawLine(transform.position, bl);
        Gizmos.DrawLine(transform.position, br);
        Gizmos.DrawLine(transform.position, tl);
        Gizmos.DrawLine(transform.position, tr);
    }
}