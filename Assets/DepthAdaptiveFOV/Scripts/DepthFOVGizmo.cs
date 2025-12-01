using UnityEngine;

[ExecuteAlways]
public class DepthFOVGizmo : MonoBehaviour
{
    [Header("Depth FOV Parameters")]
    public float farFOV = 60f;
    public float farDistance = 1f;

    [Header("Visualization")]
    public float maxDepth = 2f;
    public int depthSteps = 10;
    public float frustumSize = 0.5f;

    private void OnDrawGizmos()
    {
        var cam = GetComponent<Camera>();
        if (cam == null) return;

        float aspect = cam.aspect;

        // 各深度での視錐台断面を描画
        for (int i = 0; i <= depthSteps; i++)
        {
            float depth = (float)i / depthSteps * maxDepth;
            if (depth < 0.01f) depth = 0.01f;

            // カメラのFOVをnearFOVとして使用
            float nearScale = Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
            float farScale = Mathf.Tan(farFOV * 0.5f * Mathf.Deg2Rad);

            // 非線形補間: depth=0 → t=0, depth=farDist → t=0.5, depth=∞ → t=1
            float t = depth / (depth + farDistance);
            float currentScale = Mathf.Lerp(nearScale, farScale, t);

            // この深度での視錐台サイズ
            float halfHeight = currentScale * depth;
            float halfWidth = halfHeight * aspect;

            // 通常の透視投影での視錐台（比較用）
            float normalScale = Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
            float normalHalfHeight = normalScale * depth;
            float normalHalfWidth = normalHalfHeight * aspect;

            Vector3 center = transform.position + transform.forward * depth;

            // 歪んだ視錐台（緑）
            Gizmos.color = new Color(0, 1, 0, 0.8f);
            DrawFrustumSlice(center, halfWidth, halfHeight);

            // 通常の視錐台（赤、半透明）
            Gizmos.color = new Color(1, 0, 0, 0.3f);
            DrawFrustumSlice(center, normalHalfWidth, normalHalfHeight);
        }

        // 深度方向のライン
        Gizmos.color = new Color(0, 1, 0, 0.5f);
        DrawFrustumEdges(aspect);
    }

    private void DrawFrustumSlice(Vector3 center, float halfWidth, float halfHeight)
    {
        Vector3 right = transform.right * halfWidth;
        Vector3 up = transform.up * halfHeight;

        Vector3 tl = center - right + up;
        Vector3 tr = center + right + up;
        Vector3 bl = center - right - up;
        Vector3 br = center + right - up;

        Gizmos.DrawLine(tl, tr);
        Gizmos.DrawLine(tr, br);
        Gizmos.DrawLine(br, bl);
        Gizmos.DrawLine(bl, tl);
    }

    private void DrawFrustumEdges(float aspect)
    {
        int segments = 20;
        Vector3[] corners = new Vector3[4]; // TL, TR, BL, BR

        for (int i = 0; i < segments; i++)
        {
            float depth0 = (float)i / segments * maxDepth;
            float depth1 = (float)(i + 1) / segments * maxDepth;
            if (depth0 < 0.01f) depth0 = 0.01f;
            if (depth1 < 0.01f) depth1 = 0.01f;

            float t0 = depth0 / (depth0 + farDistance);
            float t1 = depth1 / (depth1 + farDistance);

            var cam = GetComponent<Camera>();
            float nearScale = Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
            float farScale = Mathf.Tan(farFOV * 0.5f * Mathf.Deg2Rad);

            float scale0 = Mathf.Lerp(nearScale, farScale, t0);
            float scale1 = Mathf.Lerp(nearScale, farScale, t1);

            float hw0 = scale0 * depth0 * aspect;
            float hh0 = scale0 * depth0;
            float hw1 = scale1 * depth1 * aspect;
            float hh1 = scale1 * depth1;

            Vector3 c0 = transform.position + transform.forward * depth0;
            Vector3 c1 = transform.position + transform.forward * depth1;

            // 4つの角のライン
            Gizmos.DrawLine(c0 + transform.right * hw0 + transform.up * hh0,
                           c1 + transform.right * hw1 + transform.up * hh1);
            Gizmos.DrawLine(c0 - transform.right * hw0 + transform.up * hh0,
                           c1 - transform.right * hw1 + transform.up * hh1);
            Gizmos.DrawLine(c0 + transform.right * hw0 - transform.up * hh0,
                           c1 + transform.right * hw1 - transform.up * hh1);
            Gizmos.DrawLine(c0 - transform.right * hw0 - transform.up * hh0,
                           c1 - transform.right * hw1 - transform.up * hh1);
        }
    }
}
