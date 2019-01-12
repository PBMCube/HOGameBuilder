using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

class SceneGenerator
{
    private static string environment_suffix_key = "_bg";
    private static List<string> shadows_suffix_keys = new List<string>() { "shadow", "sh", "light", "glow" };
    private static List<string> patches_suffix_keys = new List<string>() { "patch" };
    private static List<string> silhouettes_suffix_keys = new List<string>() { "silhouette" };

    private Transform schrodingers_folder = null; // folder for unrecognized images
    private Transform environment_folder = null;  // folder for environment images
    private Transform scene_folder = null;        // scene folder 

    // Scene contains SceneItems that can have several placeholders that can have shadows or patch layers

    private class Item
    {
        public SpriteRenderer displayImage = null;
        public Dictionary<string, PlaceHolder> placeHolders = null;
    }

    private class PlaceHolder
    {
        public SpriteRenderer itemImage = null;  // ref to itemImage that must contain placeholder
        public List<SpriteRenderer> shadows = new List<SpriteRenderer>();
        public List<SpriteRenderer> patches = new List<SpriteRenderer>();
    }

    public void BuildScene(List<SpriteRenderer> spriteRenderers, Transform root, Vector2Int imageSize)
    {
        scene_folder = root;
        var sceneComponent = scene_folder.gameObject.AddComponent<SceneComponent>();
        sceneComponent.SceneSize = imageSize;

        AttachSceneItemComponents(ParseSceneItems(spriteRenderers));
    }

    private Dictionary<string, Item> ParseSceneItems(List<SpriteRenderer> spriteRenderers)
    {
        Dictionary<string, Item> items = new Dictionary<string, Item>();

        while (spriteRenderers.Count != 0)
        {
            var sprite = spriteRenderers[0];
            bool isParsed = TryParseEnvironment(sprite) || TryParsePlaceHolder(sprite, items);
            if (!isParsed)
            {
#if DEBUG_SCENE_GENERATION
                Debug.LogError("Can't parse " + spriteRenderers[0].name);
#endif
            }

            spriteRenderers.RemoveAt(0);
        }

        return items;
    }

    private bool TryParseEnvironment(SpriteRenderer sprite)
    {
        if (sprite.name.EndsWith(environment_suffix_key))
        {
            sprite.gameObject.transform.SetParent(GetEnvironmentFolder());
            return true;
        }
        else return false;
    }

    private bool TryParsePlaceHolder(SpriteRenderer sprite, Dictionary<string, Item> items)
    {
        string itemName = GetSceneItemName(sprite.name);
        if (itemName == null)
            return false;

        if (!items.ContainsKey(itemName))
            items.Add(itemName, new Item());

        string placeHolderName = itemName;
        string suffix = sprite.name.Remove(0, itemName.Length);

        // regex matches for example "_01_shadow", selecting placeholder suffix into group 1 and layer type (shadow, patch, etc) into group 2
        Match match = Regex.Match(suffix,
            GetRegexPattern(@"^(?:((?=_\d+(?:_|$))_+\d+))?(?:_+({0})(\d+|_|$)|$)?", shadows_suffix_keys.Concat(silhouettes_suffix_keys)));

        if (match.Success && match.Groups.Count > 1 && match.Groups[1] != null && match.Groups[1].Success)
        {
            placeHolderName += match.Groups[1].Value;
        }

        bool isSecondMatchSuccessful = match.Success && match.Groups.Count > 2 && match.Groups[2] != null &&
                                       match.Groups[2].Success;

        var item = items[itemName];

        // check silhouette at first
        if (isSecondMatchSuccessful && silhouettes_suffix_keys.Contains(match.Groups[2].Value))
        {
#if DEBUG_SCENE_GENERATION
            if (silhouette != null)
            {
                Debug.LogWarning("silhouette already exists " + silhouette.name + " " + sprite.name);
            }
#endif
            item.displayImage = sprite;
            return true;
        }

        // check other layer types
        if (item.placeHolders == null)
            item.placeHolders = new Dictionary<string, PlaceHolder>();

        if (!item.placeHolders.ContainsKey(placeHolderName))
            item.placeHolders.Add(placeHolderName, new PlaceHolder());

        if (sprite.name == placeHolderName)
        {
            if (item.placeHolders[placeHolderName].itemImage != null)
            {
                sprite.gameObject.transform.SetParent(GetErrorFolder());
                return false;
            }
            else
            {
                item.placeHolders[placeHolderName].itemImage = sprite;
                return true;
            }
        }

        if (isSecondMatchSuccessful)
            item.placeHolders[placeHolderName].shadows.Add(sprite);
        else
            item.placeHolders[placeHolderName].patches.Add(sprite);

        return true;
    }

