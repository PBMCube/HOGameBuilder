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

            sceneGenerator.BuildScene(sprites, root, new Vector2Int(42, 34));
            Assert.AreEqual(root.childCount, 0);

            var sceneComponent = root.GetComponent<SceneComponent>();
            Assert.That(sceneComponent.SceneSize, Is.EqualTo(new Vector2Int(42, 34)));
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

            Transform environmentFolder = GetChildByName(root, SceneDescriptorsHelper.EnvironmentFolderName);

            Assert.AreEqual(root.childCount, 3); // Environment, owl and bg
            Assert.AreEqual(environmentFolder.childCount, spriteEnvironmentNames.Length);
            for (int i = 0; i < environmentFolder.childCount; i++)
            {
                Assert.That(spriteEnvironmentNames, Has.Member(environmentFolder.GetChild(i).name));
            }
        }

        [Test]
        public void SceneGeneratorTestSimplePlaceholdersNames()
        {
            var sceneGenerator = new SceneGenerator();
            Transform root = new GameObject().transform;

            string[] spriteNames =  {
                "spider",
                "hat_01",
                "hat_01_sh",
                "hat_01_shadow",
                "hat_02",
                "hat_02_light",
                "hat_02_glow",
                "ancient_book_01",
                "ancient_book_02",
                "ancient_book_02_patch",
                "ancient_book_02_someting_that_must_be_patch",
                "ancient_book_03",
                "ancient_book_silhouette",
            };

            sceneGenerator.BuildScene(CreateSprites(spriteNames),
                root, new Vector2Int(0, 0));

            Assert.That(root.childCount, Is.EqualTo(3)); // 3 scene items

            Transform sceneItem;
            Transform placeholder;

            sceneItem = TestSceneItem(root, "spider", 1, "Spider");
            Assert.That(sceneItem.GetChild(0).name, Is.EqualTo("spider"));

            sceneItem = TestSceneItem(root, "hat", 2, "Hat");
            placeholder = TestPlaceholder(sceneItem, 0, "hat_01", 2);
            TestSceneItemChildLayer(placeholder, 0, "hat_01_sh", SceneItemChildLayer.LayerType.Shadow);
            TestSceneItemChildLayer(placeholder, 1, "hat_01_shadow", SceneItemChildLayer.LayerType.Shadow);

            placeholder = TestPlaceholder(sceneItem, 1, "hat_02", 2);
            TestSceneItemChildLayer(placeholder, 0, "hat_02_light", SceneItemChildLayer.LayerType.Shadow);
            TestSceneItemChildLayer(placeholder, 1, "hat_02_glow", SceneItemChildLayer.LayerType.Shadow);

            sceneItem = TestSceneItem(root, "ancient_book", 3, "Ancient book");
            Assert.That(sceneItem.GetComponent<SceneItem>().Silhouette, Is.Not.Null);

            placeholder = TestPlaceholder(sceneItem, 0, "ancient_book_01", 0);
            placeholder = TestPlaceholder(sceneItem, 1, "ancient_book_02", 2);
            TestSceneItemChildLayer(placeholder, 0, "ancient_book_02_patch", SceneItemChildLayer.LayerType.Patch);
            TestSceneItemChildLayer(placeholder, 1, "ancient_book_02_someting_that_must_be_patch", SceneItemChildLayer.LayerType.Patch);

            placeholder = TestPlaceholder(sceneItem, 2, "ancient_book_03", 0);
        }

        [Test]
        public void SceneGeneratorTestChildLayersWithoutPlaceholders()
        {
            var sceneGenerator = new SceneGenerator();
            Transform root = new GameObject().transform;

            // Layers without placeholders should be checked by programmer
            // so must to be moved to Error folder
            string[] spriteNames =  {
                "a_01_01",
                "a_02_01_sh",
                "hat_01_patch_sh",
                "hat_01_shadow_patch",
                "hat_02_hat_01",
                "hat_02_light_01_hat",
                "glow_light"
            };

            sceneGenerator.BuildScene(CreateSprites(spriteNames),
                root, new Vector2Int(0, 0));

            Transform errorFolder = GetChildByName(root, SceneDescriptorsHelper.ErrorFolderName);

            Assert.That(root.childCount, Is.EqualTo(1)); // is Error folder
            Assert.That(errorFolder.childCount, Is.EqualTo(spriteNames.Length));
        }

        [Test]
        public void SceneGeneratorTestInvalidPlaceholdersNames()
        {
            var sceneGenerator = new SceneGenerator();
            Transform root = new GameObject().transform;

            // Invalid names should be checked by programmer
            // so must to be moved to Error folder
            string[] spriteNames =  {
                "_01",
                "",
                " ",
                "%6-%#",
                "\0",
                "\n"
            };

            sceneGenerator.BuildScene(CreateSprites(spriteNames),
                root, new Vector2Int(0, 0));

            Transform errorFolder = GetChildByName(root, SceneDescriptorsHelper.ErrorFolderName);

            Assert.That(root.childCount, Is.EqualTo(1)); // is Error folder
            Assert.That(errorFolder.childCount, Is.EqualTo(spriteNames.Length));
        }

        private Transform TestSceneItem(Transform root, string name, int expectedPlaceholders, string expectedDisplayName)
        {

            var child = GetChildByName(root, name);
            Assert.That(child.childCount, Is.EqualTo(expectedPlaceholders));
            Assert.That(child.GetComponent<SceneItem>().DisplayName, Is.EqualTo(expectedDisplayName));

            return child;
        }

        private Transform TestPlaceholder(Transform sceneItem, int idx, string expectedName, int expectedChilds)
        {
            var placeholder = sceneItem.GetChild(idx);

            Assert.That(placeholder.name, Is.EqualTo(expectedName));
            Assert.That(placeholder.childCount, Is.EqualTo(expectedChilds));

            return placeholder;
        }

        private SceneItemChildLayer TestSceneItemChildLayer(Transform placeholder, int idx, string expectedName, SceneItemChildLayer.LayerType expectedType)
        {
            var childLayer = placeholder.GetChild(idx).GetComponent<SceneItemChildLayer>();

            Assert.That(childLayer.name, Is.EqualTo(expectedName));
            Assert.That(childLayer.Type, Is.EqualTo(expectedType));

            return childLayer;
        }

        private List<SpriteRenderer> CreateSprites(string[] layerNames)
        {
            var sprites = new List<SpriteRenderer>();
            for (int i = 0; i < layerNames.Length; i++)
            {
                var gameObject = new GameObject(layerNames[i]);
                var spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
                spriteRenderer.sprite = Sprite.Create(new Texture2D(1, 1), new Rect(), new Vector2(1, 1));

                sprites.Add(spriteRenderer);
            }
            return sprites;
        }

        private Transform GetChildByName(Transform root, string name)
        {
            return root.Find(name);
        }
    }
}
