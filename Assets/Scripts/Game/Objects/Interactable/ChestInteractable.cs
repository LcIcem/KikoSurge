using System;
using UnityEngine;

/// <summary>
/// 宝箱交互脚本
/// <para>通过获取 Interactable 组件实现交互，交互后播放开启动画并掉落物品</para>
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class ChestInteractable : MonoBehaviour
{
    [Header("宝箱配置")]
    [SerializeField] private ChestConfig _chestConfig;

    [Header("动画状态机")]
    [SerializeField] private Animator _animator;
    private static readonly int k_OpenTrigger = Animator.StringToHash("open");

    private Interactable _interactable;
    private bool _isOpened;

    public ChestConfig ChestConfig => _chestConfig;
    public bool IsOpened => _isOpened;

    private void Awake()
    {
        _interactable = GetComponent<Interactable>();
        if (_interactable == null)
        {
            Debug.LogError($"[ChestInteractable] Interactable component not found on {gameObject.name}");
            return;
        }

        if (_animator == null)
            _animator = GetComponent<Animator>();
    }

    private void Start()
    {
        // 初始化交互提示文本
        _interactable.SetHintText("开启[{0}]");

        // 如果配置了标题，设置信息卡片
        if (_chestConfig != null && !string.IsNullOrEmpty(_chestConfig.ChestName))
        {
            _interactable.SetInfoCardContent(_chestConfig.ChestName, "点击开启宝箱");
        }

        // 订阅交互事件
        _interactable.OnInteract += OnChestInteract;
    }

    private void OnEnable()
    {
        if (_interactable != null)
            _interactable.OnInteract += OnChestInteract;
    }

    private void OnDisable()
    {
        if (_interactable != null)
            _interactable.OnInteract -= OnChestInteract;
    }

    private void OnDestroy()
    {
        if (_interactable != null)
            _interactable.OnInteract -= OnChestInteract;
    }

    /// <summary>
    /// 宝箱交互回调
    /// </summary>
    private void OnChestInteract()
    {
        if (_isOpened)
            return;

        _isOpened = true;
        _interactable.SetInteractionEnabled(false);

        // 播放开启动画
        if (_animator != null)
        {
            _animator.SetTrigger(k_OpenTrigger);
        }
        else
        {
            // 没有动画时直接触发掉落
            OnLootAnimationComplete();
        }
    }

    /// <summary>
    /// 动画播放完毕后调用（通过动画事件绑定）
    /// </summary>
    public void OnLootAnimationComplete()
    {
        if (_chestConfig == null || _chestConfig.LootTable == null)
        {
            LogWarning("宝箱配置或掉落表为空");
            return;
        }

        // 调用 LootManager 处理掉落
        LootManager.Instance.ProcessChestLoot(_chestConfig, transform.position);
    }

    /// <summary>
    /// 重置宝箱状态（对象池复用时调用）
    /// </summary>
    public void ResetChest()
    {
        _isOpened = false;
        _interactable.SetInteractionEnabled(true);
        _interactable.ResetInteractionState();

        // 重置动画状态（如果Animator支持）
        if (_animator != null)
        {
            _animator.Rebind();
        }
    }

    private void Log(string msg) => Debug.Log($"[ChestInteractable] {msg}");
    private void LogWarning(string msg) => Debug.LogWarning($"[ChestInteractable] {msg}");
}
