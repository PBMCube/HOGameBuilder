using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class PickableItemsBuilder : MonoBehaviour {

    public class BuildParams
    {
        public GameObject ScenePrefab;
        public GameObject ItemPrefab;

        public int VisibleItemCount = 24;
        public int PickableItemCount = 12;
        public int AvailableItemMaxCount = 5;

        public HOPAController Controller;
    }

    public class BuildResult
    {
        public List<SceneItem> PickableItemList = new List<SceneItem>();
        public List<SceneItem> AvailableItemList = new List<SceneItem>();
    }

    private readonly BuildParams _buildParams = null;

    public PickableItemsBuilder(BuildParams buildParams)
    {
        _buildParams = buildParams;
    }
    
    public BuildResult Build(Transform sceneFolder)
    {
        // in this case we have already loaded full scene (in sceneFolder)
        // so we need to randomly remove items to fit constraints in buildParams
        // eg. Total items must be less than VisibleItemCount, pickable items less than PickableItemCount
        var shuffledSceneItems = shuffle(sceneFolder.GetComponentsInChildren<SceneItem>()).ToList();

        List<SceneItem> visibleItems = RefineSceneItems(shuffledSceneItems, sceneFolder, _buildParams);
        List<SceneItem> pickableItems = MakeSomeSceneItemsPickable(visibleItems, _buildParams);

        return GetBuildResult(pickableItems);
    }

    public BuildResult Build(string dataBasePath)
    {
        SceneDescriptor sceneDescriptor = SceneDescriptorsHelper.LoadFromFile(dataBasePath);
        return Build(sceneDescriptor);
    }

    public BuildResult Build(TextAsset textAsset)
    {
        SceneDescriptor sceneDescriptor = SceneDescriptorsHelper.CreateFromString(textAsset.text);
        return Build(sceneDescriptor);
    }

    public BuildResult Build(SceneDescriptor sceneDescriptor)
    {
        // in this case scene does not exists
        // so first need to pick items directly from SceneDescriptor
        // and then create objects ONLY for picked items

        SpritesCache cache = new SpritesCache();

        // create scene
        Transform sceneFolder = CreateSceneFolder(sceneDescriptor);

        // load envinronment images
        SceneDescriptorsHelper.CreateInstances(sceneDescriptor.imagesEnvironment, sceneFolder, cache);
        
        // load items from SceneDescriptor
        var visibleItems = CreateVisibleItems(shuffle(sceneDescriptor.items).ToList(), sceneFolder, cache, _buildParams);

        // and make some of them pickable
        var pickableItems = MakeSomeSceneItemsPickable(visibleItems, _buildParams);

        return GetBuildResult(pickableItems);
    }

    public Transform CreateSceneFolder(SceneDescriptor sceneDescriptor)
    {
        GameObject scene = Instantiate(_buildParams.ScenePrefab);
        scene.name = sceneDescriptor.sceneName;

        SceneComponent sceneComponent = scene.GetComponent<SceneComponent>();

        if (sceneComponent != null)
            sceneComponent.SceneSize = sceneDescriptor.sceneSize;
        else
        {
            sceneComponent = scene.AddComponent<SceneComponent>();
            sceneComponent.SceneSize = sceneDescriptor.sceneSize;
        }

        return scene.transform;
    }

    /// <summary>
    /// Create constrained count of visible SceneItems
    /// 
    /// BuildParams defines constraint on visible SceneItems count
    /// </summary>
    /// <returns></returns>
    private List<SceneItem> CreateVisibleItems(List<ItemDescriptor> itemDescriptors, Transform sceneFolder, SpritesCache cache, BuildParams buildParams)
    {
        List<SceneItem> visibleItems = new List<SceneItem>();

        if (buildParams.ItemPrefab == null)
        {
#if DEBIG_PICKABLE_ITEMS_BUILDER
            Debug.LogError("Item Prefab does not exist");
#endif
            return visibleItems;
        }

        CollisionController collisionController = new CollisionController(sceneFolder.gameObject.GetComponent<SceneComponent>().SceneSize);

        foreach (ItemDescriptor itemDescriptor in itemDescriptors)
        {
            if (itemDescriptor.placeHolders.Count == 0)
            {
#if DEBIG_PICKABLE_ITEMS_BUILDER
                Debug.LogWarningFormat("Item Descriptor {0} does not have placeholders", itemDescriptor.name);
#endif
                continue;
            }

            var shuffledPlaceHolders = shuffle(itemDescriptor.placeHolders).ToList();

            foreach (ItemPlaceHolderDescriptor placeHolder in shuffledPlaceHolders)
            {
                if (collisionController.CheckCollision(placeHolder.image))
                    continue;

                Sprite sprite = cache.Get(placeHolder.image.assetPath, placeHolder.image.name);
                if (sprite == null)
                    continue;

                GameObject sceneItem = Instantiate(buildParams.ItemPrefab, sceneFolder);

                sceneItem.name = placeHolder.image.name;
                AttachSpriteToObject(sceneItem.transform, placeHolder.image, sprite);

                SceneItem sceneItemComponent = sceneItem.GetComponent<SceneItem>();
                sceneItemComponent.DisplayName = itemDescriptor.displayName;

                Sprite silhouette = cache.Get(itemDescriptor.displayImage.assetPath, itemDescriptor.displayImage.name);
                if (silhouette != null)
                    sceneItemComponent.Silhouette = silhouette;

                foreach (var shadow in placeHolder.shadows)
                {
                    var obj = CreateSpriteInstance(shadow, cache);
                    // NOTE in game mode shadows attached to scene, not to scene item
                    obj.transform.SetParent(sceneFolder);
                    var childComponent = obj.gameObject.AddComponent<SceneItemChildLayer>();
                    childComponent.Type = SceneItemChildLayer.LayerType.Shadow;
                    sceneItemComponent.ChildLayers.Add(childComponent);
                }

                foreach (var patch in placeHolder.patches)
                {
                    var obj = CreateSpriteInstance(patch, cache);
                    // NOTE in game mode patches attached to scene, not to scene item
                    obj.transform.SetParent(sceneFolder);
                    var childComponent = obj.gameObject.AddComponent<SceneItemChildLayer>();
                    childComponent.Type = SceneItemChildLayer.LayerType.Patch;
                    sceneItemComponent.ChildLayers.Add(childComponent);
                }

                visibleItems.Add(sceneItemComponent);

                break;
            }

            if (visibleItems.Count >= buildParams.VisibleItemCount)
                break;
        }

        return visibleItems;
    }

    /// <summary>
    /// Picks random SceneItems and their PlaceHolders
    /// until conditions in BuildParams are satisfied
    /// (eg. 20 visible items)
    /// 
    /// For picked SceneItems removed all placeholders except one
    /// Other SceneItems removed from scene
    /// </summary>
    /// <returns>returns list of visible items</returns>
    private List<SceneItem> RefineSceneItems(List<SceneItem> itemList, Transform sceneFolder, BuildParams buildParams)
    {
        CollisionController collisionController = new CollisionController(sceneFolder.gameObject.GetComponent<SceneComponent>().SceneSize);
        List<SceneItem> visibleItemList = new List<SceneItem>();

        foreach (SceneItem item in itemList)
        {
            if (visibleItemList.Count >= buildParams.VisibleItemCount)
            {
                Destroy(item.gameObject);
                continue;
            }

            // skip if item does not contains placeholders
            if (item.transform.childCount == 0)
                continue;
            
            // get list of all placeholders
            List<SpriteRenderer> placeHolders = new List<SpriteRenderer>();
            foreach (Transform child in item.transform)
            {
                // filter out themeselves
                if (child == item.transform)
                    continue;

                SpriteRenderer childSpriteRenderer = child.gameObject.GetComponent<SpriteRenderer>();
                if (childSpriteRenderer == null || childSpriteRenderer.sprite == null)
                    continue;

                placeHolders.Add(childSpriteRenderer);
            }
            
            // shuffle and select one that does not collide with items already attached to scene
            placeHolders = shuffle(placeHolders).ToList();

            SpriteRenderer pickedPlaceHolder = null;
            foreach (SpriteRenderer placeHolder in placeHolders)
            {
                if (!collisionController.CheckCollision(placeHolder))
                {
                    pickedPlaceHolder = placeHolder;
                    break;
                }
            }

            // if no one placeholder is suitable, remove whole item from scene
            if (pickedPlaceHolder == null)
            {
                Destroy(item.gameObject);
                continue;
            }

            // else remove other placeholders except picked one
            // also do some gameObjects hierarchy flattening
            SpriteRenderer itemSpriteRenderer = item.gameObject.AddComponent<SpriteRenderer>();
            itemSpriteRenderer.sprite = pickedPlaceHolder.sprite;
            itemSpriteRenderer.sortingOrder = pickedPlaceHolder.sortingOrder;

            Transform[] childs = pickedPlaceHolder.GetComponentsInChildren<Transform>();
            foreach (Transform child in childs)
            {
                if (child == pickedPlaceHolder.transform)
                    continue;
                child.SetParent(sceneFolder, true);
            }

            item.transform.position = pickedPlaceHolder.transform.position;

            childs = item.GetComponentsInChildren<Transform>();
            foreach (Transform child in childs)
            {
                if (child == item.transform)
                    continue;
                Destroy(child.gameObject);
            }

            visibleItemList.Add(item);
        }
        return visibleItemList;
    }

    /// <summary>
    /// Make some items pickable
    /// </summary>
    /// <returns>return list of pickalbe items</returns>
    private List<SceneItem> MakeSomeSceneItemsPickable(List<SceneItem> visibleItemList, BuildParams buildParams)
    {
        List<SceneItem> pickableItemList = new List<SceneItem>();

        int maxPickableItemCount = Math.Min(buildParams.PickableItemCount, visibleItemList.Count);
        for (int i = 0; i < maxPickableItemCount; i++)
        {
            SceneItem item = visibleItemList[i];

            var spriteRenderer = item.gameObject.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                var boxCollider = item.gameObject.GetComponent<BoxCollider2D>();
                if (boxCollider == null)
                {
                    var size = spriteRenderer.sprite.bounds.size;

                    boxCollider = item.gameObject.AddComponent<BoxCollider2D>();
                    boxCollider.size = new Vector2(size.x, size.y);
                }
            }

            PickableItem component = item.gameObject.AddComponent<PickableItem>();
            component.Controller = buildParams.Controller;
            pickableItemList.Add(item);
        }
        return pickableItemList;
    }

    /// <summary>
    /// Build Available item list and pickable item list 
    /// </summary>
    /// <returns></returns>
    private BuildResult GetBuildResult(List<SceneItem> itemList)
    {   
        int availableItemMaxCount = Math.Min(_buildParams.AvailableItemMaxCount, itemList.Count);

        return new BuildResult()
        {
            AvailableItemList = itemList.GetRange(0, availableItemMaxCount),
            PickableItemList = itemList.GetRange(availableItemMaxCount, 
                itemList.Count - availableItemMaxCount)
        };
    }

    private static SpriteRenderer CreateSpriteInstance(ImageDescriptor imageDescriptor, SpritesCache cache)
    {
        Sprite sprite = cache.Get(imageDescriptor.assetPath, imageDescriptor.name);
        if (sprite == null)
            return null;

        var obj = new GameObject(imageDescriptor.name);
        return AttachSpriteToObject(obj.transform, imageDescriptor, sprite);
    }

    private static SpriteRenderer AttachSpriteToObject(Transform obj, ImageDescriptor imageDescriptor, Sprite sprite)
    {
        SpriteRenderer imageRenderer = obj.GetComponent<SpriteRenderer>();

        if (imageRenderer == null)
            imageRenderer = obj.gameObject.AddComponent<SpriteRenderer>();

        imageRenderer.sprite = sprite;
        imageRenderer.sortingOrder = imageDescriptor.sortingOrder;
        obj.position = imageDescriptor.position;

        return imageRenderer;
    }

    private static IEnumerable<T> shuffle<T>(IEnumerable<T> source)
    {
        var rnd = new System.Random();
        return source.OrderBy(item => rnd.Next());
    }
}
