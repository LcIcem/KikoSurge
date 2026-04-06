using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;
using UnityEngine.AddressableAssets;

using LcIcemFramework.Core;
using LcIcemFramework.Managers.Addressables;
using UA = UnityEngine.AddressableAssets;

namespace LcIcemFramework.Managers.Scenes
{
    /// <summary>
    /// 场景切换管理器（基于 Addressables.LoadSceneAsync）
    /// <para>
    /// 注意：类名为 GameSceneManager 以避免与 UnityEngine.SceneManagement.SceneManager 冲突。
    /// </para>
    /// </summary>
    public class GameSceneManager : SingletonMono<GameSceneManager>
    {
        // 当前场景实例 (用于卸载场景时使用)
        private SceneInstance _currentSceneInstance;

        protected override void Init() { }

        /// <summary>
        /// 异步加载场景
        /// </summary>
        /// <param name="sceneName">场景名称</param>
        /// <param name="onProgress">0.0~1.0 进度回调，用于 Loading 进度条</param>
        /// <param name="onComplete">加载完成回调</param>
        public void LoadSceneAsync(string sceneName, UnityAction<float> onProgress = null, UnityAction onComplete = null)
        {
            // 将timeScale设为1
            Time.timeScale = 1f;
            // 通过 Addressables 加载场景
            var op = UA.Addressables.LoadSceneAsync(sceneName);
            StartCoroutine(LoadSceneCoroutine(op, onProgress, onComplete, sceneName));
        }

        // 异步加载场景协程
        private IEnumerator LoadSceneCoroutine(
            AsyncOperationHandle<SceneInstance> op,
            UnityAction<float> onProgress,
            UnityAction onComplete,
            string sceneName)
        {
            // 轮询进度，直到加载到 0.9（剩下 0.1 是 allowSceneActivation）
            while (op.PercentComplete < 0.9f)
            {
                onProgress?.Invoke(op.PercentComplete / 0.9f);
                yield return new WaitForEndOfFrame();
            }

            // 等待激活完成
            onProgress?.Invoke(1f);
            yield return new WaitUntil(() => op.IsDone);

            if (op.Status != AsyncOperationStatus.Succeeded)
            {
                LogError($"场景加载失败: {sceneName}");
                yield break;
            }

            // 保存场景实例，供后续 UnloadCurrentScene 使用
            _currentSceneInstance = op.Result;

            onComplete?.Invoke();
            Log($"加载场景: {sceneName}");
        }

        /// <summary>
        /// 同步加载（会卡主线程，仅用于调试）
        /// </summary>
        /// <param name="sceneName"></param>
        public void LoadScene(string sceneName)
        {
            SceneManager.LoadScene(sceneName);
        }

        /// <summary>
        /// 获取当前场景名称
        /// </summary>
        public string GetCurrentScene()
        {
            return SceneManager.GetActiveScene().name;
        }

        /// <summary>
        /// 异步卸载当前场景
        /// </summary>
        public AsyncOperationHandle UnloadCurrentScene()
        {
            if (!_currentSceneInstance.Equals(default))
            {
                var op = UA.Addressables.UnloadSceneAsync(_currentSceneInstance);
                _currentSceneInstance = default;
                return op;
            }
            LogWarning("没有可卸载的场景实例");
            return default;
        }

        #region 日志
        private void Log(string msg) => Debug.Log($"[GameSceneManager] {msg}");
        private void LogWarning(string msg) => Debug.LogWarning($"[GameSceneManager] {msg}");
        private void LogError(string msg) => Debug.LogError($"[GameSceneManager] {msg}");
        #endregion
    }
}
