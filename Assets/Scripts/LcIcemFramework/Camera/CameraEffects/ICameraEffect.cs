using UnityEngine;

namespace LcIcemFramework.Camera
{
/// <summary>
/// 摄像机特效接口
/// </summary>
public interface ICameraEffect
{
    // 更新特效数据
    void UpdateEff();

    // 获取特效产生的偏移位置
    Vector3 GetOffset();

    // 触发特效
    void Trigger(float intensity);
}
}