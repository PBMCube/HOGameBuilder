using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SceneDescriptor {

    public string sceneName;
    public Vector2Int sceneSize = new Vector2Int();
    public List<ImageDescriptor> imagesEnvironment = new List<ImageDescriptor>();
    public List<ItemDescriptor> items = new List<ItemDescriptor>();
}

[Serializable]
public class ItemDescriptor
{
    public string name;
    public string displayName;
    public ImageDescriptor displayImage = new ImageDescriptor();
    public List<ItemPlaceHolderDescriptor> placeHolders = new List<ItemPlaceHolderDescriptor>();
}

[Serializable]
public class ImageDescriptor
{
    public string name;
    public string assetPath;
    public int sortingOrder;
    public Vector3 position;
    public Vector2 size;
}

[Serializable]
public class ItemPlaceHolderDescriptor
{
    public ImageDescriptor image;
    public List<ImageDescriptor> shadows = new List<ImageDescriptor>();
    public List<ImageDescriptor> patches = new List<ImageDescriptor>();
}