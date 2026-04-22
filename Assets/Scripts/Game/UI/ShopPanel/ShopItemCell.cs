using System;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 商店商品列表项
/// </summary>
public class ShopItemCell : MonoBehaviour
{
    [SerializeField] private Image _imgIcon;
    [SerializeField] private TMP_Text _txtName;
    [SerializeField] private TMP_Text _txtDescription;
    [SerializeField] private TMP_Text _txtPrice;
    [SerializeField] private Button _btnBuyOne;
    [SerializeField] private Button _btnBuyAll;

    private int _itemIndex;
    private Action<int> _onBuyOneClicked;
    private Action<int> _onBuyAllClicked;

    /// <summary>
    /// 初始化商品数据
    /// </summary>
    public void Initialize(int index, ItemConfig itemConfig, int quantity, int unitPrice, Action<int> onBuyOneClicked, Action<int> onBuyAllClicked)
    {
        _itemIndex = index;
        _onBuyOneClicked = onBuyOneClicked;
        _onBuyAllClicked = onBuyAllClicked;

        if (itemConfig == null)
        {
            Clear();
            return;
        }

        // 设置图标
        if (_imgIcon != null)
        {
            _imgIcon.sprite = itemConfig.Icon;
            _imgIcon.preserveAspect = true;
            _imgIcon.enabled = itemConfig.Icon != null;
        }

        // 设置名称（数量跟在名称后面）
        if (_txtName != null)
        {
            _txtName.text = quantity > 1 ? $"{itemConfig.Name} x{quantity}" : itemConfig.Name;
        }

        // 设置描述（详细描述）
        if (_txtDescription != null)
        {
            _txtDescription.text = BuildItemDescription(itemConfig);
        }

        // 设置价格（显示单价和总价）
        int totalPrice = unitPrice * quantity;
        if (_txtPrice != null)
        {
            _txtPrice.text = quantity > 1 ? $"单价 {unitPrice} / 总价 {totalPrice}" : $"单价 {totalPrice}";
        }
    }

    /// <summary>
    /// 清空商品数据
    /// </summary>
    public void Clear()
    {
        if (_imgIcon != null)
        {
            _imgIcon.sprite = null;
            _imgIcon.enabled = false;
        }
        if (_txtName != null) _txtName.text = "";
        if (_txtDescription != null) _txtDescription.text = "";
        if (_txtPrice != null) _txtPrice.text = "";
    }

    private void Start()
    {
        if (_btnBuyOne != null)
        {
            _btnBuyOne.onClick.AddListener(OnBuyOneClicked);
        }
        if (_btnBuyAll != null)
        {
            _btnBuyAll.onClick.AddListener(OnBuyAllClicked);
        }
    }

    private void OnDestroy()
    {
        if (_btnBuyOne != null)
        {
            _btnBuyOne.onClick.RemoveListener(OnBuyOneClicked);
        }
        if (_btnBuyAll != null)
        {
            _btnBuyAll.onClick.RemoveListener(OnBuyAllClicked);
        }
    }

    private void OnBuyOneClicked()
    {
        _onBuyOneClicked?.Invoke(_itemIndex);
    }

    private void OnBuyAllClicked()
    {
        _onBuyAllClicked?.Invoke(_itemIndex);
    }

    /// <summary>
    /// 构建物品详细描述
    /// </summary>
    private string BuildItemDescription(ItemConfig itemConfig)
    {
        if (itemConfig == null)
            return "";

        return itemConfig.Type switch
        {
            ItemType.Weapon => BuildWeaponDescription(itemConfig),
            ItemType.Potion => BuildPotionDescription(itemConfig),
            ItemType.Relic => BuildRelicDescription(itemConfig),
            _ => itemConfig.Description ?? ""
        };
    }

    /// <summary>
    /// 构建武器描述
    /// </summary>
    private string BuildWeaponDescription(ItemConfig itemConfig)
    {
        var weaponConfig = GameDataManager.Instance?.GetWeaponConfig(itemConfig.Id);
        if (weaponConfig == null)
            return itemConfig.Description ?? "";

        var sb = new StringBuilder();

        // 武器类型
        string fireModeName = weaponConfig.fireMode switch
        {
            FireMode.Single => "单发",
            FireMode.Spread => "霰弹",
            FireMode.Burst => "连发",
            FireMode.Continuous => "激光",
            FireMode.Charge => "蓄力",
            _ => "未知"
        };
        sb.AppendLine($"类型: {fireModeName}");

        // 子弹类型
        if (weaponConfig.bulletConfig != null)
        {
            string bulletTypeName = weaponConfig.bulletConfig.bulletType switch
            {
                BulletType.Straight => "直线",
                BulletType.Parabola => "抛物线",
                BulletType.Homing => "追踪",
                _ => "未知"
            };
            sb.AppendLine($"弹道: {bulletTypeName}");
        }

        // 射速
        sb.AppendLine($"射速: {weaponConfig.fireRate:F1}/秒");

        // 弹夹
        sb.AppendLine($"弹夹: {weaponConfig.magazineSize}");

        // 武器伤害
        if (weaponConfig.weaponDamage > 0)
            sb.AppendLine($"武器伤害: +{weaponConfig.weaponDamage:F1}");

        // 暴击率
        if (weaponConfig.weaponCritRate > 0)
            sb.AppendLine($"暴击率: +{weaponConfig.weaponCritRate:P0}");

        // 暴击倍率
        if (weaponConfig.weaponCritMultiplier > 0)
            sb.AppendLine($"暴击倍率: +{weaponConfig.weaponCritMultiplier:P0}");

        // 伤害加成
        if (weaponConfig.weaponDamageBonus > 0)
            sb.AppendLine($"伤害加成: +{weaponConfig.weaponDamageBonus:P0}");

        if (!string.IsNullOrEmpty(itemConfig.Description))
        {
            sb.AppendLine();
            sb.Append(itemConfig.Description);
        }

        return sb.ToString();
    }

