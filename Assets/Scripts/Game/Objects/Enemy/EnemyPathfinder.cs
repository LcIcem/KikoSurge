using System.Collections;
using Pathfinding;
using UnityEngine;

/// <summary>
/// 封装 A* Seeker 寻路，自己控制移动。
/// 官方纯 Seeker 用法：StartPath → 回调收到路径 → 自己沿 vectorPath 移动。
/// </summary>
[RequireComponent(typeof(Seeker))]
public class EnemyPathfinder : MonoBehaviour
{
    private Seeker _seeker;
    private Rigidbody2D _rigidbody;
    private Path _path; // 当前路径
    private int _waypointIndex = 0; // 路径点索引

    [SerializeField] private float _speed = 3f; // 移动速度
    [SerializeField] private float _nextWaypointThreshold = 0.2f; // 到达下一个点的阈值

    private Vector3 _target = Vector3.zero; // 目标位置
    private Vector3 _preTarget = Vector3.zero; // 上一次目标位置
 
    // 是否正在移动
    public bool IsMoving => _path != null && _waypointIndex < _path.vectorPath.Count;

    protected void Awake()
    {
        _seeker = GetComponent<Seeker>();
        _rigidbody = GetComponent<Rigidbody2D>();
    }

    protected void FixedUpdate()
    {
        if (_path == null || _waypointIndex >= _path.vectorPath.Count)
            return;

        // 当前寻路节点
        Vector2 waypoint = _path.vectorPath[_waypointIndex];

        // 得到当前位置到寻路节点的方向向量
        Vector2 dir = (waypoint - _rigidbody.position).normalized;
        // 根据方向向量开始移动
        _rigidbody.MovePosition(_rigidbody.position + dir * _speed * Time.fixedDeltaTime);

        // 到达阈值，进入下一个点
        if (Vector2.Distance(_rigidbody.position, waypoint) < _nextWaypointThreshold)
            _waypointIndex++;
    }

    // 设置寻路目标
    private void SetTarget(Transform target)
    {
        if (target == null) return;

        _preTarget = _target;
        _target = target.position;
    }

    // 开始移动 -发起寻路请求
    public void StartMoveTo(Transform target)
    {
        SetTarget(target);

        // 当目标位置改变时 才重新生成寻路路径
        if (_preTarget != _target)
        {
            _seeker.StartPath(transform.position, _target, OnPathComplete);
        }
    }

    // 路径计算完成回调
    private void OnPathComplete(Path p)
    {
        if (p.error) return;

        _path = p;
        _waypointIndex = 0;
    }

    // 停止移动
    public void StopMove()
    {
        _path = null;
        _waypointIndex = 0;
    }

    // 设置移动速度
    public void SetSpeed(float speed)
    {
        _speed = speed;
    }
}
