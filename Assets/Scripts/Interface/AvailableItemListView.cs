using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

// interface for available item view
public interface IView
{
    Transform CreateItem(Transform parent);
    void UpdateView(Transform view, SceneItem data);
}

// display as text only
public class TextView : IView
{
    public Transform Prefab = null;

    public Transform CreateItem(Transform parent)
    {
        if (Prefab == null)
            return null;

        Transform itemView = AvailableItemListView.Instantiate(Prefab, parent) as Transform;
        itemView.GetComponent<Text>().text = "";
        return itemView;
    }

    public void UpdateView(Transform view, SceneItem data)
    {
        if (data == null)
        {
            // reset view
            view.GetComponent<Text>().text = "";
            return;
        }

        view.GetComponent<Text>().text = data.DisplayName;
    }
}

// display as silhouette
public class SilhouetteView : IView
{
    public Transform Prefab = null;

    public Transform CreateItem(Transform parent)
    {
        if (Prefab == null)
            return null;

        Transform itemView = AvailableItemListView.Instantiate(Prefab, parent) as Transform;
        return itemView;
    }

    public void UpdateView(Transform view, SceneItem data)
    {
        if (data == null)
        {
            // reset view
            var childImages = view.GetComponentsInChildren<Image>();
            foreach (var tmp in childImages)
            {
                tmp.enabled = false;
                tmp.sprite = null;
            }
            return;
        }

        var images = view.GetComponentsInChildren<Image>();
        foreach (var image in images)
        {
            image.enabled = true;
        }

        var imageComponent = images.Last();

        // find sprite to display
        Sprite silhouette = data.Silhouette;
        if (silhouette == null)
            silhouette = data.GetComponent<SpriteRenderer>().sprite;

        // set sprite
        imageComponent.sprite = silhouette;

        Resize(view, imageComponent, silhouette);
    }

    private static void Resize(Transform view, Image imageComponent, Sprite silhouette)
    {
        // scale down image to fit parent size and preserve aspect ratio

        var parentSize = view.GetComponent<RectTransform>().sizeDelta;

        float scaledWidth = silhouette.rect.width;
        float scaledHeight = silhouette.rect.height;

        float aspect = silhouette.rect.height / silhouette.rect.width;
        
        if (scaledWidth > parentSize.x)
        {
            scaledWidth = parentSize.x;
            scaledHeight = scaledWidth * aspect;
        }
        if (scaledHeight > parentSize.y)
        {
            scaledHeight = parentSize.y;
            scaledWidth = scaledHeight / aspect;
        }

        // validate resize
        if (scaledWidth > parentSize.x || scaledHeight > parentSize.y)
        {
            Debug.LogWarning("Wrong scaling for " + silhouette.name + " silhouette");
        }

        var rectTransformChild = imageComponent.GetComponent<RectTransform>();
        rectTransformChild.sizeDelta = new Vector2(scaledWidth, scaledHeight);
    }
}

public class ViewFactory
{
    static public IView CreateView(AvailableItemListView viewComponent, AvailableItemListView.ViewType viewType)
    {
        IView view = null;

        switch(viewType)
        {
            case AvailableItemListView.ViewType.Text:
                view = new TextView() { Prefab = viewComponent.AvailableItemTextPrefab };
                break;
            case AvailableItemListView.ViewType.Silhouette:
                view = new SilhouetteView() { Prefab = viewComponent.AvailableItemSilhouettePrefab };
                break;
            default:
                throw new UnityException("Unsupported view type " + viewType);                 
        }

        return view;
    }
}

public class AvailableItemListView : MonoBehaviour {

    public enum ViewType
    {
        Text,
        Silhouette
    }

    public HOPAController Controller;
    public ViewType Type;
    public Transform AvailableItemTextPrefab;
    public Transform AvailableItemSilhouettePrefab;

    private IView PlaceholderView;
    private Transform[] Placeholders;

    public void Init () {        
        int itemCount = Controller.PickableItemsController.AvailableItemMaxCount;
        RectTransform transform = GetComponent<RectTransform>();
        float itemWidth = transform.rect.width / (float)itemCount;
        float itemHeight = transform.rect.height;

        Type = Controller.ViewType;
        PlaceholderView = ViewFactory.CreateView(this, Type);

        Placeholders = new Transform[itemCount];
        for (int i = 0; i < itemCount; i++)
        {            
            Placeholders[i] = PlaceholderView.CreateItem(this.transform);
            Placeholders[i].name = "Placeholder" + (i + 1);
            RectTransform itemTransform = Placeholders[i].GetComponent<RectTransform>();
            itemTransform.anchoredPosition = new Vector2(itemWidth * i, 0); ;
            itemTransform.sizeDelta = new Vector2(itemWidth, itemHeight);
        }        
    }

    public void UpdateView(List<SceneItem> AvailableItemList)
    {
        for (int i = 0; i < AvailableItemList.Count; i++)
        {
            PlaceholderView.UpdateView(Placeholders[i], AvailableItemList[i]);
        }
        for (int i = AvailableItemList.Count; i < Placeholders.Length; i++)
        {
            PlaceholderView.UpdateView(Placeholders[i], null);
        }
    }    
}
