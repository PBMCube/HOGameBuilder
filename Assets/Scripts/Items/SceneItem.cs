using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// component for item that can be placed in scene
public class SceneItem : MonoBehaviour {

    #region Serializable to database

    public Sprite Silhouette;
    public string DisplayName;

    #endregion

    #region Runtime only (no need to save to database)

    public List<SceneItemChildLayer> ChildLayers = new List<SceneItemChildLayer>(); 

    #endregion
}
