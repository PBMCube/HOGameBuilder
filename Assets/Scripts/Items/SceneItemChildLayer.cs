using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// component for child layers of SceneItem
// child layers needed for shadows and patch images
public class SceneItemChildLayer : MonoBehaviour {

    public enum LayerType
    {
        Shadow,     // shadow layer, hided when item is picked up
        Patch       // additional images that help to fit item into environment
    }

    public LayerType Type;
}
