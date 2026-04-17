using System;
using LcIcemFramework;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 检查点交互脚本
/// <para>放置在检查点预设体上，自身负责按键检测和交互触发</para>
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class Checkpoint : MonoBehaviour
{
    [Header("交互提示UI（可选）")]
    [SerializeField] private GameObject _promptUI;

    /// <summary>
    /// 检查点激活时触发
    /// </summary>
    public event Action OnCheckpointActivated;

    /// <summary>
    /// 玩家是否在交互范围内且检查点未激活
    /// </summary>
    public bool CanInteract => _isPlayerInRange && !_isActivated;

    /// <summary>
    /// 检查点是否已被激活
    /// </summary>
    public bool IsActivated => _isActivated;

    private bool _isPlayerInRange;
    private bool _isActivated;
    private InputAction _interactAction;

    private void Awake()
    {
        // 确保碰撞体是触发器
        Collider2D col = GetComponent<Collider2D>();
        if (col != null && !col.isTrigger)
        {
            col.isTrigger = true;
        }

        // 初始隐藏提示UI
        if (_promptUI != null)
            _promptUI.SetActive(false);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        _isPlayerInRange = true;
        ShowPrompt(true);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        _isPlayerInRange = false;
        ShowPrompt(false);
    }

    private void Update()
    {
        // 通过 InputManager 获取 interact Action（Playermap）
        if (_interactAction == null)
        {
            _interactAction = ManagerHub.Input?.GetInputActionFromMap("Player", "Interact");
        }

        if (_interactAction != null && _interactAction.WasPressedThisFrame() && CanInteract)
        {
            Interact();
        }
    }

    /// <summary>
    /// 执行检查点交互
    /// </summary>
    public void Interact()
    {
        if (!CanInteract)
            return;

        _isActivated = true;
        ShowPrompt(false);

        Debug.Log($"[Checkpoint] 激活检查点: {gameObject.name}");

        OnCheckpointActivated?.Invoke();
    }

    private void ShowPrompt(bool show)
    {
        if (_promptUI != null)
            _promptUI.SetActive(show);
    }

    private void OnDestroy()
    {
        OnCheckpointActivated = null;
    }
}