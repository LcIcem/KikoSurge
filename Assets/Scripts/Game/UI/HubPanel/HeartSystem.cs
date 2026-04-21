using System.Collections.Generic;
using LcIcemFramework.Util.Ext;
using UnityEngine.UI;
using UnityEngine;
using LcIcemFramework;
using LcIcemFramework.Core;
using Game.Event;

/// <summary>
/// 生命值系统
/// <para>负责生命值的相关数据处理</para>
/// </summary>
public class HeartSystem : MonoBehaviour
{
    [Header("心形配置")]
    [SerializeField] private int _maxColumns = 5;             // 能够显示的最大列数
    [SerializeField] private int _heartOffset = 18;     // 每颗心之间的间隔
    [SerializeField] private GameObject _heartPrefab;  // 生命Image的预设体
    [SerializeField] private RectTransform _heartContainer;  // 所有生命Image的父对象

    [SerializeField]
    [Tooltip("从低到高，生命值依次递增")]
    private List<Sprite> _heartSprites; // 所有要显示的图片（从低到高，生命值依次递增）

    private float _lastMaxHealth = -1f;   // 上一次的最大生命值

    void Awake()
    {
        // 订阅更新血量显示事件
        EventCenter.Instance.Subscribe<PlayerRuntimeData>(GameEventID.UpdateHeartDisplay, UpdateHeartDisplay);
    }

    void Start()
    {
        // 立即刷新一次显示（处理首次加载时事件已发布但订阅时序问题）
        RefreshHeartsImmediately();
    }

    /// <summary>
    /// 立即从 SessionManager 获取数据并刷新心形显示
    /// </summary>
    private void RefreshHeartsImmediately()
    {
        var playerData = SessionManager.Instance?.GetPlayerData();
        if (playerData != null)
        {
            UpdateHeartDisplay(playerData);
        }
    }

    void OnDestroy()
    {
        // 退订更新血量显示事件
        EventCenter.Instance.Unsubscribe<PlayerRuntimeData>(GameEventID.UpdateHeartDisplay, UpdateHeartDisplay);
    }

    // 更新生命值的显示
    private void UpdateHeartDisplay(PlayerRuntimeData playerData)
    {
        // 安全检查
        if (playerData == null)
            return;

        // 检查引用是否为空
        if (_heartContainer == null || _heartPrefab == null)
            return;

        // 检查最大血量是否改变
        CheckModify(playerData);

        // 安全检查
        if (_heartSprites == null || _heartSprites.Count < 5)
            return;

        int index = 0;
        float curHealth = playerData.Health;
        int lastFullHeart = Mathf.FloorToInt(curHealth); // 最后一颗完整的心的索引

        int childCount = _heartContainer.childCount;

        for (int i = 0; i < childCount; i++)
        {
            Transform heart = _heartContainer.GetChild(i);
            Image imgHeart = heart?.GetComponent<Image>();
            if (imgHeart == null)
                continue;

            if (i > lastFullHeart)
            {
                // 显示空的心
                imgHeart.sprite = _heartSprites[0];
                imgHeart.preserveAspect = true;
            }
            else if (i == lastFullHeart)
            {
                // 显示不完整的心
                float fractionalHealth = curHealth - lastFullHeart;
                int fractionIndex = Mathf.Clamp(Mathf.FloorToInt(fractionalHealth * 4), 0, 4);
                imgHeart.sprite = _heartSprites[fractionIndex];
                imgHeart.preserveAspect = true;
            }
            else
            {
                // 显示完整的心
                imgHeart.sprite = _heartSprites[4];
                imgHeart.preserveAspect = true;
            }
        }
    }

    private void CheckModify(PlayerRuntimeData playerData)
    {
        // 如果容器为空，强制重建（首次显示或旧面板销毁后）
        if (_heartContainer.childCount == 0)
        {
            RebuildHearts(playerData);
            return;
        }

        // 如果最大生命值发生改变，也需要重建
        if (_lastMaxHealth != playerData.maxHealth)
        {
            RebuildHearts(playerData);
        }
    }

    private void RebuildHearts(PlayerRuntimeData playerData)
    {
        // 先删除所有的子对象（使用 while 循环确保全部删除）
        while (_heartContainer.childCount > 0)
        {
            Transform child = _heartContainer.GetChild(0);
            if (child != null)
                DestroyImmediate(child.gameObject);
        }

        // 将 玩家实际血量 转换为 要显示的心的个数
        int maxHearts = Mathf.CeilToInt(playerData.maxHealth);

        // 创建Image
        for (var i = 0; i < maxHearts; i++)
        {
            // 实例化一个生命值Image对象 并将它设置为heartContainer的子对象
            GameObject heartObj = Instantiate(_heartPrefab, _heartContainer);
            RectTransform rectHeart = heartObj.GetComponent<RectTransform>();

            // 计算每个Image的坐标
            int x = i % _maxColumns * _heartOffset;
            int y = -i / _maxColumns * _heartOffset;

            rectHeart.anchoredPosition = new Vector2(x, y);
        }

        _lastMaxHealth = playerData.maxHealth;
    }
}
