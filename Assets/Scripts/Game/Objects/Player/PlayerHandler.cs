using System.Collections;
using System.Collections.Generic;
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
    /// 创建玩家并放置到指定位置（异步等待 RoleStaticData 加载）
    /// </summary>
    /// <param name="worldPos">世界坐标位置</param>
    /// <param name="existingData">已有玩家数据（大厅玩家用 null 会自动创建基础数据）</param>
    /// <param name="isLobbyPlayer">是否为大厅玩家（大厅玩家不显示 HubPanel）</param>
    public void CreatePlayer(Vector3 worldPos, PlayerRuntimeData existingData = null, bool isLobbyPlayer = false)
    {
        MonoManager.Instance.StartCoroutine(CreatePlayerAfterDataLoaded(worldPos, existingData, isLobbyPlayer));
    }

    private IEnumerator CreatePlayerAfterDataLoaded(Vector3 worldPos, PlayerRuntimeData existingData, bool isLobbyPlayer)
    {
        // 等待 RoleStaticData 加载完成
        while (!GameDataManager.Instance.IsRoleStaticDataLoaded)
        {
            yield return new WaitForSeconds(0.1f);
        }

        if (_playerInstance != null)
            Object.Destroy(_playerInstance);

        // 确定角色数据
        // 继续游戏时：使用 SessionManager 中的 selectedRoleId（从存档恢复）
        // 新游戏/大厅玩家：使用 UI 当前选择的角色
        RoleStaticData roleData;
        if (existingData != null && SessionManager.Instance.HasActiveSession)
        {
            // 继续游戏：使用存档中的 roleId
            int savedRoleId = SessionManager.Instance.CurrentSession.selectedRoleId;
            roleData = GameDataManager.Instance.GetRoleStaticData(savedRoleId);
        }
        else
        {
            // 新游戏或大厅玩家：使用 UI 当前选择的角色
            roleData = GameDataManager.Instance.GetRoleStaticDataByCurSel();
        }

        if (roleData == null)
        {
            Debug.LogError("[PlayerHandler] Role data not found! Using default.");
            roleData = GameDataManager.Instance.GetDefaultRoleStaticData();
        }

        _playerPrefabs = roleData.prefab;

        // 确定玩家运行时数据
        PlayerRuntimeData playerData;
        List<int> weaponIds;

        if (existingData != null)
        {
            // 已有数据：直接使用（用于游戏玩家继续游戏时从 SessionManager 获取的计算后数据）
            playerData = existingData;
            weaponIds = SessionManager.Instance.GetEquippedWeaponSlots().ConvertAll(slot => slot.itemId);
        }
        else if (isLobbyPlayer)
        {
            // 大厅玩家：创建基础数据（不含加成，用于角色选择界面展示）
            playerData = PlayerRuntimeData.CreateBasic(roleData);
            weaponIds = roleData.initialWeaponIds;
        }
        else
        {
            // 游戏玩家（无 existingData）：使用 SessionManager 获取完整数据
            playerData = SessionManager.Instance.GetPlayerData();
            weaponIds = SessionManager.Instance.GetEquippedWeaponSlots().ConvertAll(slot => slot.itemId);
            if (playerData == null)
            {
                Debug.LogWarning("[PlayerHandler] No player data from SessionManager, creating basic data");
                playerData = PlayerRuntimeData.CreateBasic(roleData);
                weaponIds = roleData.initialWeaponIds;
            }
        }

        _playerInstance = Object.Instantiate(_playerPrefabs, worldPos, Quaternion.identity);

        // 初始化玩家运行时数据
        var player = _playerInstance.GetComponent<Player>();
        player.Initialize(playerData, weaponIds);

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
                if (player.weaponHandler.CurrentWeapon != null)
                {
                    EventCenter.Instance.Publish(GameEventID.OnCurrentWeaponChanged, player.weaponHandler.CurrentWeapon);
                }
            });
        }
    }

    /// <summary>
    /// 暂时失活玩家（不销毁，武器保持）
    /// </summary>
    public void DeactivatePlayer()
    {
        if (_playerInstance != null)
        {
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
        if (_playerInstance != null)
        {
            UnityEngine.Object.Destroy(_playerInstance);
            _playerInstance = null;
        }
    }
}
