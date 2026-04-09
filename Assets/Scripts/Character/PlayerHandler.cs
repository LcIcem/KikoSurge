using System.Collections;
using System.Collections.Generic;
using LcIcemFramework.Camera;
using ProcGen.Core;
using UnityEngine;
using UnityEngine.Tilemaps;

public class PlayerHandler : MonoBehaviour
{
    [SerializeField] private GameObject _playerPrefabs;
    [SerializeField] private Tilemap _floorTilemap;
    [SerializeField] private GameEntry _gameEntry;

    private GameObject _playerInstance;

    private DungeonGraph _currentGraph;

    void Awake()
    {
        if (_gameEntry.IsBuildCompleted)
            _currentGraph = _gameEntry.dungeonGraph;
    }

    // void Start()
    // {
    //     StartCoroutine(placePlayerAfterBuildCompleted());
    // }

    public void RegeneratePlayer()
    {
        StartCoroutine(placePlayerAfterBuildCompleted());
    }
    
    private IEnumerator placePlayerAfterBuildCompleted()
    {
        while (!_gameEntry.IsBuildCompleted)
        {
            yield return new WaitForSeconds(0.1f);
        }
        _currentGraph = _gameEntry.dungeonGraph;
        PlacePlayer();
    }

    private void PlacePlayer()
    {
        if (_playerInstance != null)
            Destroy(_playerInstance);

        Vector2Int? startPos = GetStartPos();

        if (startPos.HasValue)
        {
            Vector3 worldPos = _floorTilemap.CellToWorld(new Vector3Int(startPos.Value.x, startPos.Value.y, 0));
            _playerInstance = Instantiate(_playerPrefabs, worldPos, Quaternion.identity);
            Camera.main.GetComponent<CameraController>().target = _playerInstance.transform;
        }
    }

    private Vector2Int? GetStartPos()
    {
        if (_currentGraph == null)
            return null;
        Room startRoom = _currentGraph.GetRoom(_currentGraph.startRoomId);
        return startRoom.Center;
    }
}
