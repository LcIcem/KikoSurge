using UnityEngine;

/// <summary>
/// 游戏入口管理器
/// </summary>
public class GameManager : MonoSingleton<GameManager>
{
    private void Log(string msg) => Debug.Log($"[{GetType().Name}] {msg}");

    private void Awake()
    {
        Log("GameManager initialized");
    }

    private void Start()
    {
        Log("Game started");
    }
}
