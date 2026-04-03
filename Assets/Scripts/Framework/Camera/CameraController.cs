using UnityEngine;

/// <summary>
/// 相机控制器
/// 挂载在相机 GameObject 上，随场景一起销毁/重建
/// </summary>
public class CameraController : MonoBehaviour
{
    #region 跟随
    [Header("跟随")]
    public Transform followTarget; // 跟随目标
    public float followSmoothSpeed = 5f; // 平滑速度，越大跟随越紧
    public Vector3 followOffset = new Vector3(0, 0, -10); // 相对目标偏移，Z=-10 为 2D 正交相机标准配置
    #endregion

    #region 冲击效果
    [Header("冲击")]
    public float impactDecaySpeed = 10f; // 衰减速度，越大震动消失越快

    private Vector3 impactOffset; // 当前偏移量，每帧向零衰减
    #endregion

    #region 螺旋效果
    [Header("螺旋")]
    public float spiralDecaySpeed = 10f; // 衰减速度，越大螺旋消失越快
    public float spiralAngularSpeed = 30f; // 角速度（ω），越大旋转越快

    // x = A * sin(ω * t), y = A * cos(ω * t)，幅度 A 每帧衰减 → 轨迹为向心螺旋
    private float spiralAmplitude; // 当前幅度（A），每帧向零衰减
    private float spiralAngle; // 当前相位角（θ）
    #endregion

    #region 推进效果
    [Header("推进")]
    public float zoomDepth = 5f; // 推进深度，越大拉近越深
    public float zoomReturnSpeed = 3f; // 回位速度，越大回位越快

    private float zoomIntensity; // 当前推进强度，每帧向零衰减
    #endregion

    #region 调试
    [Header("调试")]
    public bool showDebug = true; // 是否绘制调试图形
    private Vector3 debugDesiredPosition; // desired 位置，供 Gizmos 绘制
    #endregion

    // 使用 LateUpdate 而非 Update：确保在角色移动之后再移动相机，避免抖动
    private void LateUpdate()
    {
        if (followTarget == null) return;

        UpdateImpact(); // 更新冲击状态
        UpdateSpiral(); // 更新螺旋状态
        UpdateZoom();   // 更新推进状态

        Vector3 totalOffset = followOffset + impactOffset + GetSpiralOffset(); // 合成所有偏移
        totalOffset.z += zoomIntensity * zoomDepth; // 推进效果叠加在 Z 轴

        debugDesiredPosition = followTarget.position + totalOffset; // 计算 desired 位置
        transform.position = Vector3.Lerp(transform.position, debugDesiredPosition, followSmoothSpeed * Time.deltaTime); // 平滑跟随
    }

    // 冲击偏移量每帧向零线性插值衰减
    private void UpdateImpact()
    {
        impactOffset = Vector3.Lerp(impactOffset, Vector3.zero, impactDecaySpeed * Time.deltaTime);
    }

    // 相位角每帧累加，幅度每帧向零衰减
    private void UpdateSpiral()
    {
        spiralAngle += spiralAngularSpeed * Time.deltaTime;
        spiralAmplitude = Mathf.Lerp(spiralAmplitude, 0f, spiralDecaySpeed * Time.deltaTime);
    }

    // 根据当前相位角和幅度计算螺旋偏移量
    private Vector3 GetSpiralOffset()
    {
        return new Vector3(
            Mathf.Sin(spiralAngle) * spiralAmplitude,
            Mathf.Cos(spiralAngle) * spiralAmplitude,
            0f);
    }

    // 推进强度每帧向零线性插值衰减，实现平滑回位
    private void UpdateZoom()
    {
        zoomIntensity = Mathf.Lerp(zoomIntensity, 0f, zoomReturnSpeed * Time.deltaTime);
    }

    #region 公开 API
    /// <summary>
    /// 触发冲击效果：随机方向偏移后自然衰减
    /// </summary>
    /// <param name="intensity">震动幅度</param>
    public void Impact(float intensity = 0.3f)
    {
        impactOffset = new Vector3(
            Random.Range(-1f, 1f) * intensity,
            Random.Range(-1f, 1f) * intensity,
            0f);
    }

    /// <summary>
    /// 触发螺旋效果：三角函数旋转轨迹，幅度自然衰减呈向心螺旋
    /// </summary>
    /// <param name="intensity">螺旋幅度</param>
    public void Spiral(float intensity = 0.3f)
    {
        spiralAmplitude = intensity;
        spiralAngle = 0f;
    }

    /// <summary>
    /// 触发推进效果：瞬间拉近后平滑回位
    /// </summary>
    /// <param name="intensity">推进强度（0~1，1 为最深）</param>
    public void Zoom(float intensity = 1f)
    {
        zoomIntensity = intensity;
    }
    #endregion

    #region 调试
    private void OnDrawGizmos()
    {
        if (!showDebug) return; // 未开启调试时跳过

        // 绘制跟随目标
        if (followTarget != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(followTarget.position, 0.3f);
        }

        // 绘制相机目标位置（desired）
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(debugDesiredPosition, 0.3f);

        // 绘制相机实际位置
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, 0.2f);
    }
    #endregion
}
