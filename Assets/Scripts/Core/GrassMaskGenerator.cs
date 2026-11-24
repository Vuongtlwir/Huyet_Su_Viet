using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class GrassMaskGenerator : MonoBehaviour
{
    [Header("Material")]
    [SerializeField] private Material grassMaterial;
    [SerializeField] private string maskPropertyName = "_GrassMap";

    [Header("Arena")]
    [SerializeField] private Renderer grassRenderer;
    [SerializeField] private bool useRendererBounds = true;
    [SerializeField] private Vector2 arenaCenter;
    [SerializeField] private Vector2 arenaSize = new Vector2(60f, 60f);
    [SerializeField] private int resolution = 512;
    [SerializeField] private float rayStartHeight = 50f;
    [SerializeField] private float rayDistance = 100f;
    [SerializeField] private LayerMask groundLayers;
    [SerializeField] private LayerMask ignoreLayers;
    [SerializeField] private int paddingPixels = 1;

    [Header("UV Mapping")]
    [SerializeField] private bool flipX;
    [SerializeField] private bool flipZ = true;

    [Header("Trigger")]
    [SerializeField] private bool generateOnAwake = true;

    private const int MIN_RESOLUTION = 16;
    private const int MAX_RESOLUTION = 4096;
    private const int PROGRESS_STEP_ROWS = 32;

    private Texture2D maskTexture;
    private int maskPropertyId;

    private void Awake()
    {
        ApplyDefaults();
        maskPropertyId = Shader.PropertyToID(maskPropertyName);

        if (generateOnAwake)
        {
            GenerateMask();
        }
    }

    private void Reset()
    {
        ApplyDefaults();
    }

    private void ApplyDefaults()
    {
        if (groundLayers.value == 0)
        {
            groundLayers = LayerConfig.GroundMask;
        }

        if (ignoreLayers.value == 0)
        {
            ignoreLayers = (1 << LayerConfig.Player) | (1 << LayerConfig.Enemy);
        }
    }

    private void OnDestroy()
    {
        ReleaseTexture();
    }

    [ContextMenu("Generate Grass Mask")]
public void GenerateMask()
{
    ApplyDefaults();

    if (!Validate()) return;

    resolution = Mathf.Clamp(resolution, MIN_RESOLUTION, MAX_RESOLUTION);
    maskPropertyId = Shader.PropertyToID(maskPropertyName);

    Renderer target = grassRenderer != null ? grassRenderer : GetComponent<Renderer>();
    Vector2 center = arenaCenter;
    Vector2 mapSize = arenaSize;

    if (useRendererBounds && target != null)
    {
        Bounds bounds = target.bounds;
        center = new Vector2(bounds.center.x, bounds.center.z);
        mapSize = new Vector2(bounds.size.x, bounds.size.z);
    }

    if (mapSize.x <= 0f || mapSize.y <= 0f) return;

    Vector3 startPos = new Vector3(center.x - mapSize.x * 0.5f, 0f, center.y - mapSize.y * 0.5f);

    Collider[] ownColliders = target != null ? target.GetComponentsInChildren<Collider>(true) : System.Array.Empty<Collider>();
    bool[] colliderEnabled = new bool[ownColliders.Length];

    for (int i = 0; i < ownColliders.Length; i++)
    {
        colliderEnabled[i] = ownColliders[i].enabled;
        ownColliders[i].enabled = false;
    }

    int pixelCount = resolution * resolution;
    bool[] ground = new bool[pixelCount];
    bool[] obstacleMask = new bool[pixelCount]; // Mảng đánh dấu vị trí bị vật cản che

    int raycastLayers = ~ignoreLayers.value;
    int groundBits = groundLayers.value;
    int groundCount = 0;
    float lastRow = resolution - 1;

    System.Diagnostics.Stopwatch timer = System.Diagnostics.Stopwatch.StartNew();

    try
    {
        for (int z = 0; z < resolution; z++)
        {
            float normalizedZ = lastRow > 0f ? (float)z / lastRow : 0f;
            if (flipZ) normalizedZ = 1f - normalizedZ;
            float worldZ = startPos.z + normalizedZ * mapSize.y;

            for (int x = 0; x < resolution; x++)
            {
                float normalizedX = lastRow > 0f ? (float)x / lastRow : 0f;
                if (flipX) normalizedX = 1f - normalizedX;
                float worldX = startPos.x + normalizedX * mapSize.x;

                Vector3 rayOrigin = new Vector3(worldX, rayStartHeight, worldZ);
                int index = z * resolution + x;

                // Bắn raycast từ trên xuống
                if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, rayDistance, raycastLayers, QueryTriggerInteraction.Ignore))
                {
                    bool isHitGround = (groundBits & (1 << hit.collider.gameObject.layer)) != 0;

                    if (isHitGround)
                    {
                        ground[index] = true;
                        groundCount++;
                    }
                    else
                    {
                        // Nếu tia va vào vật không phải Ground (ví dụ: Đá, Cây, Nhà...)
                        // Đánh dấu đây là vật cản để loại bỏ cỏ hoàn toàn tại đây
                        obstacleMask[index] = true;
                    }
                }

#if UNITY_EDITOR
                if (!Application.isPlaying && z % PROGRESS_STEP_ROWS == 0)
                {
                    EditorUtility.DisplayProgressBar("Grass Mask Generator", $"Đang raycast hàng {z}/{resolution}", (float)z / resolution);
                }
#endif
            }
        }
    }
    finally
    {
        for (int i = 0; i < ownColliders.Length; i++)
        {
            if (ownColliders[i] != null) ownColliders[i].enabled = colliderEnabled[i];
        }

#if UNITY_EDITOR
        if (!Application.isPlaying) EditorUtility.ClearProgressBar();
#endif
    }

    timer.Stop();

    // 1. Phóng đại vùng cỏ để phủ kín viền đất
    if (paddingPixels > 0)
    {
        ground = Dilate(ground, resolution, paddingPixels, true);
    }

    // 2. MỞ RỘNG VÙNG XÓA CỎ QUANH VẬT CẢN (Dilation cho Obstacle)
    // Giúp cỏ không dính / đâm xuyên qua viền chân đá, gốc cây
    if (paddingPixels > 0)
    {
        obstacleMask = Dilate(obstacleMask, resolution, paddingPixels, true);
    }

    // 3. XÓA CỎ TẠI VỊ TRÍ VẬT CẢN (Đục lỗ mask)
    for (int i = 0; i < pixelCount; i++)
    {
        if (obstacleMask[i])
        {
            ground[i] = false; // Bắt buộc là vùng đen (không có cỏ)
        }
    }

    int visibleCount = ApplyToMaterial(ground);

    Debug.Log($"GrassMaskGenerator: {resolution}x{resolution} | Xóa cỏ thành công tại các vị trí vật cản!", this);
}

    private bool Validate()
    {
        if (grassMaterial == null)
        {
            Debug.LogError("GrassMaskGenerator: chưa gán material (Grass_Green).", this);
            return false;
        }

        if (!grassMaterial.HasProperty(maskPropertyName))
        {
            Debug.LogError("GrassMaskGenerator: material không có property '" + maskPropertyName + "'.", this);
            return false;
        }

        if (groundLayers.value == 0)
        {
            Debug.LogError("GrassMaskGenerator: groundLayers rỗng — không biết lớp nào là mặt đất.", this);
            return false;
        }

        if (arenaSize.x <= 0f || arenaSize.y <= 0f)
        {
            Debug.LogError("GrassMaskGenerator: arenaSize phải > 0.", this);
            return false;
        }

        return true;
    }

    private int ApplyToMaterial(bool[] ground)
    {
        int pixelCount = ground.Length;
        Color32[] pixels = new Color32[pixelCount];
        int visibleCount = 0;

        for (int i = 0; i < pixelCount; i++)
        {
            if (ground[i])
            {
                pixels[i] = Color.white;
                visibleCount++;
            }
            else
            {
                pixels[i] = Color.black;
            }
        }

        ReleaseTexture();

        maskTexture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false, true);
        maskTexture.name = "GrassMask_" + resolution;
        maskTexture.wrapMode = TextureWrapMode.Clamp;
        maskTexture.filterMode = FilterMode.Bilinear;
        maskTexture.anisoLevel = 0;
        maskTexture.SetPixels32(pixels);
        maskTexture.Apply(false, false);

        grassMaterial.SetTexture(maskPropertyId, maskTexture);

        return visibleCount;
    }

    private static bool[] Dilate(bool[] source, int size, int radius, bool value)
    {
        bool[] result = (bool[])source.Clone();

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                if (source[y * size + x] != value)
                {
                    continue;
                }

                int minY = Mathf.Max(0, y - radius);
                int maxY = Mathf.Min(size - 1, y + radius);
                int minX = Mathf.Max(0, x - radius);
                int maxX = Mathf.Min(size - 1, x + radius);

                for (int ny = minY; ny <= maxY; ny++)
                {
                    for (int nx = minX; nx <= maxX; nx++)
                    {
                        result[ny * size + nx] = value;
                    }
                }
            }
        }

        return result;
    }

    private void ReleaseTexture()
    {
        if (maskTexture == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(maskTexture);
        }
        else
        {
            DestroyImmediate(maskTexture);
        }

        maskTexture = null;
    }
}
