using UnityEngine;
using Game.Event;
using LcIcemFramework.Core;
using LcIcemFramework;

/// <summary>
/// 篝火点交互脚本
/// <para>挂载在带有 Interactable 组件的篝火点上</para>
/// <para>功能：回血 + 保存 checkpoint（用于中途退出后继续游玩）</para>
/// <para>注意：此checkpoint仅用于「中途退出后继续」，死亡后session结束，不会从checkpoint复活</para>
public class RestPointInteractable : MonoBehaviour
{
    [Header("回血配置")]
    [SerializeField] private float _healAmount = 50f;

    [Header("交互配置")]
    [SerializeField] private Interactable _interactable;

    [Header("环境音效配置")]
    [SerializeField] private AudioClip _ambientSFX;

    private int _myRoomId = -1;
    private int _currentPlayerRoomId = -1;
    private bool _isPlayerInMyRoom;

    private void Start()
    {
        if (_interactable == null)
        {
            _interactable = GetComponent<Interactable>();
        }

        if (_interactable != null)
        {
            _interactable.SetHintText("休息[{0}]");
            _interactable.OnInteract += OnInteract;
        }

        // 获取自己所在房间的ID
        var levelController = FindFirstObjectByType<LevelController>();
        if (levelController != null)
        {
            var tileData = levelController.GetTileData();
            if (tileData != null)
            {
                Vector2Int gridPos = Vector2Int.FloorToInt(transform.position);
                _myRoomId = tileData.GetRoomIdAt(gridPos);
            }
        }

        // 订阅房间事件
        EventCenter.Instance.Subscribe<RoomEnterParams>(GameEventID.OnRoomEnter, OnRoomEnter);
        EventCenter.Instance.Subscribe<CorridorEnterParams>(GameEventID.OnCorridorEnter, OnCorridorEnter);
    }

    private void OnRoomEnter(RoomEnterParams p)
    {
        _currentPlayerRoomId = p.roomId;

        if (p.roomId == _myRoomId && !_isPlayerInMyRoom)
        {
            // 玩家进入自己所在房间，播放音效
            _isPlayerInMyRoom = true;
            if (_ambientSFX != null)
            {
                ManagerHub.Audio.PlayAmbient(_ambientSFX);
            }
        }
        else if (_isPlayerInMyRoom && p.roomId != _myRoomId)
        {
            // 玩家离开自己所在房间，停止音效
            _isPlayerInMyRoom = false;
            ManagerHub.Audio.StopAmbient();
        }
    }

    private void OnCorridorEnter(CorridorEnterParams p)
    {
        if (_isPlayerInMyRoom)
        {
            // 玩家进入走廊，停止音效
            _isPlayerInMyRoom = false;
            ManagerHub.Audio.StopAmbient();
        }
    }

    private void OnInteract()
    {
        // 隐藏 InfoCard
        _interactable.ShowInfoCard(false);

        // 回血
        HealPlayer();

        // 保存 checkpoint
        SaveCheckpoint();
    }

    private void HealPlayer()
    {
        // 通过 GameObject.FindWithTag 找到玩家
        var playerGo = GameObject.FindWithTag("Player");
        if (playerGo != null)
        {
            var player = playerGo.GetComponent<Player>();
            if (player != null)
            {
                // 通过 RuntimeData 直接增加生命值（setter会自动clamp）
                float currentHealth = player.RuntimeData.Health;
                float newHealth = Mathf.Min(currentHealth + _healAmount, player.RuntimeData.maxHealth);
                player.RuntimeData.Health = newHealth;

                // 同步生命值到 SessionManager（用于检查点保存）
                SessionManager.Instance?.SetPlayerHealth(newHealth);

                // 通知UI更新（使用 Player._playerData 引用）
                EventCenter.Instance.Publish(GameEventID.UpdateHeartDisplay, player.RuntimeData);
            }
        }
    }

    private void SaveCheckpoint()
    {
        // 创建checkpoint快照（用于中途退出后继续游玩）
        var levelController = FindFirstObjectByType<LevelController>();
        if (levelController != null)
        {
            var snapshot = levelController.CreateCheckpointSnapshot();
            if (snapshot != null)
            {
                SessionManager.Instance.SaveCheckpoint(snapshot);
            }
        }
    }

    private void OnDestroy()
    {
        if (_interactable != null)
        {
            _interactable.OnInteract -= OnInteract;
        }

        EventCenter.Instance.Unsubscribe<RoomEnterParams>(GameEventID.OnRoomEnter, OnRoomEnter);
        EventCenter.Instance.Unsubscribe<CorridorEnterParams>(GameEventID.OnCorridorEnter, OnCorridorEnter);
    }
}