    /// <summary>
    /// 构建药水描述
    /// </summary>
    private string BuildPotionDescription(ItemConfig itemConfig)
    {
        var potionConfig = itemConfig as PotionItemConfig;
        if (potionConfig == null)
            return itemConfig.Description ?? "";

        var sb = new StringBuilder();

        // 即时效果
        if (potionConfig.instantEffectType != PotionInstantEffectType.None
            && potionConfig.instantEffectValue > 0)
        {
            string effectName = potionConfig.instantEffectType switch
            {
                PotionInstantEffectType.Heal => "恢复生命",
                _ => "未知效果"
            };
            sb.AppendLine($"即时: {effectName} +{potionConfig.instantEffectValue:F0}");
        }

        // 限时效果
        if (potionConfig.timedEffectType != PotionTimedEffectType.None
            && potionConfig.timedEffectValue > 0)
        {
            string effectName = potionConfig.timedEffectType switch
            {
                PotionTimedEffectType.Shield => "护盾",
                PotionTimedEffectType.SpeedBoost => "加速",
                _ => "未知效果"
            };

            if (potionConfig.timedEffectType == PotionTimedEffectType.SpeedBoost)
                sb.AppendLine($"限时: [{effectName}] +{potionConfig.timedEffectValue:F0}% 速度");
            else
                sb.AppendLine($"限时: [{effectName}] +{potionConfig.timedEffectValue:F1}");

            sb.AppendLine($"持续: {potionConfig.timedEffectDuration:F1}秒");
        }

        if (!string.IsNullOrEmpty(itemConfig.Description))
        {
            sb.AppendLine();
            sb.Append(itemConfig.Description);
        }

        return sb.ToString();
    }

    /// <summary>
    /// 构建遗物描述
    /// </summary>
    private string BuildRelicDescription(ItemConfig itemConfig)
    {
        var relicConfig = itemConfig as RelicConfig;
        if (relicConfig == null)
            return itemConfig.Description ?? "";

        var sb = new StringBuilder();

        // 属性加成
        if (relicConfig.modifiers != null)
        {
            var validModifiers = relicConfig.modifiers.FindAll(m => m.value != 0f);
            if (validModifiers.Count > 0)
            {
                sb.AppendLine("属性加成:");
                foreach (var mod in validModifiers)
                {
                    string modName = GetModifierDisplayName(mod.type);
                    string valueStr = IsPercentModifier(mod.type)
                        ? $"{mod.value * 100:F0}%"
                        : $"+{mod.value:F1}";
                    sb.AppendLine($"  {modName}: {valueStr}");
                }
            }
        }

        // 遗物效果
        if (relicConfig.effects != null && relicConfig.effects.Count > 0)
        {
            sb.AppendLine("遗物效果:");
            foreach (var effect in relicConfig.effects)
            {
                string effectDesc = GetRelicEffectDescription(effect);
                if (!string.IsNullOrEmpty(effectDesc))
                {
                    sb.AppendLine($"  {effectDesc}");
                }
            }
        }

        if (!string.IsNullOrEmpty(itemConfig.Description))
        {
            sb.AppendLine();
            sb.Append(itemConfig.Description);
        }

        return sb.ToString();
    }

    /// <summary>
    /// 获取修饰符显示名称
    /// </summary>
    private string GetModifierDisplayName(ModifierType type)
    {
        return type switch
        {
            ModifierType.MaxHealth => "最大生命",
            ModifierType.Attack => "攻击力",
            ModifierType.Defense => "防御力",
            ModifierType.MoveSpeed => "移动速度",
            ModifierType.CritRate => "暴击率",
            ModifierType.CritMultiplier => "暴击伤害",
            ModifierType.DamageBonus => "伤害加成",
            ModifierType.DefBreak => "防御穿透",
            _ => type.ToString()
        };
    }

    /// <summary>
    /// 判断是否为百分比修饰符
    /// </summary>
    private bool IsPercentModifier(ModifierType type)
    {
        return type is ModifierType.CritRate
            or ModifierType.CritMultiplier
            or ModifierType.DamageBonus
            or ModifierType.DefBreak;
    }

    /// <summary>
    /// 获取遗物效果描述
    /// </summary>
    private string GetRelicEffectDescription(RelicEffect effect)
    {
        return effect switch
        {
            DungeonGenerationEffect dge when dge.extraEliteChance > 0 || dge.extraTreasureChance > 0 =>
                $"额外精英怪 +{dge.extraEliteChance}%, 额外宝箱 +{dge.extraTreasureChance}%",
            RoomBehaviorEffect rbe when rbe.enemyCountBonus != 0 || rbe.eliteChanceBonus > 0 || rbe.lootMultiplier > 1f =>
                $"敌人波次 +{rbe.enemyCountBonus}, 精英概率 +{rbe.eliteChanceBonus * 100:F0}%, 掉落 +{(rbe.lootMultiplier - 1f) * 100:F0}%",
            _ => null
        };
    }
}
