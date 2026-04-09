using LcIcemFramework.Core;
using ProcGen.Config;
using ProcGen.Runtime;
using ProcGen.Seed;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 游戏入口单例
/// </summary>
public class GameEntry : SingletonMono<GameEntry>
{
    [Header("生成配置")]
    [SerializeField] private DungeonModel_SO _dungeonModel; // Inspector上的地牢配置SO

    [Header("随机数")]
    [SerializeField] private long _seed;

    private DungeonBuilder _builder;
    private GameRandom _rng;

    protected override void Init()
    {
        _rng = new GameRandom(_seed);
        _builder = gameObject.GetComponent<DungeonBuilder>();
        if (_builder == null)
            _builder = gameObject.AddComponent<DungeonBuilder>();
    }


    // 调试用
    [ContextMenu("重新生成地牢")]
    public void Rebuild()
    {
        _rng.Reset();
        _builder.Build(_dungeonModel, _rng);
    }

    public void OnPressRBtn(InputAction.CallbackContext ctx)
    {
        if (ctx.performed) _builder.Build(_dungeonModel, new GameRandom(System.Environment.TickCount));
    }
}
