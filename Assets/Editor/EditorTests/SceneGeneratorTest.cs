using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests
{
    public class SceneGeneratorTest
    {
        [Test]
        public void SceneGeneratorTestNull()
        {
            var sceneGenerator = new SceneGenerator();
            var sprites = new List<SpriteRenderer>();
            Transform root = new GameObject().transform;

            Assert.Throws<NullReferenceException>(delegate
                { sceneGenerator.BuildScene(null, null, new Vector2Int(0, 0)); });

            Assert.Throws<NullReferenceException>(delegate
                { sceneGenerator.BuildScene(sprites, null, new Vector2Int(0, 0)); });

            Assert.Throws<NullReferenceException>(delegate
                { sceneGenerator.BuildScene(null, root, new Vector2Int(0, 0)); });
        }

        [Test]
        public void SceneGeneratorTestZero()
        {
            var sceneGenerator = new SceneGenerator();
            Transform root = new GameObject().transform;
            var sprites = new List<SpriteRenderer>();

            sceneGenerator.BuildScene(sprites, root, new Vector2Int(0, 0));
            Assert.AreEqual(root.childCount, 0);
        }

        [Test]
        public void SceneGeneratorTestEnvironment()
        {
            var sceneGenerator = new SceneGenerator();
            Transform root = new GameObject().transform;
            string[] spriteEnvironmentNames =  {
                "back_bg",
                "light_01_bg",
                "owl_01_bg",
                "owl_01_patch_bg",
                "sh_bg",
            };

            string[] spriteRegularNames =
            {
                "owl_01",
                "owl_01_sh",
                "bg"
            };

            sceneGenerator.BuildScene(CreateSprites(spriteEnvironmentNames.Concat(spriteRegularNames).ToArray()), 
                root, new Vector2Int(0, 0));

            Transform environmentFolder = GetChildByName(root, "Environment");

            Assert.AreEqual(root.childCount, 3); // Environment, owl and bg
            Assert.AreEqual(environmentFolder.childCount, spriteEnvironmentNames.Length);
            for (int i = 0; i < environmentFolder.childCount; i++)
            {
                Assert.That(spriteEnvironmentNames, Has.Member(environmentFolder.GetChild(i).name));
            }
        }

        // A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
        // `yield return null;` to skip a frame.
        [UnityTest]
        public IEnumerator SceneGeneratorTestWithEnumeratorPasses()
        {
            // Use the Assert class to test conditions.
            // Use yield to skip a frame.
            yield return null;
        }

        private List<SpriteRenderer> CreateSprites(string[] layerNames)
        {
            var sprites = new List<SpriteRenderer>();
            for (int i = 0; i < layerNames.Length; i++)
            {
                var gameObject = new GameObject(layerNames[i]);
                var sprite = gameObject.AddComponent<SpriteRenderer>();
                sprites.Add(sprite);
            }
            return sprites;
        }

        private Transform GetChildByName(Transform root, string name)
        {
            return root.Find(name);
        }
    }
}
