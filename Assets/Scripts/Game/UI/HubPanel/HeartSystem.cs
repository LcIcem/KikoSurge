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

    void Start()
    {
        // 订阅更新血量显示事件
        EventCenter.Instance.Subscribe<PlayerRuntimeData>(GameEventID.UpdateHeartDisplay, UpdateHeartDisplay);
    }

    void OnDestroy()
    {
        // 退订更新血量显示事件
        EventCenter.Instance.Unsubscribe<PlayerRuntimeData>(GameEventID.UpdateHeartDisplay, UpdateHeartDisplay);
    }

    // 更新生命值的显示
    private void UpdateHeartDisplay(PlayerRuntimeData playerData)
    {
        // 检查最大血量是否改变
        CheckModify(playerData);

        int index = 0;
        float curHealth = playerData.Health;
        int lastFullHeart = Mathf.FloorToInt(curHealth); // 最后一颗完整的心的索引

        foreach (Transform heart in _heartContainer)
        {
            Image imgHeart = heart.GetComponent<Image>();

            if (index > lastFullHeart)
            {
                // 显示空的心
                imgHeart.sprite = _heartSprites[0];
            }
            else if (index == lastFullHeart)
            {
                // 显示不完整的心
                // 除去完整的生命值 后 剩余的生命值
                float fractionalHealth = curHealth - lastFullHeart;
                int fractionIndex = Mathf.FloorToInt(fractionalHealth * 4); // 将剩余的生命值 分别映射为 0~3 的索引值

                imgHeart.sprite = _heartSprites[fractionIndex];
            }
            else
            {
                // 显示完整的心   
                imgHeart.sprite = _heartSprites[4];
            }

            index++;
        }
    }

    private void CheckModify(PlayerRuntimeData playerData)
    {
        // 如果最大生命值发生改变 更新血量UI
        if (_lastMaxHealth != playerData.maxHealth)
        {
            // 先删除之前的
            foreach (Transform child in _heartContainer)
            {
                Destroy(child.gameObject);
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
}
