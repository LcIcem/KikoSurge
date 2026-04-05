using UnityEngine;

/// <summary>
/// 摄像机螺旋向心特效
/// </summary>
[System.Serializable]
public class CameraSpiralEffect : ICameraEffect
{
    public float maxAmp = 10f; // 最大螺旋振幅
    public float ampDecaySpeed = 10f; // 衰减速度，越大螺旋消失越快
    public float angularSpeed = 30f; // 角速度（ω），越大旋转越快

    private float _ampIntensity; // 当前幅度强度，每帧向零衰减
    private float _totalTime; // 效果累计总时间(t)
    private bool _isTriggered; // 特效是否触发标识
    private Transform _cam; // 相机引用，用于基于相机坐标计算偏移

    // x = A * sin(ω * t), y = A * cos(ω * t)，幅度 A 每帧衰减 → 轨迹为向心螺旋
    // 振幅（A）等于 强度 * 最大振幅

    public CameraSpiralEffect(Transform cam)
    {
        _cam = cam;
    }


    /// <summary>
    /// 更新当前 相位角 和 振幅强度
    /// </summary>
    public void UpdateEff()
    {
        if (!_isTriggered) return;
        
        // 相位角每帧累加，幅度每帧向零衰减
        _totalTime += Time.deltaTime;
        _ampIntensity = Mathf.Lerp(_ampIntensity, 0f, ampDecaySpeed * Time.deltaTime);
        if (_ampIntensity.IsEqualsTo(0f, 1e-4f))
            _isTriggered = false;
    }

    /// <summary>
    /// 获取螺旋特效生成的偏移点，基于相机坐标旋转
    /// </summary>
    public Vector3 GetOffset()
    {
        Vector2 osc = MathUtil.GetXYOscPoint(_totalTime, _ampIntensity * maxAmp, angularSpeed, Mathf.PI / 2);                                                                          
        // 相机局部 XOY 平面的 2D 震荡映射到世界坐标
        return _cam.right * osc.x + _cam.up * osc.y;
    }

    /// <summary>
    /// 触发特效
    /// </summary>
    /// <param name="intensity">特效强度（0~1）</param>
    public void Trigger(float intensity = 0.5f)
    {
        _ampIntensity = Mathf.Clamp(intensity, 0f, 1f);
        _totalTime = 0f;
        _isTriggered = true;
    }

}