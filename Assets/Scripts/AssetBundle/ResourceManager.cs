﻿/**
 * 资源加载管理类
 */

using Base.Common;
using Base.Pool;
using UnityEngine;
using Base.Utils;
using Base.Debug;
using System.Collections.Generic;

public class ResourceManager : Singleton<ResourceManager>
{
    /// <summary>
    /// 缓存引用计数为0的资源，达到缓存上限时情掉最早没用的资源
    /// </summary>
    private ResourceMapList<ResourceItem> mNoRefResourceMapList = new ResourceMapList<ResourceItem>();
    /// <summary>
    /// 缓存使用的资源列表
    /// </summary>
    public Dictionary<string, ResourceItem> ResourceDict { get; private set; } = new Dictionary<string, ResourceItem>();

    /// <summary>
    /// 同步加载
    /// </summary>
    public T Load<T>(string path) where T : Object
    {
        if (string.IsNullOrEmpty(path)) return null;
        var md5 = FileUtil.GetMD5HashFromFile(path);
        var item = GetCacheResourceItem(md5);
        if (item != null) return item.Obj as T;
        return default(T);
    }

    /// <summary>
    /// 获取缓存里的资源项
    /// </summary>
    private ResourceItem GetCacheResourceItem(string md5)
    {
        ResourceItem item = null;
        if (ResourceDict.TryGetValue(md5, out item) && item != null))
        {
            item.RefCount++;
            item.LastUseTime = Time.realtimeSinceStartup;
        }
        return item;
    }
}
