using UnityEngine;

public class GizmosHelpers
{
    public static void DrawTriangle(Vector3 A, Vector3 B, Vector3 C)
    {
        Gizmos.DrawLine(A, B);
        Gizmos.DrawLine(B, C);
        Gizmos.DrawLine(C, A);
    }

    public static void DrawRectangle(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
    {
        Gizmos.DrawLine(a, b);
        Gizmos.DrawLine(c, d);
        Gizmos.DrawLine(a, c);
        Gizmos.DrawLine(b, d);
    }
}

public class Plane
{
    public Vector3 normal;
    public Vector3 p;

    public Plane(Vector3 pNormal, Vector3 pP)
    {
        normal = Vector3.Normalize(pNormal);
        p = pP;
    }

    public void DrawPlane(float size)
    {
        Gizmos.DrawLine(p, p + normal);
    }

    public float SignedDistance(Vector3 q)
    {
        return Vector3.Dot(normal, q - p);
    }

    public bool RayIntersection(Ray ray, out Vector3 hitPoint)
    {
        hitPoint = new Vector3(Mathf.Infinity, Mathf.Infinity, Mathf.Infinity);
        try
        {
            float t = Vector3.Dot(normal, p - ray.origin) / Vector3.Dot(ray.dir, normal);
            if (t < 0) return false;

            hitPoint = ray.GetPoint(t);
            return true;
        }
        catch
        {
            return false;
        }

    }
}

public class OBB
{
    private Vector3 extents;
    private Vector3 center;
    private Vector3[] axes;
    private Vector3 min;
    private Vector3 max;

    public OBB(Vector3 pExtents, Vector3 pCenter, Vector3[] pAxes)
    {
        extents = new Vector3(pExtents.x, pExtents.y, pExtents.z);
        center = new Vector3(pCenter.x, pCenter.y, pCenter.z);

        axes = new Vector3[3];
        axes[0] = pAxes[0];
        axes[1] = pAxes[1];
        axes[2] = pAxes[2];

        min = center - extents;
        max = center + extents;
    }

    public void Draw(Quaternion rotation)
    {
        Matrix4x4 boxMatrix = Matrix4x4.TRS(center, rotation, new Vector3(1, 1, 1));
        Gizmos.matrix = boxMatrix;
        Gizmos.DrawWireCube(Vector3.zero, extents * 2.0f);
        Gizmos.matrix = Matrix4x4.identity;
    }

    public Vector3 ToLocalCoords(Vector3 worldPos)
    {
        Vector3 result;
        result.x = Vector3.Dot(worldPos - center, axes[0]);
        result.y = Vector3.Dot(worldPos - center, axes[1]);
        result.z = Vector3.Dot(worldPos - center, axes[2]);
        return result;
    }

    public Vector3 ToWorldCoords(Vector3 localPos)
    {
        Vector3 result = center;
        for (int i = 0; i < 3; i++)
        {
            result += localPos[i] * axes[i];
        }
        return result;
    }

    public bool RayIntersection(Ray ray, out Vector3 entryPoint, out Vector3 exitPoint)
    {
        entryPoint = Vector3.zero;
        exitPoint = Vector3.zero;

        float tmin = 0.0f;
        float tmax = Mathf.Infinity;

        Vector3 p = ToLocalCoords(ray.origin);
        Vector3 d = ToLocalCoords(ray.dir + center);

        Vector3 newMin = -extents;
        Vector3 newMax = extents;

        for (int i = 0; i < 3; i++)
        {
            if (Mathf.Abs(d[i]) < 0.000001)
            {
                if (p[i] < newMin[i] || p[i] > newMax[i]) return false;
            }
            else
            {
                float t1 = (newMin[i] - p[i]) / d[i];
                float t2 = (newMax[i] - p[i]) / d[i];

                if (t1 > t2) (t1, t2) = (t2, t1);

                if (t1 > tmin) tmin = t1;
                if (t2 < tmax) tmax = t2;

                if (tmin > tmax) return false;
            }
        }

        Vector3 localEntry = p + d * tmin;
        Vector3 localExit = p + d * tmax;

        entryPoint = ToWorldCoords(localEntry);
        exitPoint = ToWorldCoords(localExit);

        return true;
    }
}

public class Ray
{
    public Vector3 origin;
    public Vector3 dir;

    public Ray(Vector3 pOrigin, Vector3 pDirection)
    {
        origin = pOrigin;
        dir = Vector3.Normalize(pDirection);
    }

    public Vector3 GetPoint(float t)
    {
        return origin + dir * t;
    }

    public void DrawRay()
    {
        Gizmos.DrawLine(origin, origin + dir);
    }
}

public class RayCastTest : MonoBehaviour
{
    [SerializeField] private Transform rayObject;
    [SerializeField] private Transform planeObject;
    [SerializeField] private GameObject boxObject;

    private Plane plane;
    private OBB box;
    private Ray ray;

    void Start()
    {
        Vector3[] axes =
        {
            boxObject.transform.rotation * Vector3.right,
            boxObject.transform.rotation * Vector3.up,
            boxObject.transform.rotation * Vector3.forward
        };

        if (rayObject != null) ray = new Ray(rayObject.position, rayObject.forward);
        if (planeObject != null) plane = new Plane(Vector3.Cross(planeObject.forward, planeObject.right), planeObject.position);
        if (boxObject != null) box = new OBB(boxObject.GetComponent<Collider>().bounds.extents, boxObject.transform.position, axes);
    }

    void Update()
    {
        Vector3[] axes =
        {
            boxObject.transform.rotation * Vector3.right,
            boxObject.transform.rotation * Vector3.up,
            boxObject.transform.rotation * Vector3.forward
        };

        if (rayObject != null) ray = new Ray(rayObject.position, rayObject.forward);
        if (planeObject != null) plane = new Plane(Vector3.Cross(planeObject.forward, planeObject.right), planeObject.position);
        if (boxObject != null) box = new OBB(boxObject.transform.localScale/2.0f, boxObject.transform.position, axes);
    }

    void OnDrawGizmos()
    {
        if (ray == null) return;

        Gizmos.color = Color.green;
        ray.DrawRay();

        if (plane != null)
        {
            Gizmos.color = Color.green;
            plane.DrawPlane(0.0f);
            Vector3 hitPoint = Vector3.zero;
            if (plane.RayIntersection(ray, out hitPoint))
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(ray.origin, hitPoint);
            }
        }
        if (box != null)
        {
            Gizmos.color = Color.green;
            box.Draw(boxObject.transform.rotation);
            Vector3 entryPoint = Vector3.zero;
            Vector3 exitPoint = Vector3.zero;
            if (box.RayIntersection(ray, out entryPoint, out exitPoint))
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(ray.origin, exitPoint);
                Gizmos.DrawSphere(entryPoint, 0.1f);
                Gizmos.DrawSphere(exitPoint, 0.1f);
            }
        }

    }
}
