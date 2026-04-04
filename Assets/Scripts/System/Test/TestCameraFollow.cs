using UnityEngine;

/// <summary>
/// 相机跟随测试脚本
/// 挂载在 Player 上，按 WASD / 方向键移动
/// </summary>
public class TestCameraFollow : MonoBehaviour
{
    [Header("移动")]
    public float moveSpeed = 5f;

    [Header("相机引用")]
    // 在 Inspector 中拖入场景中的 CameraController
    public CameraController cameraController;

    [Header("效果触发")]
    public KeyCode impactRandomKey = KeyCode.Q;
    public KeyCode impactDirKey = KeyCode.E;
    public KeyCode spiralKey = KeyCode.Space;
    public KeyCode zoomKey = KeyCode.Z;

    private void Update()
    {
        // 移动
        Vector2 input = InputMgr.Instance.MoveInput;
        transform.Translate(new Vector3(input.x, input.y) * (moveSpeed * Time.deltaTime));

        if (cameraController == null) return;

        if (Input.GetKeyDown(impactRandomKey)) cameraController.ImpactRandom();
        if (Input.GetKeyDown(impactDirKey)) cameraController.ImpactDir(ImpactDirection.Up);
        if (Input.GetKeyDown(spiralKey))    cameraController.Spiral();
        if (Input.GetKeyDown(zoomKey)) cameraController.Zoom();

        this.transform.rotation = cameraController.transform.rotation;
    }
}
