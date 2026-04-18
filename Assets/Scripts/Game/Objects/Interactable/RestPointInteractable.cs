using UnityEngine;
using Game.Event;
using LcIcemFramework.Core;

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

    private void Start()
    {
        if (_interactable == null)
        {
            _interactable = GetComponent<Interactable>();
        }

        if (_interactable != null)
        {
            _interactable.SetHintText("按[{0}]休息");
            _interactable.OnInteract += OnInteract;
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

                // 通知UI更新
                EventCenter.Instance.Publish(GameEventID.UpdateHeartDisplay, player.RuntimeData);
            }
        }
    }

    private void SaveCheckpoint()
    {
        // 创建checkpoint快照（用于中途退出后继续游玩）
        var levelController = FindAnyObjectByType<LevelController>();
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
    }
}
