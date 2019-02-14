﻿/**
 * AssetBundle资源管理
 */

using Base.Common;
using UnityEngine;
using Base.Debug;
using Base.Pool;
using System.IO;
using System.Collections.Generic;
using Base.Utils;
using System.Runtime.Serialization.Formatters.Binary;

public class AssetBundleManager : MonoSingleton<AssetBundleManager>
{
    /// <summary>
    /// 配置文件路径
    /// </summary>
    private readonly string ASSETBUNDLECONFIGPATH = Application.streamingAssetsPath + "/assetbundleconfig";
    /// <summary>
    /// 以path为key存储一份AssetBundleBase
    /// </summary>
    private Dictionary<string, AssetBundleBase> mAssetBundleBaseDict = new Dictionary<string, AssetBundleBase>();
    /// <summary>
    /// 存储加载的AssetBundleItem
    /// </summary>
    private Dictionary<string, AssetBundleItem> mAssetBundleItemDict = new Dictionary<string, AssetBundleItem>();
    /// <summary>
    /// AssetBundleItem对应的类对象池
    /// </summary>
    private ClassObjectPool<AssetBundleItem> mAssetBundleItemPool = ObjectManager.Instance.GetOrCreateClassPool<AssetBundleItem>();

    /// <summary>
    /// 加载AssetBundle配置文件
    /// </summary>
    public bool LoadAssetBundleConfig()
    {
        mAssetBundleBaseDict.Clear();
        mAssetBundleItemDict.Clear();
        var assetBundle = AssetBundle.LoadFromFile(ASSETBUNDLECONFIGPATH);
        var textAsset = assetBundle.LoadAsset<TextAsset>("assetbundleconfig");
        if (textAsset == null)
        {
            Debugger.LogError("AssetBundleConfig is not exist!");
            return false;
        }
        var stream = new MemoryStream(textAsset.bytes);
        var formatter = new BinaryFormatter();
        var assetBundleConfig = (AssetBundleConfig)formatter.Deserialize(stream);
        stream.Close();
        Debug.Log(assetBundleConfig.AssetBundleList.Count);
        for (var i = 0; i < assetBundleConfig.AssetBundleList.Count; i++)
        {
            var abBase = assetBundleConfig.AssetBundleList[i];
            var path = abBase.Path;
            if (mAssetBundleBaseDict.ContainsKey(path))
            {
                Debugger.LogError("Duplicate path! AssetName:{0}, ABName:{1}", abBase.AssetName, abBase.ABName);
            }
            else
            {
                mAssetBundleBaseDict.Add(path, abBase);
            }
        }
        return true;
    }

    /// <summary>
    /// 获取ABBase资源
    /// </summary>
    public AssetBundleBase GetAssetBundleBase(string path)
    {
        if (mAssetBundleBaseDict.ContainsKey(path))
        {
            return mAssetBundleBaseDict[path];
        }
        return null;
    }

    /// <summary>
    /// 根据AB包名同步加载AssetBundle
    /// </summary>
    private AssetBundle SyncLoadAssetBundle(string name)
    {
        AssetBundleItem item = null;
        if (!mAssetBundleItemDict.TryGetValue(name, out item))
        {
            AssetBundle assetBundle = null;
            var path = StringUtil.Concat(Application.streamingAssetsPath, "/", name);
            if (File.Exists(path)) assetBundle = AssetBundle.LoadFromFile(path);
            if (assetBundle == null) Debugger.LogError("Load AssetBundle Error: " + name);
            item = mAssetBundleItemPool.Spawn();
            item.AssetBundle = assetBundle;
            item.RefCount++;
            mAssetBundleItemDict.Add(name, item);
        }
        else
        {
            item.RefCount++;
        }
        return item.AssetBundle;
    }

    /// <summary>
    /// 加载AssetBundleBase所依赖的所有AB包
    /// </summary>
    public AssetBundle SyncLoadAssetBundle(AssetBundleBase abBase)
    {
        if (abBase == null) return null;
        var ab = SyncLoadAssetBundle(abBase.ABName);
        if (abBase.ABDependList != null)
        {
            for (var i = 0; i < abBase.ABDependList.Count; i++)
            {
                SyncLoadAssetBundle(abBase.ABDependList[i]);
            }
        }
        return ab;
    }

    /// <summary>
    /// 卸载AssetBundle
    /// </summary>
    private void UnloadAssetBundle(string name)
    {
        AssetBundleItem item = null;
        if (mAssetBundleItemDict.TryGetValue(name, out item) && item != null)
        {
            item.RefCount--;
            if (item.RefCount <= 0 && item.AssetBundle != null)
            {
                item.AssetBundle.Unload(true);
                item.Reset();
                mAssetBundleItemPool.Recycle(item);
                mAssetBundleItemDict.Remove(name);
            }
        }
    }

    /// <summary>
    /// 根据path找到对应的ABBase,卸载关联的ab包
    /// </summary>
    public void UnloadAssetBundleByPath(string path)
    {
        var abBase = GetAssetBundleBase(path);
        if (abBase == null) return;
        if (abBase.ABDependList != null)
        {
            for (var i = 0; i < abBase.ABDependList.Count; i++)
            {
                UnloadAssetBundle(abBase.ABDependList[i]);
            }
        }
        UnloadAssetBundle(abBase.ABName);
    }

    /// <summary>
    /// 异步加载AB包
    /// </summary>
    public void AsyncLoadAssetBundle()
    {

    }

    /// <summary>
    /// 处理Load列表里的资源
    /// </summary>
    //private void ProcessLoadAsset()
    //{
    //    foreach (var loader in _currentLoadQueue)
    //    {
    //        if (loader.CanAsyncLoadAsset())
    //        {
    //            loader.isBeenLoad = true;
    //            StartCoroutine(loader.AysncLoadAsset());
    //        }
    //    }
    //}

    /// <summary>
    /// 检测
    /// </summary>
    private void Update()
    {
        
    }
}