    private void AttachSceneItemComponents(Dictionary<string, Item> items)
    {
        if (items == null)
        {
#if DEBUG_SCENE_GENERATION
                Debug.LogError("AttachSceneItemComponents: Error: items is null");
#endif
            return;
        }

        foreach (var item in items)
        {
            Transform sceneItem = null;
            SceneItem sceneItemComponent = null;

            foreach (var placeHolder in item.Value.placeHolders)
            {
                if (placeHolder.Value.itemImage == null)
                {
                    foreach (var patch in placeHolder.Value.patches)
                        patch.gameObject.transform.SetParent(GetErrorFolder());

                    foreach (var shadow in placeHolder.Value.shadows)
                        shadow.gameObject.transform.SetParent(GetErrorFolder());
                }
                else
                {
                    Transform parent = null;
                    if (sceneItem == null)
                    {
                        sceneItem = new GameObject(item.Key).transform;
                        sceneItem.SetParent(scene_folder);
                        sceneItemComponent = sceneItem.gameObject.AddComponent<SceneItem>();
                        sceneItemComponent.DisplayName = ToDisplayName(item.Key);

                        if (item.Value.displayImage != null)
                        {
                            sceneItemComponent.Silhouette = item.Value.displayImage.sprite;

                            // destroy silhouette object
                            GameObject.DestroyImmediate(item.Value.displayImage.gameObject);
                        }
                    }

                    placeHolder.Value.itemImage.gameObject.transform.SetParent(sceneItem);
                    parent = placeHolder.Value.itemImage.gameObject.transform;

                    foreach (var patch in placeHolder.Value.patches)
                    {
                        var component = patch.gameObject.AddComponent<SceneItemChildLayer>();
                        component.Type = SceneItemChildLayer.LayerType.Shadow;
                        if (sceneItemComponent)
                            sceneItemComponent.ChildLayers.Add(component);

                        patch.gameObject.transform.SetParent(parent);
                    }

                    foreach (var shadow in placeHolder.Value.shadows)
                    {
                        var component = shadow.gameObject.AddComponent<SceneItemChildLayer>();
                        component.Type = SceneItemChildLayer.LayerType.Shadow;
                        if (sceneItemComponent)
                            sceneItemComponent.ChildLayers.Add(component);

                        shadow.gameObject.transform.SetParent(parent);
                    }
                }
            }
        }
    }

    /// <summary>
    /// extract SceneItem name from layer name
    /// 
    /// Full layer name can be name_01_sh or name_01_patch
    /// So SceneItem name would be 'name'
    /// PlaceholderName would be 'name_01'
    /// </summary>
    private string GetSceneItemName(string layerName)
    {
        Match match = Regex.Match(layerName, GetRegexPattern(@"(.*?)(?:_+(?:\d.*|{0})(?:\d+|_|$)|$)", shadows_suffix_keys.Concat(patches_suffix_keys).Concat(silhouettes_suffix_keys)));
        if (match.Success)
            return match.Groups[1].Value;
        else
            return null;
    }

    private string GetRegexPattern(string pattern, IEnumerable<string> list)
    {
        return String.Format(pattern, string.Join("|", list.ToArray()).Trim('|'));
    }

    public static string ToDisplayName(string str)
    {
        string displayName = str.Replace('_', ' ').Trim();

        if (String.IsNullOrEmpty(displayName))
            return displayName;

        displayName = Char.ToUpper(displayName[0]) + (displayName.Length > 1 ? displayName.Substring(1) : "");
        return displayName;
    }

    private Transform GetEnvironmentFolder()
    {
        if (environment_folder == null)
        {
            environment_folder = new GameObject(SceneDescriptorsHelper.EnvironmentFolderName).transform;
            environment_folder.SetParent(scene_folder);
            environment_folder.SetAsFirstSibling();
        }

        return environment_folder;
    }

    private Transform GetErrorFolder()
    {
        if (schrodingers_folder == null)
        {
            schrodingers_folder = new GameObject(SceneDescriptorsHelper.ErrorFolderName).transform;
            schrodingers_folder.SetParent(scene_folder);
            schrodingers_folder.SetAsFirstSibling();
        }

        return schrodingers_folder;
    }
}
