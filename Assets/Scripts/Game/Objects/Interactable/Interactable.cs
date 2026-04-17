using System;
using UnityEngine;
using UnityEngine.InputSystem;
using LcIcemFramework;
using LcIcemFramework.Core;

/// <summary>
/// 可交互物体基组件
/// <para>挂载此组件代表物体具备交互功能，检测玩家靠近和按键触发</para>
/// <para>子类通过订阅 OnInteract 事件处理具体交互逻辑</para>
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Interactable : MonoBehaviour
{
    /// <summary>
    /// 交互提示 UI（World Space Canvas 子对象）
    /// </summary>
    [SerializeField] protected GameObject _promptUI;

    /// <summary>
    /// 交互触发时调用
    /// </summary>
    public event Action OnInteract;

    /// <summary>
    /// 玩家是否在交互范围内且交互未被禁用
    /// </summary>
    public bool CanInteract => _isPlayerInRange && _interactionEnabled;

    private bool _isPlayerInRange;
    private bool _interactionEnabled = true;
    private InputAction _interactAction;

    /// <summary>
    /// 设置交互是否启用
    /// </summary>
    public void SetInteractionEnabled(bool enabled)
    {
        _interactionEnabled = enabled;
        if (!enabled)
            ShowPrompt(false);
    }

    protected virtual void Awake()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null && !col.isTrigger)
            col.isTrigger = true;

        if (_promptUI != null)
            _promptUI.SetActive(false);
    }

    protected virtual void OnEnable()
    {
        _interactAction = ManagerHub.Input?.GetInputActionFromMap("Player", "Interact");
        if (_interactAction != null)
            _interactAction.performed += OnInteractPerformed;
    }

    protected virtual void OnDisable()
    {
        if (_interactAction != null)
            _interactAction.performed -= OnInteractPerformed;
    }

    protected virtual void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        _isPlayerInRange = true;
        ShowPrompt(true);
    }

    protected virtual void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        _isPlayerInRange = false;
        ShowPrompt(false);
    }

    private void OnInteractPerformed(InputAction.CallbackContext ctx)
    {
        if (CanInteract)
            Interact();
    }

    /// <summary>
    /// 执行交互
    /// </summary>
    public virtual void Interact()
    {
        if (!CanInteract)
            return;

        Debug.Log($"[Interactable] 触发交互: {gameObject.name}");
        ShowPrompt(false);
        OnInteract?.Invoke();
    }

    /// <summary>
    /// 重新显示提示（面板关闭后调用）
    /// </summary>
    public virtual void ResumePrompt()
    {
        if (_isPlayerInRange)
            ShowPrompt(true);
    }

    /// <summary>
    /// 显示/隐藏提示
    /// </summary>
    protected virtual void ShowPrompt(bool show)
    {
        if (_promptUI != null)
            _promptUI.SetActive(show);
    }

    protected virtual void OnDestroy()
    {
        OnInteract = null;
    }
}
