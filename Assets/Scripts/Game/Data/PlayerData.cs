using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// 玩家数据类
/// </summary>
public class PlayerData
{
    public int id;                  // 当前选择的角色id
    public string name;             // 当前选择的角色名
    private float _health = 5;      // 当前生命值（颗）
    public float maxHealth = 5;     // 当前最大生命值
    public float atk = 1;           // 当前攻击力
    public float def = 1;           // 当前防御力
    public float moveSpeed = 4;     // 当前移动速度
    public float DashSpeed = 6;     // 冲刺速度
    public float dashDuration = 0.1f; // 冲刺持续时间
    public float dashGap = 0.2f;    // 冲刺间隔时间
    public float invincibleDuration = 0.2f; // 无敌时间


    // 注意： 每当有生命值和最大生命值同时变化时，要先变化最大生命值。否则会因为 最大生命值 没来得及更新导致 生命值 出现错误
    // 比如这里先set了health 由于maxHealth没更新，生命值被限制在了更新前的maxHealth 
    public float Health
    {
        get => _health;
        set => _health = Mathf.Clamp(value, 0, maxHealth);
    }
}