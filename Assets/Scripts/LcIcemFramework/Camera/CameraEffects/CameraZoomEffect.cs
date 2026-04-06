using UnityEngine;

using LcIcemFramework.Util.Ext;

namespace LcIcemFramework.Camera
{
    /// <summary>
    /// 摄像机推进特效
    /// </summary>
    [System.Serializable]
    public class CameraZoomEffect : ICameraEffect
    {
        public float maxZoomDepth = 5f; // 最大推进深度
        public float zoomReturnSpeed = 3f; // 回位速度，越大回位越快

        private float _intensity; // 当前推进强度，每帧向零衰减
        private bool _isTriggered; // 特效是否触发标识
        private Transform _cam; // 相机引用

        public CameraZoomEffect(Transform cam)
        {
            _cam = cam;
        }

        /// <summary>
        /// 更新推进强度
        /// </summary>
        public void UpdateEff()
        {
            if (!_isTriggered) return;

            // 进强度每帧向零线性插值衰减，实现平滑回位
            _intensity = Mathf.Lerp(_intensity, 0, zoomReturnSpeed * Time.deltaTime);
            if (_intensity.IsEqualsTo(0f, 1e-4f))
                _isTriggered = false;
        }

        /// <summary>
        /// 获取特效偏移值
        /// </summary>
        public Vector3 GetOffset()
        {
            if (!_isTriggered) return Vector3.zero;
            return _cam.forward * _intensity * maxZoomDepth;
        }

        /// <summary>
        /// 触发特效
        /// </summary>
        /// <param name="intensity">特效强度（0~1）</param>
        public void Trigger(float intensity = 0.5f)
        {
            _intensity = Mathf.Clamp(intensity, 0, 1);
            _isTriggered = true;
        }
    }
}
