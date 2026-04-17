using System.Collections;
using LcIcemFramework.Camera;
using LcIcemFramework.Core;
using LcIcemFramework;
using UnityEngine;
using Game.Event;

/// <summary>
/// 玩家处理类
/// <para>只负责玩家的创建/失活/激活/传送，不持有玩家数据，不依赖 Unity 生命周期</para>
/// </summary>
public class PlayerHandler
{
    // 玩家相关
    private GameObject _playerPrefabs;
    private GameObject _playerInstance;

    /// <summary>
    /// 获取玩家实例
    /// </summary>
    public GameObject PlayerInstance => _playerInstance;

    /// <summary>
    /// 获取玩家数据（委托给 GameDataManager）
    /// </summary>
    public PlayerData PlayerData => GameDataManager.Instance.PlayerData;

    /// <summary>
    /// 创建玩家并放置到指定位置（异步等待 RoleInfo 加载）
    /// </summary>
    /// <param name="worldPos">世界坐标位置</param>
    public void CreatePlayer(Vector3 worldPos)
    {
        // 使用 MonoManager 启动协程，等待 RoleInfo 加载完成后创建玩家
        MonoManager.Instance.StartCoroutine(CreatePlayerAfterDataLoaded(worldPos));
    }

    private IEnumerator CreatePlayerAfterDataLoaded(Vector3 worldPos)
    {
        // 等待 RoleInfo 加载完成
        while (!GameDataManager.Instance.IsRoleInfoLoaded)
        {
            yield return new WaitForSeconds(0.1f);
        }

        if (_playerInstance != null)
            Object.Destroy(_playerInstance);

        _playerPrefabs = GameDataManager.Instance.GetRoleDataByCurSel().prefabs;

        // 从 RoleInfo 初始化 PlayerData（写入 GameDataManager）
        PlayerData playerData = GameDataManager.Instance.GetRoleDataByCurSel().ConvertToPlayerData();
        GameDataManager.Instance.PlayerData = playerData;

        _playerInstance = Object.Instantiate(_playerPrefabs, worldPos, Quaternion.identity);

        CameraManager.Instance.Follow(_playerInstance.transform);

        ManagerHub.UI.ShowPanel<HubPanel>(UILayerType.Middle, (panel) =>
        {
            EventCenter.Instance.Publish(GameEventID.UpdateHeartDisplay, playerData);
            // 主动发布当前武器信息（事件可能在 HubPanel 订阅之前就发布了）
            var player = _playerInstance.GetComponent<Player>();
            if (player != null && player.weaponHandler.CurrentWeapon != null)
            {
                EventCenter.Instance.Publish(GameEventID.OnCurrentWeaponChanged, player.weaponHandler.CurrentWeapon);
            }
        });
    }

    /// <summary>
    /// 暂时失活玩家（不销毁）
    /// </summary>
    public void DeactivatePlayer()
    {
        if (_playerInstance != null)
        {
            var player = _playerInstance.GetComponent<Player>();
            player?.weaponHandler.ClearAllWeapons();
            _playerInstance.SetActive(false);
        }
    }

    /// <summary>
    /// 激活玩家并传送到指定位置
    /// </summary>
    /// <param name="worldPos">世界坐标位置</param>
    public void ReactivatePlayer(Vector3 worldPos)
    {
        if (_playerInstance == null)
        {
            CreatePlayer(worldPos);
            return;
        }

        _playerInstance.SetActive(true);
        _playerInstance.transform.position = worldPos;

        // 重新设置摄像机跟随
        CameraManager.Instance.Follow(_playerInstance.transform);
    }
}
