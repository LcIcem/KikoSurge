/// <summary>
/// 子弹伤害计算参数
/// </summary>
public class BulletDamageParams
{
    // ===== 子弹基础伤害 =====
    public float bulletBaseDamage;

    // ===== 玩家属性 =====
    public float playerAtk;
    public float playerCritRate;
    public float playerCritMultiplier;
    public float playerDamageBonus;
    public float playerDefBreak;

    // ===== 武器属性 =====
    public float weaponDamage;
    public float weaponCritRate;
    public float weaponCritMultiplier;
    public float weaponDamageBonus;

    // ===== 目标属性 =====
    public float targetDefense;
}
