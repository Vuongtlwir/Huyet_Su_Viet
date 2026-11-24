using System.Collections;
using UnityEngine;

public class Cooldown : MonoBehaviour
{
    private void Start()
    {
        StartCoroutine(CountdownRoutine());
        StartCoroutine(TwoFrameRoutine());
    }

    private IEnumerator CountdownRoutine()
    {
        for (int i = 3; i > 0; i--)
        {
            Debug.Log($"Countdown {i} | time={Time.time:F2}");
            yield return new WaitForSeconds(1f);
        }

        Debug.Log($"Countdown done | time={Time.time:F2}");
    }

    private IEnumerator TwoFrameRoutine()
    {
        int startFrame = Time.frameCount;
        Debug.Log($"TwoFrame start | frame={startFrame}");

        yield return null;
        yield return null;

        int endFrame = Time.frameCount;
        Debug.Log($"TwoFrame end | frame={endFrame} | delta={endFrame - startFrame}");
    }
}