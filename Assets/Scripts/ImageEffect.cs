using UnityEngine;

public class ImageEffect : MonoBehaviour
{
    [SerializeField] Material imageEffectMat;
    [SerializeField] GameObject box;
    private Camera cam;

    void Start()
    {
        cam = GetComponent<Camera>();
        cam.depthTextureMode = cam.depthTextureMode | DepthTextureMode.Depth;
    }
    
    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Vector3 extents = box.transform.localScale / 2.0f;
        Vector3 center = box.transform.position;
        Vector3[] axes =
        {
            box.transform.rotation * Vector3.right,
            box.transform.rotation * Vector3.up,
            box.transform.rotation * Vector3.forward
        };

        imageEffectMat.SetVector("_BoxCenter", new Vector4(center.x, center.y, center.z, 0.0f));
        imageEffectMat.SetVector("_BoxExtents", new Vector4(extents.x, extents.y, extents.z, 0.0f));
        imageEffectMat.SetVector("_BoxX", new Vector4(axes[0].x, axes[0].y, axes[0].z, 0.0f));
        imageEffectMat.SetVector("_BoxY", new Vector4(axes[1].x, axes[1].y, axes[1].z, 0.0f));
        imageEffectMat.SetVector("_BoxZ", new Vector4(axes[2].x, axes[2].y, axes[2].z, 0.0f));

        Graphics.Blit(source, destination, imageEffectMat);
    }
}
