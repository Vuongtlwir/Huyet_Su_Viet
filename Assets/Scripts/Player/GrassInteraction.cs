using UnityEngine;

public class GrassInteraction : MonoBehaviour
{
    [Header("Material")]
    [SerializeField] private Material grassMaterial;
    [SerializeField] private string positionProperty = "_PlayerPosition";
    [SerializeField] private string radiusProperty = "_BendRadius";
    [SerializeField] private string trailProperty = "_TrailPositions";

    [Header("Bend")]
    [SerializeField] private float bendRadius = 1.5f;

    [Header("Trail")]
    [SerializeField] private float trailSpacing = 0.4f;
    [SerializeField] private float trailLifetime = 1f;

    [Header("Misc")]
    [SerializeField] private float moveThreshold = 0.01f;

    private const int TRAIL_SAMPLE_COUNT = 8;

    private static readonly Vector4 HIDDEN_POSITION = new Vector4(0f, -10000f, 0f, 0f);
    private static readonly Vector4 CLEARED_TRAIL = Vector4.zero;

    private int positionId;
    private int radiusId;
    private int trailId;

    private Vector4[] trailPositions;
    private Vector4[] trailBuffer;
    private float[] trailAges;

    private Vector3 lastSamplePosition;
    private Vector3 lastLivePosition;
    private float lastRadius = -1f;
    private bool hasSample;
    private bool hasLive;

    private void Awake()
    {
        positionId = Shader.PropertyToID(positionProperty);
        radiusId = Shader.PropertyToID(radiusProperty);
        trailId = Shader.PropertyToID(trailProperty);

        trailPositions = new Vector4[TRAIL_SAMPLE_COUNT];
        trailBuffer = new Vector4[TRAIL_SAMPLE_COUNT];
        trailAges = new float[TRAIL_SAMPLE_COUNT];

        for (int i = 0; i < TRAIL_SAMPLE_COUNT; i++)
        {
            trailAges[i] = float.MaxValue;
        }
    }

    private void Update()
    {
        if (grassMaterial == null)
        {
            return;
        }

        Vector3 position = transform.position;

        if (!hasLive || (position - lastLivePosition).sqrMagnitude > moveThreshold * moveThreshold || !Mathf.Approximately(bendRadius, lastRadius))
        {
            grassMaterial.SetVector(positionId, new Vector4(position.x, position.y, position.z, 0f));
            grassMaterial.SetFloat(radiusId, bendRadius);
            lastLivePosition = position;
            lastRadius = bendRadius;
            hasLive = true;
        }

        if (!hasSample || (position - lastSamplePosition).sqrMagnitude >= trailSpacing * trailSpacing)
        {
            PushSample(position);
            lastSamplePosition = position;
            hasSample = true;
        }

        UpdateTrail(Time.deltaTime);
    }

    private void OnDisable()
    {
        if (grassMaterial == null)
        {
            return;
        }

        grassMaterial.SetVector(positionId, HIDDEN_POSITION);
        ClearTrail();
        hasLive = false;
        hasSample = false;
    }

    private void PushSample(Vector3 position)
    {
        for (int i = TRAIL_SAMPLE_COUNT - 1; i > 0; i--)
        {
            trailPositions[i] = trailPositions[i - 1];
            trailAges[i] = trailAges[i - 1];
        }

        trailPositions[0] = new Vector4(position.x, position.y, position.z, 1f);
        trailAges[0] = 0f;
    }

    private void UpdateTrail(float deltaTime)
    {
        for (int i = 0; i < TRAIL_SAMPLE_COUNT; i++)
        {
            trailAges[i] += deltaTime;

            float strength = 1f - trailAges[i] / trailLifetime;

            if (strength < 0f)
            {
                strength = 0f;
            }

            strength *= strength;

            Vector4 position = trailPositions[i];
            trailBuffer[i] = new Vector4(position.x, position.y, position.z, strength);
        }

        grassMaterial.SetVectorArray(trailId, trailBuffer);
    }

    private void ClearTrail()
    {
        for (int i = 0; i < TRAIL_SAMPLE_COUNT; i++)
        {
            trailBuffer[i] = CLEARED_TRAIL;
            trailAges[i] = float.MaxValue;
        }

        grassMaterial.SetVectorArray(trailId, trailBuffer);
    }
}
