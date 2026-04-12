using System.Collections;
using System.Collections.Generic;
using LcIcemFramework.Camera;
using LcIcemFramework.Core;
using LcIcemFramework.Managers;
using LcIcemFramework.Managers.UI;
using ProcGen.Core;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// 玩家处理类
/// <para>用于对接玩家与游戏关卡</para>
/// </summary>
public class PlayerHandler : MonoBehaviour
{
    [SerializeField] private Tilemap _floorTilemap;
    [SerializeField] private GameEntry _gameEntry;
    [SerializeField] private CinemachineCamera _cinemachineCamera;

    // 玩家相关
    private GameObject _playerPrefabs;
    private GameObject _playerInstance;
    private PlayerData _playerData;
    private bool _isFirstPlace = true;

    // 当前地图信息
    private DungeonGraph _currentGraph;

    // void Start()
    // {
    //     StartCoroutine(placePlayerAfterBuildCompleted());
    // }

    // 再次生成玩家（启动协程）
    public void RegeneratePlayer()
    {
        StartCoroutine(placePlayerAfterDataPrepared());
    }

    // 数据准备好后放置玩家 协程
    private IEnumerator placePlayerAfterDataPrepared()
    {
        // 等待地图构建完毕
        while (!_gameEntry.IsBuildCompleted)
        {
            yield return new WaitForSeconds(0.1f);
        }
        _currentGraph = _gameEntry.dungeonGraph;

        // 等待 RoleInfo 加载完成
        while (!GameDataManager.Instance.IsRoleInfoLoaded)
        {
            yield return new WaitForSeconds(0.1f);
        }

        TryPlacePlayer();
    }

    // 放置玩家到地图中
    private void TryPlacePlayer()
    {
        // 如果已有玩家实例 先销毁实例
        if (_playerInstance != null)
            Destroy(_playerInstance);

        // 加载预设体
        if (_playerPrefabs == null)
            _playerPrefabs = GameDataManager.Instance.GetRoleDataByCurSel().prefabs;

        // 如果是该局第一次放置玩家 从GameDataManager中获取选择角色的信息 并且 初始化playerData的值
        if (_isFirstPlace)
        {
            // 从GameDataManager中得到默认配置的角色数据 赋值给 实际游玩时的玩家数据
            _playerData = GameDataManager.Instance.GetRoleDataByCurSel().ConvertToPlayerData();

            // 设置GameDataManager中的 实际游玩时的玩家数据（这个操作必须先于所有要使用playerData的操作） 
            // 此操作仅在进入每场次游戏的开始时执行，玩家数据 后续在其它逻辑实时更新
            GameDataManager.Instance.PlayerData = _playerData;
            _isFirstPlace = false;
        }

        // 获取要放置的位置
        Vector2Int? startPos = GetStartPos();

        // 如果能得到该位置 实例化玩家
        if (startPos.HasValue)
        {
            // 将瓦片地图坐标 转为 世界坐标
            Vector3 worldPos = _floorTilemap.CellToWorld(new Vector3Int(startPos.Value.x, startPos.Value.y, 0));
            // 实例化玩家
            _playerInstance = Instantiate(_playerPrefabs, worldPos, Quaternion.identity);

            // 设置摄像机跟随
            // Camera.main.GetComponent<CameraController>().target = _playerInstance.transform;
            _cinemachineCamera.Follow = _playerInstance.transform;

            // 显示游戏UI
            ManagerHub.UI.ShowPanel<GamePanel>(UILayerType.Middle, (panel) =>
            {
                // 显示Gamepanel后 通知血条UI更新
                EventCenter.Instance.Publish(EventID.UpdateHeartDisplay, _playerData);
            });
        }
    }

    // 获取开始房间的中心位置坐标
    private Vector2Int? GetStartPos()
    {
        if (_currentGraph == null)
            return null;
        Room startRoom = _currentGraph.GetRoom(_currentGraph.startRoomId);
        return startRoom.Center;
    }
}
