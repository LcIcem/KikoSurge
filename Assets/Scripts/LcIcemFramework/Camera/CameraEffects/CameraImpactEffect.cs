using UnityEngine;

using LcIcemFramework.Util.Ext;

namespace LcIcemFramework.Camera
{
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
    public class CameraImpactEffect : ICameraEffect
    {
        public float maxImpactDis = Vector3.one.magnitude; // 最大冲击偏移距离
        public float impactDecaySpeed = 10f; // 衰减速度，越大冲击效果消失越快

        private ImpactDirection _dir = ImpactDirection.Up; // 冲击方向
        private float _intensity; // 特效强度，每帧向零衰减
        private bool _isTriggered; // 特效是否触发标识

        // 特效应用的摄像机信息
        private Transform _cam;

        public CameraImpactEffect(Transform cam)
        {
            _cam = cam;
        }

        /// <summary>
        /// 更新当前 冲击强度
        /// </summary>
        public void UpdateEff()
        {
            if (!_isTriggered) return;

            // 每帧向零衰减
            _intensity = Mathf.Lerp(_intensity, 0f, impactDecaySpeed * Time.deltaTime);
            if (_intensity.IsEqualsTo(0f, 1e-4f))
                _isTriggered = false;
        }

        /// <summary>
        /// 获取冲击特效生成的偏移点
        /// </summary>
        public Vector3 GetOffset()
        {
            if (!_isTriggered) return Vector3.zero;

            Vector3 v = _dir switch
            {
                ImpactDirection.Up => _cam.up,
                ImpactDirection.Down => -_cam.up,
                ImpactDirection.Left => -_cam.right,
                ImpactDirection.Right => _cam.right,
                ImpactDirection.UpLeft => (_cam.up + -_cam.right).normalized,
                ImpactDirection.UpRight => (_cam.up + _cam.right).normalized,
                ImpactDirection.DownRight => (-_cam.up + _cam.right).normalized,
                ImpactDirection.DownLeft => (-_cam.up + -_cam.right).normalized,
                _ => Vector2.zero
            };
            return v * (maxImpactDis * _intensity);
        }

        /// <summary>
        /// 触发特效（随机方向）
        /// </summary>
        /// <param name="intensity">特效强度（0~1）</param>
        public void Trigger(float intensity = 0.5f)
        {
            _intensity = Mathf.Clamp(intensity, 0f, 1f);
            _dir = (ImpactDirection)Random.Range(0, System.Enum.GetValues(typeof(ImpactDirection)).Length);
            _isTriggered = true;
        }

        /// <summary>
        /// 触发特效（指定方向）
        /// </summary>
        /// <param name="dir">冲击方向</param>
        /// <param name="intensity">特效强度（0~1）</param>
        public void TriggerDir(ImpactDirection dir, float intensity = 0.5f)
        {
            _dir = dir;
            _intensity = Mathf.Clamp(intensity, 0f, 1f);
            _isTriggered = true;
        }
    }
}
