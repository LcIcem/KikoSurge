using UnityEngine;
using LcIcemFramework;
using Game.Event;
using LcIcemFramework.Core;

/// <summary>
/// 伤害数字飘字管理器
/// 完全动态创建，不依赖预设置的 Canvas 或根节点
/// </summary>
public class DamageNumberManager : SingletonMono<DamageNumberManager>
{
    [Header("配置")]
    [Tooltip("伤害飘字预制体，需包含 DamageNumberUI 组件")]
    [SerializeField] private GameObject _damageNumberPrefab;

    [Header("飘字偏移配置")]
    [Tooltip("随机偏移范围 X（像素）")]
    [SerializeField] private float _randomOffsetX = 30f;
    [Tooltip("随机偏移范围 Y（像素）")]
    [SerializeField] private float _randomOffsetY = 20f;

    private Camera _worldCamera;
    private Canvas _uiCanvas;
    private RectTransform _damageNumberRoot;
    private RectTransform _canvasRect;

    protected override void Init()
    {
        // 相机在 LateUpdate 中动态查找
        SubscribeEvents();
    }

    private void LateUpdate()
    {
        if (_worldCamera == null)
            _worldCamera = Camera.main;

        // 如果还没找到 Canvas，每帧尝试找一次
        if (_uiCanvas == null)
        {
            _uiCanvas = FindObjectOfType<Canvas>();
            if (_uiCanvas != null)
                _canvasRect = _uiCanvas.GetComponent<RectTransform>();
        }

        // 如果还没创建根节点，在找到 Canvas 后创建
        if (_damageNumberRoot == null && _uiCanvas != null)
        {
            CreateDamageNumberRoot();
        }
    }

    private void CreateDamageNumberRoot()
    {
        if (_uiCanvas == null) return;

        GameObject rootObj = new GameObject("DamageNumberRoot");
        rootObj.transform.SetParent(_uiCanvas.transform);
        _damageNumberRoot = rootObj.AddComponent<RectTransform>();
        _damageNumberRoot.anchorMin = Vector2.zero;
        _damageNumberRoot.anchorMax = Vector2.one;
        _damageNumberRoot.sizeDelta = Vector2.zero;
        _damageNumberRoot.anchoredPosition = Vector2.zero;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        UnsubscribeEvents();
    }

    private void SubscribeEvents()
    {
        EventCenter.Instance.Subscribe<DamageNumberParams>(GameEventID.Combat_ShowDamageNumber, OnShowDamageNumber);
    }

    private void UnsubscribeEvents()
    {
        EventCenter.Instance.Unsubscribe<DamageNumberParams>(GameEventID.Combat_ShowDamageNumber, OnShowDamageNumber);
    }

    private void OnShowDamageNumber(DamageNumberParams p)
    {
        SpawnDamageNumber(p.damage, p.worldPosition, p.isCrit, p.isPlayerDamage);
    }

    public void SpawnDamageNumber(float damage, Vector3 worldPosition, bool isCrit = false, bool isPlayerDamage = false)
    {
        if (_damageNumberPrefab == null)
        {
            Debug.LogWarning("[DamageNumberManager] Prefab is null");
            return;
        }

        if (_worldCamera == null || _damageNumberRoot == null)
            return;

        // 世界坐标转屏幕坐标
        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(_worldCamera, worldPosition);

        // 屏幕坐标转 UI 本地坐标
        Vector2 uiLocalPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _damageNumberRoot,
            screenPos,
            null,
            out uiLocalPos
        );

        // 从对象池获取
        GameObject obj = ManagerHub.Pool.Get(_damageNumberPrefab, Vector3.zero, Quaternion.identity);
        if (obj == null) return;

        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.SetParent(_damageNumberRoot);

        // 添加随机偏移，防止多发子弹同时命中时飘字重叠
        float offsetX = Random.Range(-_randomOffsetX, _randomOffsetX);
        float offsetY = Random.Range(-_randomOffsetY, _randomOffsetY);
        rt.localPosition = uiLocalPos + new Vector2(offsetX, offsetY);
        rt.localScale = Vector3.one;
        rt.rotation = Quaternion.identity;

        DamageNumberUI dn = obj.GetComponent<DamageNumberUI>();
        dn?.Show(damage, isCrit, isPlayerDamage);
    }
}
