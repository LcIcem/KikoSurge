using UnityEngine;
using LcIcemFramework.Core;
using LcIcemFramework;

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

        // 初始化 xLua 伤害公式
        DamageLuaBridge.Initialize();

        // 显示游戏开始面板
        ManagerHub.UI.ShowPanel<LoginPanel>(UILayerType.Middle);
        // 播放背景音乐（启动时同步一次持久化数据）
        ManagerHub.Audio.ApplySettings();
        // BGM 将在 GameLifecycleManager.ChangeState 时自动播放
    }
}
