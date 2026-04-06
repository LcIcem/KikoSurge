using UnityEngine;

using LcIcemFramework.Core;

namespace Game
{

/// <summary>
/// 游戏入口管理器
/// </summary>
public class GameManager : SingletonMono<GameManager>
{
    private void Log(string msg) => Debug.Log($"[{GetType().Name}] {msg}");

    protected override void Init()
    {
        Log("GameManager initialized");
    }

    private void Start()
    {
        Log("Game started");
    }
}
}
