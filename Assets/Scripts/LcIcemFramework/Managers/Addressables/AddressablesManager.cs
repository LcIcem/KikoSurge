using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.AddressableAssets;

using LcIcemFramework.Core;
using UA = UnityEngine.AddressableAssets;

namespace LcIcemFramework
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
        /// 同步加载单个资源（首次阻塞，后续直接返回）
        /// </summary>
        /// <param name="address">资源的 Address 标识</param>
        /// <returns>加载的资源，失败返回 null</returns>
        public T Load<T>(string address) where T : Object
        {
            var handle = UA.Addressables.LoadAssetAsync<T>(address);
            handle.WaitForCompletion();
            if (handle.Status != AsyncOperationStatus.Succeeded)
            {
                LogError($"资源加载失败: {address}");
                return null;
            }
            return handle.Result;
        }

        /// <summary>
        /// 异步加载单个资源（协程回调版）
        /// </summary>
        /// <param name="address">资源的 Address 标识</param>
        /// <param name="onComplete">加载完成回调，参数为加载的资源，失败返回 null</param>
        public void LoadAsync<T>(string address, UnityAction<T> onComplete) where T : Object
        {
            StartCoroutine(LoadAssetCoroutine(address, onComplete));
        }

        private IEnumerator LoadAssetCoroutine<T>(string address, UnityAction<T> onComplete) where T : Object
        {
            var handle = UA.Addressables.LoadAssetAsync<T>(address);
            yield return handle;

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                onComplete?.Invoke(handle.Result);
            }
            else
            {
                LogError($"资源加载失败: {address}");
                onComplete?.Invoke(null);
            }
        }

        /// <summary>
        /// 按标签批量加载资源（协程回调版）
        /// </summary>
        /// <param name="label">标签名</param>
        /// <param name="onComplete">加载完成回调，参数为资源列表，失败返回 null</param>
        public void LoadByLabelAsync<T>(string label, UnityAction<IList<T>> onComplete) where T : Object
        {
            StartCoroutine(LoadByLabelCoroutine(label, onComplete));
        }

        private IEnumerator LoadByLabelCoroutine<T>(string label, UnityAction<IList<T>> onComplete) where T : Object
        {
            var handle = UA.Addressables.LoadAssetsAsync<T>(label, null);
            yield return handle;

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                onComplete?.Invoke(handle.Result);
            }
            else
            {
                LogError($"标签加载失败: {label}");
                onComplete?.Invoke(null);
            }
        }

        /// <summary>
        /// 按地址 + 标签过滤批量加载（AND 逻辑，协程回调版）
        /// </summary>
        /// <param name="address">资源地址</param>
        /// <param name="label">标签名（如武器品质 "Rare"）</param>
        /// <param name="onComplete">加载完成回调，参数为过滤后的资源列表，失败返回 null</param>
        public void LoadAsync<T>(string address, string label, UnityAction<IList<T>> onComplete) where T : Object
        {
            StartCoroutine(LoadByAddressAndLabelCoroutine(address, label, onComplete));
        }

        private IEnumerator LoadByAddressAndLabelCoroutine<T>(string address, string label, UnityAction<IList<T>> onComplete) where T : Object
        {
            // 先按地址加载所有资源
            var addrHandle = UA.Addressables.LoadAssetsAsync<T>(address, null);
            yield return addrHandle;

            if (addrHandle.Status != AsyncOperationStatus.Succeeded)
            {
                LogError($"地址+标签加载失败: {address} + {label}");
                onComplete?.Invoke(null);
                yield break;
            }

            IList<T> all = addrHandle.Result;

            if (string.IsNullOrEmpty(label))
            {
                onComplete?.Invoke(all);
                yield break;
            }

            // 按标签过滤
            var labelHandle = UA.Addressables.LoadAssetsAsync<T>(label, null);
            yield return labelHandle;

            var filtered = new List<T>();
            if (labelHandle.Status == AsyncOperationStatus.Succeeded)
            {
                var labelNames = new HashSet<string>();
                foreach (var r in labelHandle.Result)
                    labelNames.Add(r.name);

                foreach (var r in all)
                    if (labelNames.Contains(r.name))
                        filtered.Add(r);
            }

            UA.Addressables.Release(addrHandle);
            UA.Addressables.Release(labelHandle);
            onComplete?.Invoke(filtered);
        }

        /// <summary>
        /// 异步实例化 Prefab（协程回调版）
        /// </summary>
        /// <param name="address">Prefab 的 Address</param>
        /// <param name="parent">父 Transform，可为 null</param>
        /// <param name="onComplete">实例化完成回调，失败返回 null</param>
        public void InstantiateAsync(string address, Transform parent, UnityAction<GameObject> onComplete)
        {
            StartCoroutine(InstantiateCoroutine(address, parent, onComplete));
        }

        private IEnumerator InstantiateCoroutine(string address, Transform parent, UnityAction<GameObject> onComplete)
        {
            var handle = UA.Addressables.InstantiateAsync(address, parent);
            yield return handle;

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                onComplete?.Invoke(handle.Result);
            }
            else
            {
                LogError($"Prefab 实例化失败: {address}");
                onComplete?.Invoke(null);
            }
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
