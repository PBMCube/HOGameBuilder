using PhotoshopFile;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class PSDParserWindow : EditorWindow
{
    protected Texture2D image;
    private Vector2 scrollPos;
    private PsdFile psd;
    private int atlassize = 2048;
    private float pixelsToUnitSize = 1.0f;
    private string fileName;
    private List<string> LayerList = new List<string>();

    private const string atlasFilenamePattern = @"Assets/Resources/Textures/{0}_atlas.png";

    #region MenuItems
    [MenuItem("Window/PSD Parser Window")]
    public static void ShowWindow()
    {
        var wnd = GetWindow<PSDParserWindow>();
        
        wnd.minSize = new Vector2(400, 300);
        wnd.Show();
    }

    [MenuItem("Assets/Parse PSD File", true, 20000)]
    private static bool ParsePSDFileEnabled()
    {
        for (var i = 0; i < Selection.objects.Length; i++) {


            var obj = Selection.objects[i];
            var filePath = AssetDatabase.GetAssetPath(obj);
            if (filePath.EndsWith(".psd", System.StringComparison.CurrentCultureIgnoreCase)) {
                return true;
            }
        }

        return false;
    }

    [MenuItem("Assets/Parse PSD File", false, 20000)]
    private static void ParsePSDFile()
    {
        var obj = Selection.objects[0];

        var window = EditorWindow.GetWindow<PSDParserWindow>(true);
        window.minSize = new Vector2(400, 300);
        window.image = (Texture2D)obj;
        window.LoadInformation(window.image);
        window.Show();
    }

    #endregion

    public void OnEnable()
    {
        string temppath = AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(this));
        List<string> temp = temppath.Split('/').ToList();

        temp.Remove(temp[temp.Count - 1]);
        temp.Remove(temp[temp.Count - 1]);
        temppath = "";
        foreach (var item in temp) {
            temppath += (item + "/");
        }
        titleContent.image = AssetDatabase.LoadAssetAtPath<Texture>(temppath + "Logo/logo.png");
        titleContent.text = "PSD Parser window";
    }

    public void OnGUI()
    {
        EditorGUI.BeginChangeCheck();

        image = (Texture2D)EditorGUILayout.ObjectField("PSD File", image, typeof(Texture2D), true);

        bool changed = EditorGUI.EndChangeCheck();

        if (image != null) {
            if (changed) {
                string path = AssetDatabase.GetAssetPath(image);

                if (path.ToUpper().EndsWith(".PSD", System.StringComparison.CurrentCultureIgnoreCase)) {
                    LoadInformation(image);
                } else {
                    psd = null;
                }

            }
            if (psd != null) {
                scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

                foreach (Layer layer in psd.Layers) {
                    var sectionInfo = (LayerSectionInfo)layer.AdditionalInfo
                                                .SingleOrDefault(x => x is LayerSectionInfo);
                    if (sectionInfo == null) {

                        layer.Visible = EditorGUILayout.ToggleLeft(layer.Name, layer.Visible);
                    }

                }
                EditorGUILayout.EndScrollView();

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Select All", GUILayout.Width(200))) {
                    foreach (Layer layer in psd.Layers) {
                        var sectionInfo = (LayerSectionInfo)layer.AdditionalInfo
                            .SingleOrDefault(x => x is LayerSectionInfo);
                        if (sectionInfo == null) {

                            layer.Visible = true;
                        }

                    }

                }
                if (GUILayout.Button("Select None", GUILayout.Width(200))) {
                    foreach (Layer layer in psd.Layers) {
                        var sectionInfo = (LayerSectionInfo)layer.AdditionalInfo
                            .SingleOrDefault(x => x is LayerSectionInfo);
                        if (sectionInfo == null) {

                            layer.Visible = false;
                        }

                    }

                }
                EditorGUILayout.EndHorizontal();

                if (GUILayout.Button("Start"))
                {
                    CreateAtlasAndParse();
                }

            } else {
                EditorGUILayout.HelpBox("This texture is not a PSD file.", MessageType.Error);
            }
        }
    }

    private Texture2D CreateTexture(Layer layer, Vector2Int imageSize)
    {
        if ((int)layer.Rect.width == 0 || (int)layer.Rect.height == 0) {
            return null;
        }

        int src_width = (int)layer.Rect.width;
        int src_height = (int)layer.Rect.height;

        int trg_width = src_width;
        int trg_height = src_height;
        int src_x = 0;
        int src_y = 0;
        if (layer.Rect.x + trg_width > imageSize.x)
            trg_width = (int)(imageSize.x - layer.Rect.x);
        if (layer.Rect.y + trg_height > imageSize.y)
            trg_height = (int)(imageSize.y - layer.Rect.y);
        if (layer.Rect.x < 0)
        {
            src_x = -(int)layer.Rect.x;
            trg_width -= src_x;
        }
        if (layer.Rect.y < 0)
        {
            src_y = -(int)layer.Rect.y;
            trg_height -= src_y;
        }

        if (trg_width < 0 || trg_height < 0) return null;
        Texture2D tex = new Texture2D(trg_width, trg_height, TextureFormat.RGBA32, true);
        Color32[] pixels = new Color32[tex.width * tex.height];
        Channel red = (from l in layer.Channels
                       where l.ID == 0
                       select l).First();
        Channel green = (from l in layer.Channels
                         where l.ID == 1
                         select l).First();
        Channel blue = (from l in layer.Channels
                        where l.ID == 2
                        select l).First();
        Channel alpha = layer.AlphaChannel;
        for (int i = 0; i < pixels.Length; i++) {
            int tx = i % trg_width;
            int ty = i / trg_width;
            int si = tx + (ty + src_y) * src_width + src_x;
            byte r = red.ImageData[si];
            byte g = green.ImageData[si];
            byte b = blue.ImageData[si];
            byte a = 255;
            if (alpha != null) {
                a = alpha.ImageData[si];
            }
            int mod = i % tex.width;
            int n = ((tex.width - mod - 1) + i) - mod;
            pixels[pixels.Length - n - 1] = new Color32(r, g, b, a);
        }
        tex.SetPixels32(pixels);
        tex.Apply();
        return tex;
    }

    private void CreateAtlasAndParse()
    {
        List<Texture2D> textures = new List<Texture2D>();
        List<SpriteRenderer> spriteRenderers = new List<SpriteRenderer>();

        LayerList = new List<string>();
        int zOrder = 0;

        string assetPath = AssetDatabase.GetAssetPath(image);
        GameObject root = new GameObject(Path.GetFileNameWithoutExtension(assetPath) ?? "Scene");
        Vector2Int imageSize = new Vector2Int((int)psd.BaseLayer.Rect.width, (int)psd.BaseLayer.Rect.height);

        // create Texture2D for each PSD layer
        foreach (var layer in psd.Layers) {
            if (layer.Visible && layer.Rect.width > 0 && layer.Rect.height > 0) {
                if (LayerList.IndexOf(layer.Name.Split('|').Last()) == -1) {
                    LayerList.Add(layer.Name.Split('|').Last());
                    Texture2D tex = CreateTexture(layer, imageSize);
                    if (tex == null)
                        continue;
                    textures.Add(tex);
                    GameObject go = new GameObject(layer.Name);
                    SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
                    Debug.Log(layer.Name + ": " + layer.Rect.ToString());
                    go.transform.position = new Vector3((layer.Rect.width / 2 + layer.Rect.x - psd.BaseLayer.Rect.width / 2) / pixelsToUnitSize,
                                                         (-layer.Rect.height / 2 - layer.Rect.y + psd.BaseLayer.Rect.height / 2) / pixelsToUnitSize, 0);
                    spriteRenderers.Add(sr);
                    sr.sortingOrder = zOrder++;
                    go.transform.parent = root.transform;
                }
            }
        }

        // Create atlas and calculate metadata for each image
        Texture2D atlas = new Texture2D(atlassize, atlassize);
        Texture2D[] textureArray = textures.ToArray();
        Rect[] rects = atlas.PackTextures(textureArray, 2, atlassize);
        List<SpriteMetaData> Sprites = new List<SpriteMetaData>();
        for (int i = 0; i < rects.Length; i++) {
            SpriteMetaData smd = new SpriteMetaData();
            smd.name = spriteRenderers[i].name.Split('|').Last();
            smd.rect = new Rect(rects[i].xMin * atlas.width,
                rects[i].yMin * atlas.height,
                rects[i].width * atlas.width,
                rects[i].height * atlas.height);
            smd.pivot = new Vector2(0.5f, 0.5f); // Center is default otherwise layers will be misaligned
            smd.alignment = (int)SpriteAlignment.Center;
            Sprites.Add(smd);
        }

        // write atlas image to file
        string filePath = String.Format(atlasFilenamePattern, Path.GetFileNameWithoutExtension(assetPath));
        File.WriteAllBytes(filePath, atlas.EncodeToPNG());

        AssetDatabase.Refresh();
        
        // load atlas image to memory
        atlas = (Texture2D)AssetDatabase.LoadAssetAtPath(filePath, typeof(Texture2D));

        // configure atlas import options and import asset into project
        {
            var textureImporter = AssetImporter.GetAtPath(filePath) as TextureImporter;
            // Make sure the size is the same as our atlas then create the spritesheet
            textureImporter.maxTextureSize = atlassize;
            textureImporter.spritesheet = Sprites.ToArray();
            textureImporter.textureType = TextureImporterType.Sprite;
            textureImporter.spriteImportMode = SpriteImportMode.Multiple;
            textureImporter.spritePivot = new Vector2(0.5f, 0.5f);
            textureImporter.spritePixelsPerUnit = pixelsToUnitSize;
            textureImporter.textureFormat = TextureImporterFormat.AutomaticTruecolor;

            AssetDatabase.ImportAsset(filePath, ImportAssetOptions.Default);
        }

        foreach (Texture2D tex in textureArray) {
            DestroyImmediate(tex);
        }

        AssetDatabase.Refresh();

        // load atlas as separate images
        Sprite[] atlasSprites = AssetDatabase.LoadAllAssetsAtPath(filePath).Select(x => x as Sprite).Where(x => x != null).ToArray();

        for (int i = 0; i < spriteRenderers.Count; ++i)
        {
            spriteRenderers[i].name = spriteRenderers[i].name.Split('|').Last();
            spriteRenderers[i].sprite = atlasSprites.First(x => x.name == spriteRenderers[i].name);
        }

        // finally, generate scene from images
        var sceneGenerator = new SceneGenerator();
        sceneGenerator.BuildScene(spriteRenderers, root.transform, imageSize);
    }

    public static void ApplyLayerSections(List<Layer> layers)
    {
        var stack = new Stack<string>();

        foreach (var layer in Enumerable.Reverse(layers))
        {

            var sectionInfo = (LayerSectionInfo)layer.AdditionalInfo
                .SingleOrDefault(x => x is LayerSectionInfo);
            if (sectionInfo == null)
            {
                var Reverstack = stack.ToArray();
                Array.Reverse(Reverstack);
                layer.Name = String.Join("|", Reverstack) + "|" + layer.Name;
            }
            else
            {
                switch (sectionInfo.SectionType)
                {
                    case LayerSectionType.OpenFolder:
                        stack.Push(layer.Name);
                        break;
                    case LayerSectionType.Layer:
                        stack.Push(layer.Name);
                        break;
                    case LayerSectionType.ClosedFolder:
                        stack.Push(layer.Name);
                        break;
                    case LayerSectionType.SectionDivider:
                        stack.Pop();
                        break;
                }
            }
        }

    }

    public void LoadInformation(Texture2D Img)
    {
        string path = AssetDatabase.GetAssetPath(Img);

        psd = new PsdFile(path, Encoding.Default);
        fileName = Path.GetFileNameWithoutExtension(path);
        ApplyLayerSections(psd.Layers);
    }
}