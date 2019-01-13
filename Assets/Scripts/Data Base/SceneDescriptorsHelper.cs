using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Assets.Scripts.Data_Base;
using UnityEditor;
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

    public static ImageDescriptor CreateImageDescriptor(SpriteRenderer spriteRenderer)
    {
        ImageDescriptor imageDescriptor = new ImageDescriptor();
        imageDescriptor.name = spriteRenderer.sprite.name;
        imageDescriptor.assetPath = AssetDatabase.GetAssetPath(spriteRenderer.sprite);
        imageDescriptor.position = spriteRenderer.transform.position;
        imageDescriptor.size = new Vector2(spriteRenderer.sprite.rect.width, spriteRenderer.sprite.rect.height);
        imageDescriptor.sortingOrder = spriteRenderer.sortingOrder;

        return imageDescriptor;
    }

    public static ImageDescriptor CreateImageDescriptor(Sprite sprite)
    {
        ImageDescriptor imageDescriptor = new ImageDescriptor();
        imageDescriptor.name = sprite.name;
        imageDescriptor.assetPath = AssetDatabase.GetAssetPath(sprite);

        return imageDescriptor;
    }

    public static ItemDescriptor CreateItemDescriptor(SceneItem item)
    {
        ItemDescriptor itemDescriptor = new ItemDescriptor();
        itemDescriptor.name = item.gameObject.name;

        if (string.IsNullOrEmpty(item.DisplayName.Trim()))
            itemDescriptor.displayName = SceneDescriptorsHelper.ToDisplayName(item.gameObject.name);
        else
            itemDescriptor.displayName = item.DisplayName.Trim();

        if (item.Silhouette != null)
            itemDescriptor.displayImage = CreateImageDescriptor(item.Silhouette);

        itemDescriptor.placeHolders = GetItemPlaceHolders(item);

        return itemDescriptor;
    }

    public static List<ItemPlaceHolderDescriptor> GetItemPlaceHolders(SceneItem item)
    {
        List<ItemPlaceHolderDescriptor> itemPlaceHolders = new List<ItemPlaceHolderDescriptor>();
        foreach (Transform child in item.transform)
        {
            ItemPlaceHolderDescriptor itemPlaceHolder = new ItemPlaceHolderDescriptor();
            itemPlaceHolder.image = CreateImageDescriptor(child.GetComponent<SpriteRenderer>());
            itemPlaceHolder.shadows = CollectPlaceholderChilds(child, SceneItemChildLayer.LayerType.Shadow);
            itemPlaceHolder.patches = CollectPlaceholderChilds(child, SceneItemChildLayer.LayerType.Patch);
            itemPlaceHolders.Add(itemPlaceHolder);
        }
        return itemPlaceHolders;
    }

    public static List<ImageDescriptor> CollectPlaceholderChilds(Transform folder, SceneItemChildLayer.LayerType type)
    {
        List<ImageDescriptor> images = new List<ImageDescriptor>();

        var childs = folder.GetComponentsInChildren<SceneItemChildLayer>();
        foreach (var child in childs)
        {
            if (child.transform == folder || child.Type != type)
                continue;

            // TODO check existance of SpriteRenderer component before their use
            images.Add(CreateImageDescriptor(child.GetComponent<SpriteRenderer>()));
        }

        return images;
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

    public static string ToDisplayName(string str)
    {
        string displayName = str.Replace('_', ' ').Trim();

        if (String.IsNullOrEmpty(displayName))
            return displayName;

        displayName = Char.ToUpper(displayName[0]) + (displayName.Length > 1 ? displayName.Substring(1) : "");
        return displayName;
    }
}

public static class StringHelper
{
    public static bool IsNullOrWhitespace(this String str)
    {
        return String.IsNullOrEmpty(str) || String.IsNullOrEmpty(str.Trim());
    }
}
