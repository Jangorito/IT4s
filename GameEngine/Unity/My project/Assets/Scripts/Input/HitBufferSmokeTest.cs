using UnityEngine;
using IT4s.Data;
using IT4s.Input;

public class HitBufferSmokeTest : MonoBehaviour
{
    private HitBuffer _buffer;

    void Start()
    {
        _buffer = new HitBuffer();

        _buffer.Add(new HitEvent(1000, 0, 80));
        _buffer.Add(new HitEvent(1500, 0, 90));
        _buffer.Add(new HitEvent(3000, 0, 70));

        var slice = _buffer.Slice(1200, 3200);
        Debug.Log($"HitBuffer count={_buffer.Count}, slice={slice.Count}");
        foreach (var h in slice) Debug.Log(h.ToString());
    }
}
