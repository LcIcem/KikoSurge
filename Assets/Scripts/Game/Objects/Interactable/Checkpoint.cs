using System;
using LcIcemFramework;
using UnityEngine;

/// <summary>
/// 检查点交互脚本
/// <para>挂载在与 Interactable 组件相同的 GameObject 上</para>
/// </summary>
public class Checkpoint : MonoBehaviour
{
    [SerializeField] private Interactable _interactable;

    /// <summary>
    /// 检查点激活时触发
    /// </summary>
    public event Action OnCheckpointActivated;

    /// <summary>
    /// 检查点是否已被激活
    /// </summary>
    public bool IsActivated => _isActivated;

    private bool _isActivated;

    private void Start()
    {
        _interactable.OnInteract += OnInteractTriggered;
    }

    private void OnInteractTriggered()
    {
        if (_isActivated)
            return;

        _isActivated = true;
        _interactable.SetInteractionEnabled(false);

        Debug.Log($"[Checkpoint] 激活检查点: {gameObject.name}");

        OnCheckpointActivated?.Invoke();
    }

    private void OnDestroy()
    {
        OnCheckpointActivated = null;
    }
}
