﻿/**
 * 热更新资源下载器
 */

using UnityEngine;
using Base.Debug;
using Base.Utils;
using System.Collections;
using System.Collections.Generic;

public class ResourceUpdater : MonoBehaviour
{
    private const string VERSIONCONFIGFILE = "VersionConfig.json";
    /// <summary>
    /// 本地版本配置信息
    /// </summary>
    private VersionConfig mLocalVersionConfig;
    /// <summary>
    /// 服务器版本配置信息
    /// </summary>
    private VersionConfig mServerVersionConfig;
    /// <summary>
    /// 需要热更新下载的AssetBundle队列
    /// </summary>
    private Queue<string> mNeedDownLoadQueue = new Queue<string>();

    /// <summary>
    /// 开始更新
    /// </summary>
    private void Start()
    {
        StartCoroutine(CheckUpdate());
    }

    /// <summary>
    /// 检查更新，包括大版本换包和热更新
    /// </summary>
    private IEnumerator CheckUpdate()
    {
        // 是否开启热更检测
        if (!AppConfig.CheckVersionUpdate)
        {
            StartGame();
            yield break;
        }
        // 加载并初始化版本信息文件
        yield return InitVersion();

        // 没有版本对比文件，直接进入游戏
        if (mLocalVersionConfig == null || mServerVersionConfig == null)
        {
            StartGame();
            yield break;
        }

        // 检测版本配置文件
        if (CheckVersion(mLocalVersionConfig.Version, mServerVersionConfig.Version))
        {
            // 对比版本资源
            yield return CompareVersion();
        }
        if (mNeedDownLoadQueue.Count == 0)
        {
            StartGame();
        }
        else
        {
            DownLoadResource();
        }
    }

    /// <summary>
    /// 初始化本地版本
    /// </summary>
    IEnumerator InitVersion()
    {
        var localVersionConfigPath = PathUtil.GetLocalFilePath(VERSIONCONFIGFILE, true);
        var www = new WWW(localVersionConfigPath);
        yield return www;
        mLocalVersionConfig = JsonUtility.FromJson<VersionConfig>(www.text);
        www.Dispose();
        var serverVersionConfigPath = PathUtil.GetServerFileURL(VERSIONCONFIGFILE);
        www = new WWW(serverVersionConfigPath);
        yield return www;
        mServerVersionConfig = string.IsNullOrEmpty(www.error) ? JsonUtility.FromJson<VersionConfig>(www.text) : mLocalVersionConfig;
        www.Dispose();
    }

    /// <summary>
    /// 更新配置文件
    /// </summary>
    private void UpdateVersionConfig()
    {
        var path = PathUtil.GetPresistentDataFilePath(VERSIONCONFIGFILE);
        var text = JsonUtility.ToJson(mServerVersionConfig);
        FileUtil.WriteAllText(path, text);
    }
    
    /// <summary>
    /// 版本对比
    /// </summary>
    private bool CheckVersion(string sourceVersion, string targetVersion)
    {
        string[] sourceVers = sourceVersion.Split('.');
        string[] targetVers = targetVersion.Split('.');
        try
        {
            int sV0 = int.Parse(sourceVers[0]);
            int tV0 = int.Parse(targetVers[0]);
            int sV1 = int.Parse(sourceVers[1]);
            int tV1 = int.Parse(targetVers[1]);
            // 大版本更新
            if (tV0 > sV0)
            {
                Debugger.Log("New Version");
                return false;
            }
            // 热更新
            if (tV0 == sV0 && tV1 > sV1)
            {
                Debugger.Log("Update Res ...");
                return true;
            }
        }
        catch (System.Exception e)
        {
            Debugger.LogError(e.Message);
        }
        return false;
    }

    /// <summary>
    /// 比较版本资源
    /// </summary>
    /// <returns></returns>
    IEnumerator CompareVersion()
    {
        var manifestAssetBundlePath = StringUtil.PathConcat(AssetBundleConfig.AssetBundlesFolder, AssetBundleConfig.AssetBundlesFolder);
        // 本地的AssetBundleManifest
        var localManifestPath = PathUtil.GetLocalFilePath(manifestAssetBundlePath, true);
        var www = new WWW(localManifestPath);
        yield return www;
        var localManifest = www.assetBundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
        var localAllAssetBundles = new List<string>(localManifest.GetAllAssetBundles());
        var localAllAssetBundlesDict = new Dictionary<string, Hash128>();
        foreach(var abName in localAllAssetBundles)
        {
            localAllAssetBundlesDict.Add(abName, localManifest.GetAssetBundleHash(abName));
        }
        www.assetBundle.Unload(true);
        www.Dispose();
        // 服务器上的AssetBundleManifest
        var serverManifestPath = PathUtil.GetServerFileURL(manifestAssetBundlePath);
        www = new WWW(serverManifestPath);
        yield return www;
        if (www.assetBundle != null)
        {
            var serverManifest = www.assetBundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
            var serverAllAssetBundles = serverManifest.GetAllAssetBundles();
            foreach(var abName in serverAllAssetBundles)
            {
                if (localAllAssetBundlesDict.ContainsKey(abName))
                {
                    var serverAssetBundleHash = serverManifest.GetAssetBundleHash(abName);
                    if (localAllAssetBundlesDict[abName] != serverAssetBundleHash) mNeedDownLoadQueue.Enqueue(abName);
                }
                else
                {
                    mNeedDownLoadQueue.Enqueue(abName);
                }
            }
            // 可以直接记录下www.bytes,不用加到队列中又加载一次
            if (mNeedDownLoadQueue.Count > 0) mNeedDownLoadQueue.Enqueue(AssetBundleConfig.AssetBundlesFolder);
            www.assetBundle.Unload(true);
        }
        www.Dispose();
    }

    /// <summary>
    /// 正式开始游戏
    /// </summary>
    private void StartGame()
    {
        ScenesManager.Instance.LoadScene("Main");
    }

    /// <summary>
    /// 下载资源
    /// </summary>
    private void DownLoadResource()
    {
        if (mNeedDownLoadQueue.Count == 0)
        {
            UpdateVersionConfig();
            StartGame();
            return;
        }
        var abName = mNeedDownLoadQueue.Dequeue();
        Debugger.Log(StringUtil.Concat("DownLoad ", abName));
        var abPath = StringUtil.PathConcat(AssetBundleConfig.AssetBundlesFolder, abName);
        var url = PathUtil.GetServerFileURL(abPath);
        StartCoroutine(DownLoad(url, abName));
    }

    /// <summary>
    /// 替换本地的资源
    /// </summary>
    private void ReplaceLocalResource(string abName, byte[] data)
    {
        if (data == null) return;
        try
        {
            var abPath = StringUtil.PathConcat(AssetBundleConfig.AssetBundlesFolder, abName);
            var path = PathUtil.GetPresistentDataFilePath(abPath);
            FileUtil.WriteAllBytes(path, data);
        }
        catch (System.Exception e)
        {
            Debugger.LogError(e.Message);
        }
    }

    /// <summary>
    /// 下载资源
    /// </summary>
    IEnumerator DownLoad(string url, string abName)
    {
        var www = new WWW(url);
        yield return www;
        if (string.IsNullOrEmpty(www.error))
        {
            ReplaceLocalResource(abName, www.bytes);
        }
        www.Dispose();
        DownLoadResource();
    }
}
