using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.AddressableAssets;

using LcIcemFramework.Core;
using UA = UnityEngine.AddressableAssets;

namespace LcIcemFramework.Managers.Addressables
{
    /// <summary>
    /// Addressables 资源管理器
    /// 封装所有 Addressables 操作，Editor 配置决定路径（Groups + Profiles）
    /// </summary>
    public class AddressablesManager : SingletonMono<AddressablesManager>
    {
        protected override void Init() { }

        private void Log(string msg) => Debug.Log($"[AddressablesManager] {msg}");
        private void LogError(string msg) => Debug.LogError($"[AddressablesManager] {msg}");

        // ── 初始化 ──────────────────────────────────────────────

        /// <summary>
        /// 初始化 Addressables 系统。
        /// Editor 配置决定所有路径：
        /// - Local Group：Build Path = LocalBuildPath, Load Path = LocalLoadPath
        ///   → 构建时自动复制到 StreamingAssets，首包内置
        /// - Remote Group：Build Path = RemoteBuildPath, Load Path = RemoteLoadPath
        ///   → Profile 控制 URL（Dev=localhost / Release=CDN）
        /// </summary>
        public AsyncOperationHandle Initialize()
        {
            var handle = UA.Addressables.InitializeAsync();
            Log("Addressables 初始化完成");
            return handle;
        }

        // ── 热更新（可选，KikoSurge 纯单机可跳过）──────────────

        /// <summary>
        /// 检查并下载热更新（协程)。
        /// 流程：CheckForCatalogUpdates → UpdateCatalogs → GetDownloadSize → DownloadDependenciesAsync
        /// </summary>
        /// <param name="onProgress">下载进度回调（0~1）</param>
        /// <param name="onComplete">完成回调，参数表示是否有更新</param>
        public void CheckForUpdates(UnityAction<float> onProgress = null, UnityAction<bool> onComplete = null)
        {
            StartCoroutine(CheckAndDownload(onProgress, onComplete));
        }

        private IEnumerator CheckAndDownload(UnityAction<float> onProgress, UnityAction<bool> onComplete)
        {
            // [1] 检查 Catalog 更新
            var check = UA.Addressables.CheckForCatalogUpdates(false);
            yield return check;

            if (check.Status != AsyncOperationStatus.Succeeded)
            {
                LogError($"Catalog 检查失败: {check.OperationException}");
                onComplete?.Invoke(false);
                yield break;
            }

            if (check.Result.Count == 0)
            {
                Log("没有检测到更新");
                onComplete?.Invoke(false);
                yield break;
            }

            Log($"检测到 {check.Result.Count} 个 Catalog 需要更新");

            // [2] 更新 Catalog
            var update = UA.Addressables.UpdateCatalogs(check.Result, false);
            yield return update;

            if (update.Status != AsyncOperationStatus.Succeeded)
            {
                LogError($"Catalog 更新失败: {update.OperationException}");
                onComplete?.Invoke(false);
                yield break;
            }

            // [3] 获取需要下载的资源总大小
            var size = UA.Addressables.GetDownloadSizeAsync(update.Result);
            yield return size;

            if (size.Result == 0)
            {
                Log("Catalog 已更新，无需下载新资源");
                UA.Addressables.Release(check);
                UA.Addressables.Release(update);
                UA.Addressables.Release(size);
                onComplete?.Invoke(true);
                yield break;
            }

            Log($"需要下载 {size.Result / 1024.0 / 1024.0:F2} MB");

            // [4] 下载所有更新资源
            var download = UA.Addressables.DownloadDependenciesAsync(update.Result);
            while (!download.IsDone)
            {
                onProgress?.Invoke(download.PercentComplete);
                yield return new WaitForEndOfFrame();
            }

            onProgress?.Invoke(1f);

            if (download.Status == AsyncOperationStatus.Succeeded)
            {
                Log("热更新完成");
                UA.Addressables.Release(check);
                UA.Addressables.Release(update);
                UA.Addressables.Release(size);
                UA.Addressables.Release(download);
                onComplete?.Invoke(true);
            }
            else
            {
                LogError($"下载失败: {download.OperationException}");
                onComplete?.Invoke(false);
            }
        }

        // ── 场景加载 ────────────────────────────────────────────

        /// <summary>
        /// 异步加载场景（协程，传入回调）
        /// </summary>
        /// <param name="sceneName">场景名称（Addressables 中的 address）</param>
        /// <param name="onProgress">进度回调（0~1）</param>
        /// <param name="onComplete">完成回调，参数为场景实例</param>
        public void LoadSceneAsync(string sceneName, UnityAction<float> onProgress = null, UnityAction<SceneInstance> onComplete = null)
        {
            var op = UA.Addressables.LoadSceneAsync(sceneName);
            StartCoroutine(LoadSceneCoroutine(op, onProgress, onComplete, sceneName));
        }

        /// <summary>
        /// 异步加载场景（Task 版本）
        /// </summary>
        public async Task<SceneInstance> LoadSceneAsync(string sceneName)
        {
            var handle = UA.Addressables.LoadSceneAsync(sceneName);
            SceneInstance result = await handle.Task;
            if (handle.Status != AsyncOperationStatus.Succeeded)
                LogError($"场景加载失败: {sceneName}");
            return result;
        }

