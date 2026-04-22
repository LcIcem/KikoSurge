using System;
using Game.Config.Shop;
using Game.Manager;
using LcIcemFramework;
using ProcGen.Seed;
using UnityEngine;

/// <summary>
/// 商人交互脚本
/// <para>通过获取 Interactable 组件实现交互，交互后打开商店面板</para>
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class ShopkeeperInteractable : MonoBehaviour, ISpawnable
{
    [Header("商店配置")]
    [SerializeField] private ShopConfig _shopConfig;

    private Interactable _interactable;
    private GameRandom _rng;
    private bool _itemsGenerated;

    private void Awake()
    {
        _interactable = GetComponent<Interactable>();
        if (_interactable == null)
        {
            Debug.LogError($"[ShopkeeperInteractable] Interactable component not found on {gameObject.name}");
            return;
        }
    }

    private void Start()
    {
        // 初始化交互提示文本
        _interactable.SetHintText("与商人交谈[{0}]");

        // 设置信息卡片
        if (!string.IsNullOrEmpty(GetShopName()))
        {
            _interactable.SetInfoCardContent(GetShopName(), "点击打开商店");
        }

        // 订阅交互事件
        _interactable.OnInteract += OnShopInteract;
    }

    private void OnEnable()
    {
        if (_interactable != null)
            _interactable.OnInteract += OnShopInteract;
    }

    private void OnDisable()
    {
        if (_interactable != null)
            _interactable.OnInteract -= OnShopInteract;
    }

    private void OnDestroy()
    {
        if (_interactable != null)
            _interactable.OnInteract -= OnShopInteract;
    }

    /// <summary>
    /// ISpawnable 接口实现：接收随机数生成器
    /// </summary>
    public void SetRng(GameRandom rng)
    {
        _rng = rng;
    }

    /// <summary>
    /// 房间首次激活时调用（由 RoomController 或 DungeonDataManager 调用）
    /// </summary>
    public void OnRoomFirstActivated()
    {
        if (_itemsGenerated) return;

        if (_shopConfig == null)
        {
            LogWarning("ShopConfig 未配置");
            return;
        }

        if (_rng == null)
        {
            LogWarning("GameRandom 未设置，使用临时随机");
            _rng = new GameRandom(DateTime.Now.Ticks);
        }

        _itemsGenerated = true;
        GenerateShopItems();
    }

    /// <summary>
    /// 生成商店商品
    /// </summary>
    private void GenerateShopItems()
    {
        if (_shopConfig == null || _rng == null) return;
        ShopManager.Instance.RefreshShopItems(_shopConfig, _rng);
    }

    /// <summary>
    /// 商人交互回调
    /// </summary>
    private void OnShopInteract()
    {
        // 确保商品已生成（可能在房间激活时未调用）
        if (!_itemsGenerated)
        {
            OnRoomFirstActivated();
        }

        // 打开商店面板
        OpenShopPanel();

        // 禁用交互（关闭面板后重新启用）
        _interactable.SetInteractionEnabled(false);
    }

    /// <summary>
    /// 打开商店面板
    /// </summary>
    private void OpenShopPanel()
    {
        // 通过 GameLifecycleManager 打开商店（会切换到 Interacting 状态）
        GameLifecycleManager.Instance.OpenShop((panel) =>
        {
            if (panel != null)
            {
                panel.OnShopClosed += OnShopClosed;
            }
        });
    }

    /// <summary>
    /// 商店面板关闭回调
    /// </summary>
    private void OnShopClosed()
    {
        // 重新启用交互
        if (_interactable != null)
        {
            _interactable.SetInteractionEnabled(true);
            _interactable.ResetInteractionState();
        }
    }

    /// <summary>
    /// 获取商店名称
    /// </summary>
    private string GetShopName()
    {
        return _shopConfig != null ? _shopConfig.name : "商店";
    }

    /// <summary>
    /// 重置商人状态（对象池复用时调用）
    /// </summary>
    public void ResetShopkeeper()
    {
        _itemsGenerated = false;
        if (_interactable != null)
        {
            _interactable.SetInteractionEnabled(true);
            _interactable.ResetInteractionState();
        }
    }

    private void Log(string msg) => Debug.Log($"[ShopkeeperInteractable] {msg}");
    private void LogWarning(string msg) => Debug.LogWarning($"[ShopkeeperInteractable] {msg}");
}
