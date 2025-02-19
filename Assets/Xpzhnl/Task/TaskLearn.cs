using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace LearnUnity
{
    public class TaskLearn : MonoBehaviour
    {
        // 使用示例
        async void Start()
        {
            Debug.Log("开始加载场景");
            await ResourceLoader.LoadSceneAsync("Xpzhnl/Tips/TipsScene");
            Debug.Log("场景加载成功");
        }

    }

    public static class DelayClass
    {
        public static async void Delay(int delay1, int delay2)
        {
            for (int i = 0; i < 5; i++)
            {
                if (i == 0) await Task.Delay(delay1);
                else await Task.Delay(delay2);
            }
        }
    }
    public static class ResourceLoader
    {
        private static async Task<T> LoadAsync<T>(string path, System.Func<string, AsyncOperation> loadFunc) where T : class
        {
            var tcs = new TaskCompletionSource<T>();
            var asyncOp = loadFunc(path);
            asyncOp.completed += _ =>
            {
                if (asyncOp is AssetBundleCreateRequest assetBundleRequest && assetBundleRequest.assetBundle != null)
                    tcs.SetResult(assetBundleRequest.assetBundle as T);
                else if (asyncOp is AsyncOperation)
                    tcs.SetResult(true as T);
                else
                    tcs.SetException(new System.Exception($"Failed to load: {path}"));
            };
            return await tcs.Task;
        }

        public static Task<AssetBundle> LoadAssetBundleAsync(string path)
        {
            return LoadAsync<AssetBundle>(path, p => AssetBundle.LoadFromFileAsync(p));
        }

        public static Task LoadSceneAsync(string sceneName)
        {
            return LoadAsync<object>(sceneName, p => UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(p));
        }

        public static Task UnloadSceneAsync(string sceneName)
        {
            return LoadAsync<object>(sceneName, p => UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(p));
        }
    }
}

