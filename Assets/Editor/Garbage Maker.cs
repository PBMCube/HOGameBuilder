using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class GarbageMaker : EditorWindow
{
    private Transform scene;

    [MenuItem("Window/Garbage Maker")]
    public static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(GarbageMaker));
    }

    void OnGUI()
    {
        GUILayout.Label("Scene", EditorStyles.boldLabel);
        scene = EditorGUILayout.ObjectField("Scene", scene, typeof(Transform), true) as Transform;

        EditorGUILayout.Space();

        if (GUILayout.Button("Create"))
        {
            OnCreateButtonClick();
        }
    }

    private void OnCreateButtonClick()
    {
        SceneItem[] AllItems = scene.GetComponentsInChildren<SceneItem>();
        List<SceneItem> AllItemList = new List<SceneItem>(AllItems);

        foreach (SceneItem item in AllItemList)
        {
            if (item.transform == scene)
                continue;

            Transform[] placeHolders = item.GetComponentsInChildren<Transform>();
            int counter = placeHolders.Length + 1;

            for (int i = 0; i < 10; i++)
            {
                foreach (Transform placeHolder in placeHolders)
                {
                    if (placeHolder == item.transform || placeHolder.parent != item.transform)
                        continue;

                    GameObject newPlaceHolder = Instantiate(placeHolder.gameObject, item.transform) as GameObject;
                    newPlaceHolder.name = item.name + "_" + counter;

                    SpriteRenderer[] images = newPlaceHolder.GetComponentsInChildren<SpriteRenderer>();
                    foreach (SpriteRenderer image in images)
                    {
                        string resourceName = SceneDescriptorsHelper.GetResourceName(AssetDatabase.GetAssetPath(image.sprite)) + UnityEngine.Random.Range(2, 21);
                        Sprite[] spritesAll = Resources.LoadAll<Sprite>(resourceName);
                        Sprite sprite = spritesAll.FirstOrDefault(x => x.name == image.sprite.name);
                        image.sprite = sprite;
                    }
                    counter++;
                }
            }
        }

        for (int i = 0; i < 10; i++)
        {
            foreach (SceneItem item in AllItemList)
            {
                if (item.transform == scene)
                    continue;

                GameObject newitem = Instantiate(item.gameObject, scene.transform) as GameObject;
                newitem.name = item.name + i;
            }
        }
    }
}