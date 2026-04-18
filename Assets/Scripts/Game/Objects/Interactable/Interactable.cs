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
    private string _hintTemplate = "";

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

        // 如果 _hintText 有值但 _hintTemplate 为空，用 _hintText 的内容初始化 _hintTemplate
        if (_hintText != null && string.IsNullOrEmpty(_hintTemplate))
        {
            string existingText = _hintText.text;
            if (!string.IsNullOrEmpty(existingText))
            {
                _hintTemplate = existingText;
            }
        }
    }

    protected virtual void Start()
    {
        // 初始化按键文本显示
        if (!string.IsNullOrEmpty(_hintTemplate))
            UpdateHintTextWithKey();
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

        // 如果玩家正在交互中，不显示交互提示
        if (Player.IsInteracting)
            return;

        // 标记玩家开始交互，防止其他重叠物品显示UI
        Player.StartInteraction();

        ShowInteractionHint(true);
        ShowInfoCard(true);
    }

    protected virtual void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        _isPlayerInRange = false;
        ShowInteractionHint(false);
        ShowInfoCard(false);

        // 玩家离开交互范围时，结束交互状态
        if (Player.IsInteracting)
            Player.EndInteraction();
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
        ShowInfoCard(false);
        OnInteract?.Invoke();
    }

    /// <summary>
    /// 重新显示提示（面板关闭后调用）
    /// </summary>
    public virtual void ResumePrompt()
    {
        if (_isPlayerInRange)
        {
            ShowInteractionHint(true);
        }
    }

    /// <summary>
    /// 显示/隐藏交互提示
    /// </summary>
    protected virtual void ShowInteractionHint(bool show)
    {
        if (_interactionHintUI != null)
            _interactionHintUI.SetActive(show);

        if (show && !string.IsNullOrEmpty(_hintTemplate))
            UpdateHintTextWithKey();
    }

    /// <summary>
    /// 设置提示文本（支持 {0} 作为按键占位符）
    /// </summary>
    public void SetHintText(string text)
    {
        _hintTemplate = text;
        UpdateHintTextWithKey();
    }

    /// <summary>
    /// 更新提示文本，将 {0} 替换为实际的交互按键
    /// </summary>
    private void UpdateHintTextWithKey()
    {
        if (_hintText == null)
            return;

        string keyName = GetInteractKeyName();
        string displayText = string.Format(_hintTemplate, keyName);
        _hintText.text = displayText;
    }

    /// <summary>
    /// 获取交互按键的显示名称
    /// </summary>
    private string GetInteractKeyName()
    {
        // 每次都重新获取，确保获取最新的键位配置
        var interactAction = ManagerHub.Input?.GetInputActionFromMap("Player", "Interact");
        if (interactAction != null && interactAction.bindings.Count > 0)
        {
            // 遍历找到第一个非 composite 的有效绑定
            for (int i = 0; i < interactAction.bindings.Count; i++)
            {
                var binding = interactAction.bindings[i];
                // 跳过 composite 父级和 partOfComposite
                if (binding.isComposite || binding.isPartOfComposite)
                    continue;

                if (!string.IsNullOrEmpty(binding.overridePath))
                {
                    return GetDisplayNameFromPath(binding.overridePath);
                }
                return InputActionRebindingExtensions.GetBindingDisplayString(interactAction, i, default);
            }
        }
        return "E";
    }

    /// <summary>
    /// 从路径获取按键显示名称
    /// </summary>
    private string GetDisplayNameFromPath(string path)
    {
        if (string.IsNullOrEmpty(path)) return "None";

        int slashIndex = path.LastIndexOf('/');
        if (slashIndex >= 0 && slashIndex < path.Length - 1)
        {
            string key = path.Substring(slashIndex + 1);

            if (key == "leftButton") return "LMB";
            if (key == "rightButton") return "RMB";
            if (key == "middleButton") return "MMB";
            if (key == "shift") return "Shift";
            if (key == "ctrl") return "Ctrl";
            if (key == "alt") return "Alt";

            if (key.Length == 1)
                return key.ToUpper();
            return char.ToUpper(key[0]) + key.Substring(1).ToLower();
        }
        return path;
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
