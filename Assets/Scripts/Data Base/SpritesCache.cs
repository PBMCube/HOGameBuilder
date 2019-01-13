using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.Scripts.Data_Base
{
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
}
