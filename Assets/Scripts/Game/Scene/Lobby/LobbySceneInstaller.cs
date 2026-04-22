using UnityEngine;
using LcIcemFramework;
using LcIcemFramework.Core;
using System.Collections;

/// <summary>
/// 大厅场景初始化器
/// <para>挂载在大厅场景根对象上，负责创建大厅玩家</para>
/// </summary>
public class LobbySceneInstaller : MonoBehaviour
{
    [Header("出生点")]
    [SerializeField] private Transform _lobbySpawnPoint;

    private void Start()
    {
        StartCoroutine(TryCreateLobbyPlayer());
    }

    private IEnumerator TryCreateLobbyPlayer()
    {
        if (GameLifecycleManager.Instance == null)
        {
            Debug.LogWarning("[LobbySceneInstaller] GameLifecycleManager not ready, retrying next frame...");
            yield return new WaitForSeconds(0.1f);
            StartCoroutine(TryCreateLobbyPlayer());
            yield break;
        }

        Vector3 spawnPos = _lobbySpawnPoint != null ? _lobbySpawnPoint.position : Vector3.zero;
        GameLifecycleManager.Instance.CreateLobbyPlayer(spawnPos);
    }
}
