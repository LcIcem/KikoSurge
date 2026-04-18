using System;
using UnityEngine;
using TMPro;
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
    [Header("交互提示（World Space Canvas 子对象）")]
    [SerializeField] private GameObject _interactionHintUI;
    [SerializeField] private TMP_Text _hintText;

    [Header("物品信息面板（World Space Canvas 子对象）")]
    [SerializeField] private GameObject _infoCardRoot;
    [SerializeField] private TMP_Text _infoCardTitle;
    [SerializeField] private TMP_Text _infoCardDescription;

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
            ShowInteractionHint(false);
    }

    protected virtual void Awake()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null && !col.isTrigger)
            col.isTrigger = true;

        if (_interactionHintUI != null)
            _interactionHintUI.SetActive(false);

        if (_infoCardRoot != null)
            _infoCardRoot.SetActive(false);
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
        ShowInteractionHint(true);
    }

    protected virtual void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        _isPlayerInRange = false;
        ShowInteractionHint(false);
        ShowInfoCard(false);
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

        ShowInteractionHint(false);
        OnInteract?.Invoke();
    }

    /// <summary>
    /// 重新显示提示（面板关闭后调用）
    /// </summary>
    public virtual void ResumePrompt()
    {
        if (_isPlayerInRange)
            ShowInteractionHint(true);
    }

    /// <summary>
    /// 显示/隐藏交互提示
    /// </summary>
    protected virtual void ShowInteractionHint(bool show)
    {
        if (_interactionHintUI != null)
            _interactionHintUI.SetActive(show);
    }

    /// <summary>
    /// 设置提示文本
    /// </summary>
    public void SetHintText(string text)
    {
        if (_hintText != null)
            _hintText.text = text;
    }

    /// <summary>
    /// 设置物品信息内容
    /// </summary>
    public void SetInfoCardContent(string title, string description)
    {
        if (_infoCardTitle != null)
            _infoCardTitle.text = title;
        if (_infoCardDescription != null)
            _infoCardDescription.text = description;
    }

    /// <summary>
    /// 显示/隐藏物品信息面板
    /// </summary>
    public void ShowInfoCard(bool show)
    {
        if (_infoCardRoot != null)
            _infoCardRoot.SetActive(show);
    }

    protected virtual void OnDestroy()
    {
        OnInteract = null;
    }
}