        private IEnumerator LoadSceneCoroutine(
            AsyncOperationHandle<SceneInstance> op,
            UnityAction<float> onProgress,
            UnityAction<SceneInstance> onComplete,
            string sceneName)
        {
            // 进度到 0.9 后等待 allowSceneActivation
            while (op.PercentComplete < 0.9f)
            {
                onProgress?.Invoke(op.PercentComplete / 0.9f);
                yield return new WaitForEndOfFrame();
            }

            onProgress?.Invoke(1f);
            yield return new WaitUntil(() => op.IsDone);

            if (op.Status != AsyncOperationStatus.Succeeded)
            {
                LogError($"场景加载失败: {sceneName}");
                yield break;
            }

            onComplete?.Invoke(op.Result);
            Log($"加载场景: {sceneName}");
        }

        /// <summary>
        /// 异步卸载场景（协程回调版）
        /// </summary>
        public void UnloadSceneAsync(SceneInstance sceneInstance, UnityAction onComplete = null)
        {
            var op = UA.Addressables.UnloadSceneAsync(sceneInstance);
            StartCoroutine(UnloadSceneCoroutine(op, onComplete));
        }

        private IEnumerator UnloadSceneCoroutine(AsyncOperationHandle op, UnityAction onComplete)
        {
            yield return new WaitUntil(() => op.IsDone);
            if (op.Status != AsyncOperationStatus.Succeeded)
                LogError($"场景卸载失败");
            else
                Log("卸载场景完成");
            onComplete?.Invoke();
        }

        // ── 通用资源加载 ─────────────────────────────────────────

        /// <summary>
        /// 异步加载单个资源
        /// </summary>
        /// <param name="address">资源的 Address 标识</param>
        public async Task<T> LoadAsync<T>(string address) where T : Object
        {
            var handle = UA.Addressables.LoadAssetAsync<T>(address);
            T result = await handle.Task;
            if (handle.Status != AsyncOperationStatus.Succeeded)
            {
                LogError($"资源加载失败: {address}");
                return null;
            }
            return result;
        }

        /// <summary>
        /// 按标签批量加载资源
        /// </summary>
        /// <param name="label">标签名</param>
        public async Task<IList<T>> LoadByLabelAsync<T>(string label) where T : Object
        {
            var handle = UA.Addressables.LoadAssetsAsync<T>(label, null);
            IList<T> result = await handle.Task;
            if (handle.Status != AsyncOperationStatus.Succeeded)
            {
                LogError($"标签加载失败: {label}");
                return null;
            }
            return result;
        }

        /// <summary>
        /// 按地址 + 标签过滤批量加载（AND 逻辑）
        /// </summary>
        /// <param name="address">资源地址</param>
        /// <param name="label">标签名（如武器品质 "Rare"）</param>
        public async Task<IList<T>> LoadAsync<T>(string address, string label) where T : Object
        {
            // 先按地址加载所有资源，再按标签过滤
            var addrHandle = UA.Addressables.LoadAssetsAsync<T>(address, null);
            IList<T> all = await addrHandle.Task;

            if (addrHandle.Status != AsyncOperationStatus.Succeeded)
            {
                LogError($"地址+标签加载失败: {address} + {label}");
                return null;
            }

            if (string.IsNullOrEmpty(label))
            {
                UA.Addressables.Release(addrHandle);
                return all;
            }

            // 按标签过滤（需要先获取标签对应的资源，再取交集）
            var labelHandle = UA.Addressables.LoadAssetsAsync<T>(label, null);
            IList<T> labelResult = await labelHandle.Task;

            var filtered = new List<T>();
            if (labelHandle.Status == AsyncOperationStatus.Succeeded)
            {
                var labelNames = new HashSet<string>();
                foreach (var r in labelResult)
                    labelNames.Add(r.name);

                foreach (var r in all)
                    if (labelNames.Contains(r.name))
                        filtered.Add(r);
            }

            UA.Addressables.Release(addrHandle);
            UA.Addressables.Release(labelHandle);
            return filtered;
        }

        /// <summary>
        /// 异步实例化 Prefab
        /// </summary>
        public async Task<GameObject> InstantiateAsync(string address, Transform parent = null)
        {
            var handle = UA.Addressables.InstantiateAsync(address, parent);
            GameObject result = await handle.Task;
            if (handle.Status != AsyncOperationStatus.Succeeded)
            {
                LogError($"Prefab 实例化失败: {address}");
                return null;
            }
            return result;
        }

        // ── 释放 ────────────────────────────────────────────────

        /// <summary>
        /// 释放资源（引用归零时可卸载）
        /// </summary>
        public void Release<T>(AsyncOperationHandle<T> handle) where T : Object
        {
            UA.Addressables.Release(handle);
        }

        /// <summary>
        /// 释放实例化的 GameObject
        /// </summary>
        public void ReleaseInstance(GameObject instance)
        {
            if (instance == null) return;
            UA.Addressables.ReleaseInstance(instance);
        }

        /// <summary>
        /// 释放通用 AsyncOperationHandle
        /// </summary>
        public void Release(AsyncOperationHandle handle)
        {
            UA.Addressables.Release(handle);
        }
    }
}
