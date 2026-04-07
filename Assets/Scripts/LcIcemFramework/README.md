# LcIcemFramework 使用手册

> KikoSurge 游戏框架 | Unity 2022.3.62f3 LTS | Manager of Managers 架构

---

## 目录

- [1. 架构概览](#1-架构概览)
- [2. 单例基类](#2-单例基类)
- [3. ManagerHub 统一入口](#3-managerhub-统一入口)
- [4. 事件中心 EventCenter](#4-事件中心-eventcenter)
- [5. 状态机 FSM](#5-状态机-fsm)
- [6. 生命周期 MonoManager](#6-生命周期-monomanager)
- [7. 对象池 PoolManager](#7-对象池-poolmanager)
- [8. 计时器 TimerManager](#8-计时器-timermanager)
- [9. 音频 AudioManager](#9-音频-audiomanager)
- [10. UI 面板 BasePanel + UIManager](#10-ui-面板-basepanel--uimanager)
- [11. 存档 SaveManager](#11-存档-savemanager)
- [12. 场景切换 GameSceneManager](#12-场景切换-gamescenemanager)
- [13. Addressables 资源管理](#13-addressables-资源管理)
- [14. 相机特效 CameraController](#14-相机特效-cameracontroller)
- [15. 工具类](#15-工具类)
- [16. 命名空间速查表](#16-命名空间速查表)

---

## 1. 架构概览

框架采用 **Manager of Managers（管理器之管理器）** 架构，通过大单例 `ManagerHub` 统一托管所有子系统，所有跨模块通信必须走 `EventCenter`。

```
GameManager (SingletonMono<GameManager>)
    └── ManagerHub (SingletonMono<ManagerHub>)  ← 所有 Manager 的统一入口
          ├── MonoManager     (Singleton<MonoManager>)  ← 协程 / Update 帧循环驱动
          ├── EventCenter     (Singleton<EventCenter>)  ← 全局事件总线（发布-订阅）
          ├── ResManager      (Singleton<ResManager>)  ← Resources 资源加载
          ├── TimerManager    (Singleton<TimerManager>) ← 全局计时器
          ├── PoolManager     (SingletonMono<PoolManager>) ← 对象池
          ├── SaveManager     (SingletonMono<SaveManager>) ← 多槽位存档（AES加密）
          ├── AudioManager   (SingletonMono<AudioManager>) ← BGM ×1 + SFX池 ×10
          ├── AddressablesManager (SingletonMono<AddressablesManager>) ← Addressables 封装
          ├── UIManager      (Singleton<UIManager>)  ← UI 面板异步加载管理
          └── GameSceneManager (SingletonMono<GameSceneManager>) ← 场景异步加载

Camera (MonoBehaviour)
    └── ICameraEffect 接口体系（冲击 / 螺旋 / 推进）
```

### 初始化顺序（自动拓扑排序）

```
MonoManager → EventCenter → ResManager → TimerManager
           → PoolManager → SaveManager → AudioManager
           → AddressablesManager → UIManager → GameSceneManager
```

---

## 2. 单例基类

### `Singleton<T>` — 普通 C# 类

```csharp
using LcIcemFramework.Core;

public class MyService : Singleton<MyService>
{
    protected override void Init()
    {
        // 替代构造函数，首次 Instance 访问时调用
        Debug.Log("MyService initialized");
    }
}

// 访问
MyService.Instance.DoSomething();
```

### `SingletonMono<T>` — Unity 组件

```csharp
using LcIcemFramework.Core;

public class MyManager : SingletonMono<MyManager>
{
    protected override void Init()
    {
        // 在 Awake 中调用，替代构造函数
        Debug.Log("MyManager ready");
    }
}
```

**重要**：`SingletonMono<T>` 的 `Awake()` 已由基类实现，包含防重复实例保护。子类重写时须使用 `override`，业务逻辑应放在 `Init()` 中。

---

## 3. ManagerHub 统一入口

**禁止直接访问各 Manager 的 Instance**。所有 Manager 通过 `ManagerHub` 静态属性访问：

```csharp
// ✅ 正确
ManagerHub.Timer.AddTimeOut(1f, () => Debug.Log("Done"));
ManagerHub.Pool.Get("Bullet", pos, rot);
ManagerHub.UI.ShowPanel<UIMainMenu>();

// ❌ 禁止
TimerManager.Instance.AddTimeOut(...);
PoolManager.Instance.Get(...);
```

### 完整访问路径

| Manager | 访问路径 | 命名空间 |
|---------|---------|---------|
| 计时器 | `ManagerHub.Timer` | `LcIcemFramework.Managers.Timer` |
| 对象池 | `ManagerHub.Pool` | `LcIcemFramework.Managers.Pool` |
| 资源 | `ManagerHub.Res` | `LcIcemFramework.Managers.Res` |
| 存档 | `ManagerHub.Save` | `LcIcemFramework.Managers.Save` |
| 音频 | `ManagerHub.Audio` | `LcIcemFramework.Managers.Audio` |
| Addressables | `ManagerHub.Addressables` | `LcIcemFramework.Managers.Addressables` |
| UI | `ManagerHub.UI` | `LcIcemFramework.Managers.UI` |
| 场景 | `ManagerHub.Scene` | `LcIcemFramework.Managers.Scenes` |

---

## 4. 事件中心 EventCenter

**所有跨 Manager、Manager 与 View 之间的通信必须走 EventCenter。**

### 定义事件参数

在 `EventID.cs` 中添加枚举值：

```csharp
namespace LcIcemFramework.Core
{
    public enum EventID
    {
        PlayBGM,
        PlaySFX,
        PlayerDamaged,    // 新增
        WaveStarted,      // 新增
    }
}
```

为需要参数的事件创建 `IEventCallback` 子类：

```csharp
// 带参数事件
public class PlayerDamagedParams : IEventCallback
{
    public int Damage;
    public Vector3 Position;
}

// 发布
EventCenter.Instance.Publish(EventID.PlayerDamaged,
    new PlayerDamagedParams { Damage = 10, Position = transform.position });

// 订阅（强类型，无需 string）
EventCenter.Instance.Subscribe(EventID.PlayerDamaged, OnPlayerDamaged);
private void OnPlayerDamaged(PlayerDamagedParams p)
{
    Debug.Log($"Player took {p.Damage} damage at {p.Position}");
}

// 取消订阅
EventCenter.Instance.Unsubscribe(EventID.PlayerDamaged, OnPlayerDamaged);

// 无参数事件
EventCenter.Instance.Publish(EventID.WaveStarted);
EventCenter.Instance.Subscribe(EventID.WaveStarted, OnWaveStarted);
```

---

## 5. 状态机 FSM

### 基础状态

```csharp
using LcIcemFramework.FSM;

public class IdleState : StateBase
{
    public override void Enter()
    {
        Debug.Log("Enter Idle");
    }

    public override void Exec()
    {
        // 每帧执行逻辑
    }

    public override void Exit()
    {
        Debug.Log("Exit Idle");
    }
}
```

### 构建状态机

```csharp
public class PlayerFSM : FSM
{
    private IdleState _idle = new IdleState();
    private RunState  _run  = new RunState();
    private JumpState _jump = new JumpState();

    protected override void OnSetup()
    {
        // 注册状态（按类型名）
        AddState(_idle);
        AddState(_run);
        AddState(_jump);

        // 添加转换：Idle → Run（条件：正在移动）
        AddTransition(_idle, _run,  () => Speed > 0.1f);
        AddTransition(_run,  _idle, () => Speed < 0.1f);

        // 添加转换：任意状态 → Jump（触发器）
        AddAnyTransition(_jump, () => CheckTrigger("Jump"));
    }
}

// 使用
var fsm = new PlayerFSM(player);
fsm.Start();                           // 从 EntryState 开始
fsm.Update();                          // 每帧调用
fsm.SetTrigger("Jump");                // 触发转换
fsm.ChangeState(_run);                 // 强制切换
fsm.Stop();                            // 停止
```

### 组合条件

```csharp
// 逻辑与：同时满足多个条件
AddTransition(_idle, _run,
    () => Speed > 0.1f
        .And(Hp > 0)
        .And(!IsStunned));

// 逻辑或：满足任一条件
AddTransition(_idle, _run,
    () => IsMoving
        .Or(IsSliding));
```

### 子状态机（SubFSM）

嵌入 FSM 作为父状态机中的一个子状态，适合分层状态机：

```csharp
public class WeaponSubFSM : SubFSM
{
    public WeaponSubFSM(FSM parent) : base(parent) { }

    protected override void OnSetup()
    {
        AddState(new WeaponIdle());
        AddState(new WeaponAttack());
        AddState(new WeaponReload());
        // ...
    }
}

// 在父 FSM 中作为子状态使用
var weaponFSM = new WeaponSubFSM(enemyFSM);
enemyFSM.AddState(weaponFSM);
enemyFSM.AddTransition(_idle, weaponFSM, () => weaponFSM.CheckTrigger("Fire"));
```

---

## 6. 生命周期 MonoManager

为普通 C# 类（非 MonoBehaviour）提供 `Update` 帧循环和协程能力：

```csharp
// 注册帧回调
ManagerHub.Mono.AddUpdateListener(MyUpdate);

// 启动协程
Coroutine routine = ManagerHub.Mono.StartCoroutine(MyAsyncLoad());
ManagerHub.Mono.StopCoroutine(routine);
```

---

## 7. 对象池 PoolManager

**禁止使用 `Instantiate/Destroy`，所有对象必须走 PoolManager。**

### 定义可池化对象

```csharp
using LcIcemFramework.Managers.Pool;

public class Bullet : MonoBehaviour, IPoolable
{
    public void OnSpawn()
    {
        // 对象从池中取出时调用，重置状态
        _hp = _maxHp;
        gameObject.SetActive(true);
    }

    public void OnDespawn()
    {
        // 对象归还池时调用，清理状态
        gameObject.SetActive(false);
    }
}
```

### 使用对象池

```csharp
// 注册（通常在游戏初始化时）
ManagerHub.Pool.Register("Bullet", bulletPrefab, initialCount: 100, maxSize: 500);

// 获取（从池中取出，位置/旋转可自定义）
GameObject bullet = ManagerHub.Pool.Get("Bullet", firePos, Quaternion.identity);

// 归还（使用完毕后归还池）
ManagerHub.Pool.Release("Bullet", bulletInstance);

// 销毁整个池
ManagerHub.Pool.Unregister("Bullet");

// 清空所有池
ManagerHub.Pool.ClearAll();
```

---

## 8. 计时器 TimerManager

```csharp
// 单次计时器
int timerId = ManagerHub.Timer.AddTimeOut(2f, () => Debug.Log("2秒后执行"));

// 重复计时器
int repeatId = ManagerHub.Timer.AddRepeating(1f, () => Debug.Log("每秒执行"));

// 暂停/恢复（全局）
ManagerHub.Timer.Pause();
ManagerHub.Timer.Resume();

// 取消单个计时器
ManagerHub.Timer.Clear(timerId);

// 取消全部
ManagerHub.Timer.ClearAll();

// 查询剩余时间
float remaining = ManagerHub.Timer.GetRemainingTime(timerId);
```

> 使用 `Time.unscaledDeltaTime`，不受 `Time.timeScale` 影响，支持全局暂停。

---

## 9. 音频 AudioManager

```csharp
// 播放 BGM（异步加载）
ManagerHub.Audio.PlayBGM("BGM_Battle");

// 播放 SFX（从池中复用）
ManagerHub.Audio.PlaySFX("SFX_Explosion");

// 音量控制（0~1）
ManagerHub.Audio.SetBGMVolume(0.5f);
ManagerHub.Audio.SetSFXVolume(1.0f);

// 静音
ManagerHub.Audio.MuteBGM();
ManagerHub.Audio.MuteSFX();
```

音频 ID 常量定义在 `AudioID.cs` 中，建议通过常量引用而非硬编码字符串。

---

## 10. UI 面板 BasePanel + UIManager

### 编写面板类

```csharp
using LcIcemFramework.Managers.UI;
using UnityEngine.UI;

public class UIHUDPanel : BasePanel
{
    // 无需手动查找控件，基类 Awake 已自动缓存
    // 通过 GetControl<T>("控件名称") 访问

    protected override void OnClick(string btnName)
    {
        switch (btnName)
        {
            case "BtnPause":
                // 暂停游戏
                break;
        }
    }

    public override void Show()
    {
        base.Show();
        // 播放入场动画等
    }

    public override void Hide()
    {
        // 播放退场动画等
        base.Hide();
    }
}
```

### 加载面板

```csharp
// 异步加载并显示（自动防止重复加载）
ManagerHub.UI.ShowPanel<UIHUDPanel>(UILayerType.Top, panel =>
{
    Debug.Log("面板加载完成");
});

// 隐藏并销毁
ManagerHub.UI.HidePanel("UIHUDPanel");

// 查询已加载面板
var hud = ManagerHub.UI.GetPanel<UIHUDPanel>("UIHUDPanel");

// 添加自定义事件（拖拽、悬停等）
ManagerHub.UI.AddCustomEventListener(
    GetControl<Image>("DragArea"),
    EventTriggerType.BeginDrag,
    data => Debug.Log("开始拖拽"));
```

> **UI层级**：`Bottom`（底层背景）→ `Middle`（默认面板）→ `Top`（弹窗）→ `SystemLayer`（Toast提示）

---

## 11. 存档 SaveManager

### 定义存档数据

```csharp
using LcIcemFramework.Managers.Save;

public class GameSaveData : SaveData
{
    public int Level;
    public int Score;
    public List<string> UnlockedWeapons;
}
```

### 存档操作

```csharp
// 保存（自动 JSON 序列化 + AES 加密）
var data = new GameSaveData { Level = 5, Score = 12000 };
ManagerHub.Save.Save(0, data);

// 读取
GameSaveData loaded = ManagerHub.Save.Load(0) as GameSaveData;

// 查询
bool hasSave = ManagerHub.Save.Exists(0);
int[] usedSlots = ManagerHub.Save.GetUsedSlots();

// 删除
ManagerHub.Save.Delete(0);
```

> 存档路径：`Application.persistentDataPath/saves/save_0.json`

---

## 12. 场景切换 GameSceneManager

```csharp
// 异步加载（带进度条）
ManagerHub.Scene.LoadSceneAsync("GameScene",
    onProgress: p => loadingBar.value = p,
    onComplete: () => Debug.Log("场景加载完成"));

// 同步加载（仅 Editor 调试用）
ManagerHub.Scene.LoadScene("GameScene");

// 获取当前场景
string current = ManagerHub.Scene.GetCurrentScene();

// 卸载场景
ManagerHub.Scene.UnloadCurrentScene();
```

---

## 13. Addressables 资源管理

```csharp
// 初始化（通常由 ManagerHub 自动调用）
ManagerHub.Addressables.Initialize();

// 加载单个资源
Sprite sprite = await ManagerHub.Addressables.LoadAsync<Sprite>("Sprites/Player");
GameObject prefab = await ManagerHub.Addressables.LoadAsync<GameObject>("Prefabs/Enemy");

// 加载 Prefab 并实例化
GameObject enemy = await ManagerHub.Addressables.InstantiateAsync("Prefabs/Enemy", parent);

// 按标签批量加载
var weapons = await ManagerHub.Addressables.LoadByLabelAsync<GameObject>("Rare");

// 释放资源引用
ManagerHub.Addressables.Release(handle);

// 释放实例
ManagerHub.Addressables.ReleaseInstance(enemyInstance);
```

> **Addressables 地址命名规范**：`SO_角色_名称`（如 `SO_Player_Kiko`）

---

## 14. 相机特效 CameraController

将 `CameraController` 挂载到相机 GameObject 上，设置 `target` 为跟随目标。

```csharp
// 冲击效果（随机方向）
Camera.main.GetComponent<CameraController>().ImpactRandom(0.8f);

// 冲击效果（指定方向）
controller.ImpactDir(ImpactDirection.Left, 0.5f);

// 螺旋效果
controller.Spiral(0.6f);

// 推进效果
controller.Zoom(0.7f);
```

三种效果可以叠加，通过 `LateUpdate` 实时合成偏移量，实现平滑的组合视觉反馈。

---

## 15. 工具类

### 扩展方法（Extensions）

```csharp
using LcIcemFramework.Util.Ext;

// Vector2 → Vector3
vec3 = vec2.ToVector3();

// Transform 快速设坐标
transform.SetPosX(5f);
transform.SetPosY(0f);
transform.SetPosZ(-10f);

// float 精度判断（浮点误差容忍）
if (value.IsEqualsTo(0f, 1e-4f)) { }

// 浮点四舍五入
float rounded = value.Round(2);  // 保留2位小数

// 数组/列表随机取
var item = array.Random();

// 空安全字符串
string s = str.SafeToString();
```

### JSON 序列化（LitJson）

```csharp
using LcIcemFramework.Util.Data;

// 对象 ↔ JSON 字符串
string json = JsonUtil.ToJson(myData);
var obj = JsonUtil.FromJson<MyData>(json);

// 文件操作
JsonUtil.SaveToFile("path.json", data);
var loaded = JsonUtil.LoadFromFile<MyData>("path.json");
```

### 加密工具（AES / MD5 / SHA256）

```csharp
using LcIcemFramework.Util.Crypto;

// AES 加解密（存档用）
string encrypted = EncryptUtil.AESEncrypt(plainText, "kikoSurge2026");
string decrypted = EncryptUtil.AESDecrypt(encrypted, "kikoSurge2026");

// 哈希
string hash = EncryptUtil.SHA256(input);
```

### 数学工具

```csharp
using LcIcemFramework.Util.Math;

// 获取 2D 震荡点（相机螺旋效果内部使用）
Vector3 pos = MathUtil.GetXYOscPoint(time, amplitude, omega, phi);

// 方向/角度转换
Vector2 dir = MathUtil.DirectionTo(from, to);
float angle = MathUtil.AngleFromDir(dir);

// 2D 距离（忽略 Z 轴）
float dist = MathUtil.DistanceXY(p1, p2);

// 角度插值（自动处理 0°/360° 跨越）
float lerped = MathUtil.LerpAngle(a, b, t);

// 加权随机
int result = MathUtil.WeightedRandom(
    (80, 0),  // 80% 概率选 0
    (20, 1)   // 20% 概率选 1
);
```

---

## 16. 命名空间速查表

| 文件夹 | 命名空间 |
|--------|---------|
| Core | `LcIcemFramework.Core` |
| FSM | `LcIcemFramework.FSM` |
| Camera | `LcIcemFramework.Camera` |
| Managers | `LcIcemFramework.Managers` |
| Managers/Mono | `LcIcemFramework.Managers.Mono` |
| Managers/Timer | `LcIcemFramework.Managers.Timer` |
| Managers/Pool | `LcIcemFramework.Managers.Pool` |
| Managers/Res | `LcIcemFramework.Managers.Res` |
| Managers/Save | `LcIcemFramework.Managers.Save` |
| Managers/Audio | `LcIcemFramework.Managers.Audio` |
| Managers/UI | `LcIcemFramework.Managers.UI` |
| Managers/Scenes | `LcIcemFramework.Managers.Scenes` |
| Managers/Addressables | `LcIcemFramework.Managers.Addressables` |
| Util/Const | `LcIcemFramework.Util.Const` |
| Util/Ext | `LcIcemFramework.Util.Ext` |
| Util/Data | `LcIcemFramework.Util.Data` |
| Util/Crypto | `LcIcemFramework.Util.Crypto` |
| Util/Math | `LcIcemFramework.Util.Math` |
