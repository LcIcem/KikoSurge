using System;
using LcIcemFramework;
using UnityEngine;

/// <summary>
/// 终点检查点交互脚本
/// <para>挂载在终点房间的检查点上，与 Interactable 组件配合使用</para>
/// <para>功能：交互后保存 checkpoint，然后进入下一层（或通关）</para>
/// <para>注意：此checkpoint仅用于「中途退出后继续」，死亡后session结束，不会从checkpoint复活</para>
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
        _interactable.SetHintText("按[{0}]激活检查点");
        _interactable.OnInteract += OnInteractTriggered;
    }

    private void OnInteractTriggered()
    {
        if (_isActivated)
            return;

        _isActivated = true;
        _interactable.SetInteractionEnabled(false);
        _interactable.ShowInfoCard(false);

        OnCheckpointActivated?.Invoke();
    }

    private void OnDestroy()
    {
        OnCheckpointActivated = null;
    }
}
