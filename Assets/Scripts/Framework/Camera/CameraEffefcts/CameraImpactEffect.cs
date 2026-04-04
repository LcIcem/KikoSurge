using UnityEngine;

public enum ImpactDirection
{
    Up,
    Down,
    Left,
    Right,
    UpLeft,
    UpRight,
    DownRight,
    DownLeft,
}

/// <summary>
/// 摄像机冲击特效
/// </summary>
[System.Serializable]
public class CameraImapactEffect : ICameraEffect
{
    public float maxImpactDis = Vector3.one.magnitude; // 最大冲击偏移距离
    public float impactDecaySpeed = 10f; // 衰减速度，越大冲击效果消失越快

    private ImpactDirection dir = ImpactDirection.Up; // 冲击方向
    private float intensity; // 特效强度，每帧向零衰减

    // 特效应用的摄像机信息
    private Transform cam;


    public CameraImapactEffect(Transform cam)
    {
        this.cam = cam;
    }

    /// <summary>
    /// 更新当前 冲击强度
    /// </summary>
    public void UpdateEff()
    {
        // 每帧向零衰减
        intensity = Mathf.Lerp(intensity, 0f, impactDecaySpeed * Time.deltaTime);
    }

    /// <summary>
    /// 获取冲击特效生成的偏移点
    /// </summary>
    public Vector3 GetOffset()
    {
        Vector3 v = dir switch
        {
            ImpactDirection.Up => cam.up,
            ImpactDirection.Down => -cam.up,
            ImpactDirection.Left => -cam.right,
            ImpactDirection.Right => cam.right,
            ImpactDirection.UpLeft => (cam.up + -cam.right).normalized,
            ImpactDirection.UpRight => (cam.up + cam.right).normalized,
            ImpactDirection.DownRight => (-cam.up + cam.right).normalized,
            ImpactDirection.DownLeft => (-cam.up + -cam.right).normalized,
            _ => Vector2.zero
        };
        return v * (maxImpactDis * intensity);
    }

    /// <summary>
    /// 触发特效（随机方向）
    /// </summary>
    /// <param name="intensity">特效强度（0~1）</param>
    public void Trigger(float intensity = 0.5f)
    {
        this.intensity = Mathf.Clamp(intensity, 0f, 1f);
        dir = (ImpactDirection)Random.Range(0, System.Enum.GetValues(typeof(ImpactDirection)).Length);
    }

    /// <summary>
    /// 触发特效（指定方向）
    /// </summary>
    /// <param name="dir">冲击方向</param>
    /// <param name="intensity">特效强度（0~1）</param>
    public void TriggerDir(ImpactDirection dir, float intensity = 0.5f)
    {
        this.dir = dir;
        this.intensity = Mathf.Clamp(intensity, 0f, 1f);
    }

}