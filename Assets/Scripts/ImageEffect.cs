using UnityEngine;

public class ImageEffect : MonoBehaviour
{
    [SerializeField] Material imageEffectMat;
    [SerializeField] Material texMat;
    [SerializeField] GameObject box;
    [SerializeField] private float planeHeight;
    [SerializeField] private float absorption;
    [SerializeField] private Vector2Int noiseSize;
    [SerializeField] private int numChunks;
    [SerializeField] private ComputeShader noiseShader;
    [SerializeField] private float pointRadius = 2.5f;
    [SerializeField] private float lineRadius = 1.0f;
    [SerializeField] private float noiseDensity = 1.0f;
    [SerializeField] private bool visualizeNoise;

    private Camera cam;
    private RenderTexture noise;

    void OnEnable()
    {
        cam = GetComponent<Camera>();
        cam.depthTextureMode = cam.depthTextureMode | DepthTextureMode.Depth;

        noise = CalculateNoise(noiseSize.x, noiseSize.y);
    }

    RenderTexture CalculateNoise(int noiseX, int noiseY)
    {
        RenderTexture result = new RenderTexture(noiseX, noiseY, 0, RenderTextureFormat.ARGBFloat);
        result.enableRandomWrite = true;

        Vector2 chunkSize = new Vector2((float)noiseX / (float)numChunks, (float)noiseY / (float)numChunks);

        noiseShader.SetVector("_Resolution", new Vector4(noiseX, noiseY, 0.0f, 0.0f));
        noiseShader.SetTexture(0, "_Result", result);
        noiseShader.SetInt("_NumPoints", numChunks * numChunks);
        noiseShader.SetBuffer(0, "_Points", CalculatePoints(numChunks, chunkSize));
        noiseShader.SetVector("_ChunkSize", new Vector4(chunkSize.x, chunkSize.y, 0.0f, 0.0f));
        noiseShader.SetFloat("_PointRadius", pointRadius);
        noiseShader.SetFloat("_LineRadius", lineRadius);
        noiseShader.SetFloat("_DensityFactor", noiseDensity);

        uint threadGroupSizeX;
        uint threadGroupSizeY;
        noiseShader.GetKernelThreadGroupSizes(0, out threadGroupSizeX, out threadGroupSizeY, out _);
        int threadGroupsX = Mathf.CeilToInt(result.width / threadGroupSizeX);
        int threadGroupsY = Mathf.CeilToInt(result.height / threadGroupSizeY);
        noiseShader.Dispatch(0, threadGroupsX, threadGroupsY, 1);

        return result;
    }
    
    ComputeBuffer CalculatePoints(int numChunks, Vector2 chunkSize)
    {
        Vector2[] resultArray = new Vector2[numChunks * numChunks];
        for (int x = 0; x < numChunks; x++)
        {
            for (int y = 0; y < numChunks; y++)
            {
                Vector2 min = new Vector2(x * chunkSize.x, y * chunkSize.y);
                Vector2 max = min + new Vector2(chunkSize.x, chunkSize.y);
                resultArray[x + (y * numChunks)] = new Vector2(Random.Range(min.x, max.x), Random.Range(min.y, max.y));
            }
        }
        ComputeBuffer result = new ComputeBuffer(resultArray.Length, sizeof(float) * 2);
        result.SetData(resultArray);
        return result;
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

        imageEffectMat.SetFloat("_PlaneHeight", planeHeight);
        imageEffectMat.SetFloat("_Absorption", absorption);
        imageEffectMat.SetVector("_BoxCenter", new Vector4(center.x, center.y, center.z, 0.0f));
        imageEffectMat.SetVector("_BoxExtents", new Vector4(extents.x, extents.y, extents.z, 0.0f));
        imageEffectMat.SetVector("_BoxX", new Vector4(axes[0].x, axes[0].y, axes[0].z, 0.0f));
        imageEffectMat.SetVector("_BoxY", new Vector4(axes[1].x, axes[1].y, axes[1].z, 0.0f));
        imageEffectMat.SetVector("_BoxZ", new Vector4(axes[2].x, axes[2].y, axes[2].z, 0.0f));

        texMat.SetTexture("_OutputTex", noise);

        if (visualizeNoise)
        {
            Graphics.Blit(source, destination, texMat);
        }
        else
        {
            Graphics.Blit(source, destination, imageEffectMat);
        }
    }
}
