using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class SceneDescriptorsHelper : MonoBehaviour
{
    public static string ErrorFolderName = "ERROR";
    public static string EnvironmentFolderName = "Environment";

    public static SceneDescriptor LoadFromFile(string filePath)
    {
        string json = File.ReadAllText(Application.dataPath + "/" + filePath);
        return CreateFromString(json);
    }

    public static SceneDescriptor CreateFromString(string data)
    {
        return JsonUtility.FromJson<SceneDescriptor>(data);
    }

    public static List<Transform> CreateInstances(List<ImageDescriptor> imagesDescriptor, Transform parent, SpritesCache cache)
    {
        var objectsList = new List<Transform>();

        foreach (ImageDescriptor imageDescriptor in imagesDescriptor)
            objectsList.Add(CreateSpriteInstance(imageDescriptor, parent, cache).transform);

        return objectsList;
    }

    public static List<Transform> CreateInstances(List<ItemDescriptor> itemDescriptors, Transform parent, SpritesCache cache)
    {
        var objectsList = new List<Transform>();

        foreach (ItemDescriptor itemDescriptor in itemDescriptors)
        {
            Transform item = new GameObject(itemDescriptor.name).transform;
            item.SetParent(parent);
            item.position = new Vector3(0, 0, 0);
            objectsList.Add(item);

            SceneItem itemSceneComponent = item.gameObject.AddComponent<SceneItem>();
            
            Sprite silhouette = cache.Get(itemDescriptor.displayImage.assetPath, itemDescriptor.displayImage.name);
            if (silhouette != null)
                itemSceneComponent.Silhouette = silhouette;
            itemSceneComponent.DisplayName = itemDescriptor.displayName;

            foreach (ItemPlaceHolderDescriptor placeHolder in itemDescriptor.placeHolders)
            {
                var placeHolderObject = CreateSpriteInstance(placeHolder.image, item, cache);

                var shadows = CreateInstances(placeHolder.shadows, placeHolderObject.transform, cache);
                foreach(var shadow in shadows)
                {
                    var component = shadow.gameObject.AddComponent<SceneItemChildLayer>();
                    component.Type = SceneItemChildLayer.LayerType.Shadow;

                    itemSceneComponent.ChildLayers.Add(component);
                }

                var patches = CreateInstances(placeHolder.patches, placeHolderObject.transform, cache);
                foreach (var pacth in patches)
                {
                    var component = pacth.gameObject.AddComponent<SceneItemChildLayer>();
                    component.Type = SceneItemChildLayer.LayerType.Patch;

                    itemSceneComponent.ChildLayers.Add(component);
                }
            }
        }

        return objectsList;
    }

    public static SpriteRenderer CreateSpriteInstance(ImageDescriptor imageDescriptor, Transform parent, SpritesCache cache)
    {
        Sprite sprite = cache.Get(imageDescriptor.assetPath, imageDescriptor.name); 
        if (sprite == null)
            return null;

        Transform image = new GameObject(imageDescriptor.name).transform;
        image.SetParent(parent);
        image.position = imageDescriptor.position;

        SpriteRenderer imageRenderer = image.gameObject.AddComponent<SpriteRenderer>();
        imageRenderer.sprite = sprite;
        imageRenderer.sortingOrder = imageDescriptor.sortingOrder;

        return imageRenderer;
    }

    public static string GetResourceName(string path)
    {
        if (StringHelper.IsNullOrWhitespace(path))
            return null;

        string atlas = "";
        string[] atlasPath = Path.GetDirectoryName(path).Split(Path.AltDirectorySeparatorChar);
        int pos = Array.IndexOf(atlasPath, "Resources");

        if (pos < 0)
            return Path.GetFileNameWithoutExtension(path);

        for (int i = pos + 1; i < atlasPath.Length; i++)
            atlas += atlasPath[i] + Path.AltDirectorySeparatorChar;

        atlas += Path.GetFileNameWithoutExtension(path);
        return atlas;
    }

}

public static class StringHelper
{
    public static bool IsNullOrWhitespace(this String str)
    {
        return String.IsNullOrEmpty(str) || String.IsNullOrEmpty(str.Trim());
    }
}

// cache for sprite resources
public class SpritesCache
{
    private Dictionary<string, Sprite[]> dictionary = new Dictionary<string, Sprite[]>();

    public void Put(string resourceName)
    {
        if (StringHelper.IsNullOrWhitespace(resourceName))
            return;

        Sprite[] spritesAll = Resources.LoadAll<Sprite>(resourceName);
        if (spritesAll == null || spritesAll.Length <= 0)
        {
            Debug.LogError("Error sprite asset path : " + resourceName + " does not exist!");
            return;
        }
        dictionary.Add(resourceName, spritesAll);
    }

    public Sprite Get(string assetPath, string spriteName)
    {
        string resourceName = SceneDescriptorsHelper.GetResourceName(assetPath);
        if (StringHelper.IsNullOrWhitespace(resourceName) || StringHelper.IsNullOrWhitespace(spriteName))
            return null;       

        if (!dictionary.ContainsKey(resourceName))
            Put(resourceName);

        Sprite sprite = dictionary[resourceName].FirstOrDefault(x => x.name == spriteName);
        if (sprite == null)
        {
            Debug.LogError("Error sprite in asset : " + spriteName + " does not exist!");
            return null;
        }

        return sprite;
    }
}
