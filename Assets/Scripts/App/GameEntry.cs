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
        Log("GameEntry.Start() 开始");

        Log("初始化 ManagerHub...");
        _ = ManagerHub.Instance;


        Log("初始化 DamageLuaBridge...");
        DamageLuaBridge.Initialize();

        // 4. 初始化 CursorManager（必须在 UI 面板显示之前）
        InitCursorManager();

        Log("显示 LoginPanel...");
        ManagerHub.UI.ShowPanel<LoginPanel>(UILayerType.Middle);

        Log("应用音频设置...");
        ManagerHub.Audio.ApplySettings();

        Log("GameEntry.Start() 完成");
    }

    private void InitCursorManager()
    {
        Log("初始化 CursorManager...");
        var cursorMgr = CursorManager.Instance;

        // 加载光标资源（Addressables 同步加载）
        cursorMgr.aimTexture = ManagerHub.Addressables.Load<Texture2D>("cursorTexture_aim");
        cursorMgr.cursorTexture = ManagerHub.Addressables.Load<Texture2D>("cursorTexture_normal");

        // 完成事件订阅（此时 ManagerHub 已就绪）
        cursorMgr.InitAndSubscribe();
        Log("CursorManager 初始化完成");
    }
}
