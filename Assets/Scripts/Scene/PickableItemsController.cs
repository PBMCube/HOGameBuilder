using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PickableItemsController : MonoBehaviour {

    public enum GameType
    {
        Classic,
        Shadow,
        SplitScreen
    }

    public HOPAController Controller;

    // TODO when refactor these names, don't forget to update broken Unity's references
    [SerializeField] private GameObject ScenePrefub;
    [SerializeField] private GameObject ItemPrefub;
    [SerializeField] private GameObject PickableItemPrefub;

    public int VisibleItemCount = 24;
    public int PickableItemCount = 12;
    public int AvailableItemMaxCount = 5;

    public List<SceneItem> PickableItemList = new List<SceneItem>();
    public List<SceneItem> AvailableItemList = new List<SceneItem>();
    
    public void Init(Transform sceneFolder)
    {
        initFromBuildResult(getBuilder().Build(sceneFolder));
    }

    public void Init(string dataBasePath)
    {
        initFromBuildResult(getBuilder().Build(dataBasePath));
    }

    public void Init(TextAsset textAsset)
    {
        initFromBuildResult(getBuilder().Build(textAsset));
    }
    
    public bool IsCanBePickedNow(PickableItem item)
    {
        bool isInAvailableItemList = AvailableItemList.Contains(item.gameObject.GetComponent<SceneItem>());
        return isInAvailableItemList;
    }

    public void OnPickableItemPickup(PickableItem item)
    {
        AvailableItemList.Remove(item.gameObject.GetComponent<SceneItem>());
        if (PickableItemList.Any())
        {
            var newVisiblePickableItem = PickableItemList.Last();
            AvailableItemList.Add(newVisiblePickableItem);
            PickableItemList.Remove(newVisiblePickableItem);
        }
        Controller.OnAvailableItemListChanged();

        if (!AvailableItemList.Any())
        {
            Controller.OnWin();
        }
    }
    
    private PickableItemsBuilder getBuilder()
    {
        var buildParams = new PickableItemsBuilder.BuildParams()
        {
            AvailableItemMaxCount = AvailableItemMaxCount,
            VisibleItemCount = VisibleItemCount,
            PickableItemCount = PickableItemCount,

            ScenePrefab = ScenePrefub,
            ItemPrefab = ItemPrefub,

            Controller = Controller
        };

        var builder = new PickableItemsBuilder(buildParams);
        return builder;
    }

    private void initFromBuildResult(PickableItemsBuilder.BuildResult buildResult)
    {
        PickableItemList = buildResult.PickableItemList;
        AvailableItemList = buildResult.AvailableItemList;
        
        Controller.OnAvailableItemListChanged();
    }
}
