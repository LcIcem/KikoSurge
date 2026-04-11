using LcIcemFramework.Core;
using ProcGen.Config;
using ProcGen.Core;
using ProcGen.Runtime;
using ProcGen.Seed;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 游戏入口
/// 单局地牢的入口
/// </summary>
[RequireComponent(typeof(DungeonBuilder))]
public class GameEntry : MonoBehaviour
{
    [Header("生成配置")]
    [SerializeField] private DungeonModel_SO _dungeonModel; // Inspector上的地牢配置SO

    [Header("随机数")]
    [SerializeField] private long _seed;

    [Header("PlayerHandler")]
    [SerializeField] private PlayerHandler playerHandler;

    private DungeonBuilder _builder;
    private GameRandom _rng;
    public DungeonGraph dungeonGraph => _builder?.GetGraph();
    public bool IsBuildCompleted => _builder?.IsBuildCompleted ?? false;

    private void Awake()
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
        if (ctx.performed)
        {
            _builder.Build(_dungeonModel, new GameRandom(System.Environment.TickCount));
            playerHandler?.RegeneratePlayer();
        }
    }
}
