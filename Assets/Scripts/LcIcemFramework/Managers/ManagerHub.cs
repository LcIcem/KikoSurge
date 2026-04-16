using UnityEngine;
using LcIcemFramework.Core;

namespace LcIcemFramework
{

/// <summary>
/// ManagerHub - 统一管理所有 Manager 的大单例。
/// <para>
/// 提供全局统一的 Manager 访问入口，确保所有 Manager 按正确顺序初始化。
/// 设计参考：Unity 官方 Manager of Managers 架构模式。
/// </para>
/// <para>
/// 使用方式：
/// <list type="bullet">
///   <item>通过 ManagerHub.Timer / ManagerHub.Pool 等访问任意 Manager</item>
///   <item>所有 Manager 在 ManagerHub 首次被访问时自动初始化，无需手动调用</item>
///   <item>初始化顺序：MonoManager → EventCenter → ResManager → TimerManager
///         → PoolManager → SaveManager → AudioManager
///         → AddressablesManager → UIManager → GameSceneManager → GameManager → InputManager</item>
/// </list>
/// </para>
/// </summary>
public class ManagerHub : SingletonMono<ManagerHub>
{
    #region Manager 访问器

    /// <summary>计时器管理器（Timer Manager）</summary>
    public static TimerManager Timer => _timer;
    /// <summary>对象池管理器（Pool Manager）</summary>
    public static PoolManager Pool => _pool;
    /// <summary>资源管理器（Resource Manager）</summary>
    public static ResManager Res => _res;
    /// <summary>存档管理器（Save Manager）</summary>
    public static SaveManager Save => _save;
    /// <summary>音频管理器（Audio Manager）</summary>
    public static AudioManager Audio => _audio;
    /// <summary>Addressables 资源管理器</summary>
    public static AddressablesManager Addressables => _addressables;
    /// <summary>UI 管理器（UI Manager）</summary>
    public static UIManager UI => _ui;
    /// <summary>场景管理器（Scene Manager）</summary>
    public static GameSceneManager Scene => _scene;
    /// <summary>输入管理器（Input Manager）</summary>
    public static InputManager Input => _input;

    #endregion

    #region 内部 Manager 实例字段

    private static TimerManager _timer;
    private static PoolManager _pool;
    private static ResManager _res;
    private static SaveManager _save;
    private static AudioManager _audio;
    private static AddressablesManager _addressables;
    private static UIManager _ui;
    private static GameSceneManager _scene;
    private static InputManager _input;

    #endregion

    #region 生命周期

    /// <summary>
    /// Init() 在 Awake 之前被调用，完成所有 Manager 的首次访问，
    /// 从而触发它们按正确的依赖顺序初始化。
    /// </summary>
    protected override void Init()
    {
        Log("ManagerHub 初始化开始...");

        // 1. MonoManager 必须最先初始化（其他 Manager 依赖它的 Update 帧循环）
        MonoManager.Instance.AddUpdateListener(OnUpdate);

        // 2. EventCenter（无依赖）
        _ = EventCenter.Instance;

        // 3. ResManager（UIManager 构造函数中会立即调用 Load）
        _res = ResManager.Instance;

        // 4. TimerManager（依赖 MonoManager 的 Update）
        _timer = TimerManager.Instance;

        // 5. PoolManager（独立，无其他 Manager 依赖）
        _pool = PoolManager.Instance;

        // 6. SaveManager（独立）
        _save = SaveManager.Instance;

        // 7. AudioManager（独立）
        _audio = AudioManager.Instance;

        // 8. AddressablesManager（UIManager 依赖它异步加载 UI）
        _addressables = AddressablesManager.Instance;
        _addressables.Initialize();

        // 9. UIManager（依赖 AddressablesManager 异步加载 UI Prefab）
        _ui = UIManager.Instance;

        // 10. GameSceneManager（独立）
        _scene = GameSceneManager.Instance;

        // 11. InputManager（独立）
        _input = InputManager.Instance;

        Log("ManagerHub 初始化完成。");
    }

    private void OnUpdate()
    {
        // ManagerHub 的 Update 帧回调（目前无全局逻辑，可扩展）
    }

    #endregion

    #region 日志

    private void Log(string msg) => Debug.Log($"[ManagerHub] {msg}");

    #endregion
}
}
