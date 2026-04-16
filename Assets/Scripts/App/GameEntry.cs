using UnityEngine;
using LcIcemFramework.Core;
using LcIcemFramework.Managers;
using LcIcemFramework.Managers.UI;

/// <summary>
/// 游戏入口管理器
/// </summary>
public class GameEntry : MonoBehaviour
{
    private void Log(string msg) => Debug.Log($"[{GetType().Name}] {msg}");

    private void Start()
    {
        // 按顺序初始化 不是手动创建GameObject 的单例
        _ = ManagerHub.Instance;
        _ = GameDataManager.Instance;
        _ = LootManager.Instance;
        // 显示游戏开始面板
        Log("Game started");
        ManagerHub.UI.ShowPanel<LoginPanel>(UILayerType.Middle);
    }
}
