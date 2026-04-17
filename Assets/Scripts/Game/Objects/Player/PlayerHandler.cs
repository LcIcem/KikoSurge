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
    /// <param name="existingData">已有玩家数据（大厅玩家用全局数据，游戏玩家传 null）</param>
    /// <param name="isLobbyPlayer">是否为大厅玩家（大厅玩家不设置相机跟随和 HubPanel）</param>
    public void CreatePlayer(Vector3 worldPos, PlayerData existingData = null, bool isLobbyPlayer = false)
    {
        MonoManager.Instance.StartCoroutine(CreatePlayerAfterDataLoaded(worldPos, existingData, isLobbyPlayer));
    }

    private IEnumerator CreatePlayerAfterDataLoaded(Vector3 worldPos, PlayerData existingData, bool isLobbyPlayer)
    {
        // 等待 RoleInfo 加载完成
        while (!GameDataManager.Instance.IsRoleInfoLoaded)
        {
            yield return new WaitForSeconds(0.1f);
        }

        if (_playerInstance != null)
            Object.Destroy(_playerInstance);

        _playerPrefabs = GameDataManager.Instance.GetRoleDataByCurSel().prefabs;

        // 从 RoleInfo 初始化 PlayerData
        PlayerData playerData;
        if (existingData != null)
        {
            // 大厅玩家：复用已有数据（全局玩家数据）
            playerData = existingData;
        }
        else
        {
            // 游戏玩家：从角色配置创建新数据
            playerData = GameDataManager.Instance.GetRoleDataByCurSel().ConvertToPlayerData();
        }
        GameDataManager.Instance.PlayerData = playerData;

        _playerInstance = Object.Instantiate(_playerPrefabs, worldPos, Quaternion.identity);

        // 相机跟随（大厅和游戏玩家都需要）
        CameraManager.Instance.Follow(_playerInstance.transform);

        if (isLobbyPlayer)
        {
            // 大厅玩家：不显示 HubPanel
            Debug.Log("[PlayerHandler] Lobby player created (no UI setup)");
        }
        else
        {
            // 游戏玩家：显示 HubPanel
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

    /// <summary>
    /// 销毁玩家（由外部调用，如 RestartGame）
    /// </summary>
    public void DestroyPlayer()
    {
        Debug.Log($"[PlayerHandler] DestroyPlayer called. _playerInstance = {_playerInstance?.name ?? "null"}");
        if (_playerInstance != null)
        {
            Debug.Log($"[PlayerHandler] Destroying player: {_playerInstance.name}");
            Object.Destroy(_playerInstance);
            _playerInstance = null;
        }
        else
        {
            Debug.Log("[PlayerHandler] _playerInstance is null, nothing to destroy");
        }
    }
}
