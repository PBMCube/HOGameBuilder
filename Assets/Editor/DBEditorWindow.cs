using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class DBEditorWindow : EditorWindow
{
    private Transform scene;
    string dataBasePathPattern = @"Resources/{0}.json";
    private string defaultFileName = "Scene";
    private string dataBaseOpenPath = "Resources/Scene.json";

    [MenuItem("Window/DataBase Editor")]
    public static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(DBEditorWindow));
    }

    void OnGUI()
    {
        GUILayout.Label("Scene", EditorStyles.boldLabel);
        scene = EditorGUILayout.ObjectField("Scene", scene, typeof(Transform), true) as Transform;

        string dataBaseSavePath = String.Format(dataBasePathPattern, (scene == null) ? defaultFileName : scene.name);
        dataBaseSavePath = EditorGUILayout.TextField("Database path", dataBaseSavePath);

        EditorGUILayout.Space();

        if (GUILayout.Button("Save"))
        {
            OnSaveButtonClick(dataBaseSavePath);
        }

        dataBaseOpenPath = EditorGUILayout.TextField("Database path", dataBaseOpenPath);

        if (GUILayout.Button("Open"))
        {
            OnOpenButtonClick(dataBaseOpenPath);
        }
    }

    private void OnSaveButtonClick(string dataBaseSavePath)
    {
        SceneDescriptor sceneDescriptor = new SceneDescriptor();
        sceneDescriptor.sceneName = scene.name;
        sceneDescriptor.sceneSize = scene.GetComponent<SceneComponent>().SceneSize;
        sceneDescriptor.imagesEnvironment = GetImagesEnvironment();
        sceneDescriptor.items = GetItems();
        SaveToFile(sceneDescriptor, dataBaseSavePath);
    }

    private void OnOpenButtonClick(string dataBaseOpenPath)
    {
        SceneDescriptor sceneDescriptor = SceneDescriptorsHelper.LoadFromFile(dataBaseOpenPath);
        if (!String.IsNullOrEmpty(sceneDescriptor.sceneName))
            scene = new GameObject(sceneDescriptor.sceneName).transform;
        else
            scene = new GameObject("Scene").transform;

        scene.position = new Vector3(0, 0, 0);
        SceneComponent sceneComponent = scene.gameObject.AddComponent<SceneComponent>();
        sceneComponent.SceneSize = sceneDescriptor.sceneSize;

        var environmentFolder = new GameObject(SceneDescriptorsHelper.EnvironmentFolderName).transform;
        environmentFolder.transform.SetParent(scene);

        SpritesCache cache = new SpritesCache();
        SceneDescriptorsHelper.CreateInstances(sceneDescriptor.imagesEnvironment, environmentFolder, cache);
        SceneDescriptorsHelper.CreateInstances(sceneDescriptor.items, scene, cache);
    }

    private void SaveToFile(SceneDescriptor sceneDescriptor, string dataBaseSavePath)
    {
        string json = JsonUtility.ToJson(sceneDescriptor, true);
        File.WriteAllText(Application.dataPath + "/" + dataBaseSavePath, json);
    }

    private List<ImageDescriptor> GetImagesEnvironment()
    {
        List<ImageDescriptor> imagesList = new List<ImageDescriptor>();

        foreach (Transform layer in scene)
        {
            if (layer.GetComponent<SceneItem>() != null || String.Compare(layer.name, SceneDescriptorsHelper.ErrorFolderName, true) == 0)
                continue;

            Transform[] allImages = layer.GetComponentsInChildren<Transform>();

            foreach (Transform image in allImages)
            {
                if (image.GetComponent<SceneItem>() != null || image.GetComponent<SpriteRenderer>() == null)
                    continue;
                imagesList.Add(CreateImageDescriptor(image.GetComponent<SpriteRenderer>()));
            }
        }
        return imagesList;
    }

    private ImageDescriptor CreateImageDescriptor(SpriteRenderer spriteRenderer) {
        ImageDescriptor imageDescriptor = new ImageDescriptor();
        imageDescriptor.name = spriteRenderer.sprite.name;
        imageDescriptor.assetPath = AssetDatabase.GetAssetPath(spriteRenderer.sprite);
        imageDescriptor.position = spriteRenderer.transform.position;
        imageDescriptor.size = new Vector2(spriteRenderer.sprite.rect.width, spriteRenderer.sprite.rect.height);
        imageDescriptor.sortingOrder = spriteRenderer.sortingOrder;

        return imageDescriptor;
    }

    private ImageDescriptor CreateImageDescriptor(Sprite sprite)
    {
        ImageDescriptor imageDescriptor = new ImageDescriptor();
        imageDescriptor.name = sprite.name;
        imageDescriptor.assetPath = AssetDatabase.GetAssetPath(sprite);

        return imageDescriptor;
    }

    private List<ItemDescriptor> GetItems()
    {
        List<ItemDescriptor> itemList = new List<ItemDescriptor>();
        SceneItem[] allItems = scene.GetComponentsInChildren<SceneItem>();

        foreach (SceneItem item in allItems)
        {
            if (item == scene)
                continue;
            itemList.Add(CreateItemDescriptor(item));
        }
        return itemList;
    }

    private ItemDescriptor CreateItemDescriptor(SceneItem item)
    {
        ItemDescriptor itemDescriptor = new ItemDescriptor();
        itemDescriptor.name = item.gameObject.name;

        if (string.IsNullOrEmpty(item.DisplayName.Trim()))
            itemDescriptor.displayName = SceneGenerator.ToDisplayName(item.gameObject.name);
        else
            itemDescriptor.displayName = item.DisplayName.Trim();

        if (item.Silhouette != null)
            itemDescriptor.displayImage = CreateImageDescriptor(item.Silhouette);

        itemDescriptor.placeHolders = GetItemPlaceHolders(item);

        return itemDescriptor;
    }

    private List<ItemPlaceHolderDescriptor> GetItemPlaceHolders(SceneItem item)
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

    private List<ImageDescriptor> CollectPlaceholderChilds(Transform folder, SceneItemChildLayer.LayerType type)
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
}