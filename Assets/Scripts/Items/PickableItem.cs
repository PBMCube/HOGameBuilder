using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

// component for item that can be picked up by player
public class PickableItem : MonoBehaviour, IPointerClickHandler {

	public Animation PickupAnimation;
	public HOPAController Controller;

	public void Pickup()
	{
		Controller.OnPickableItemPicked (this);

        // remove shadows
        GetComponent<SceneItem>().ChildLayers.ForEach(x => {
            if (x != null && x.Type == SceneItemChildLayer.LayerType.Shadow)
                Destroy(x.gameObject);
        });
        
        // TODO playAnimation

		Destroy (gameObject);
	}

    public void OnPointerClick(PointerEventData eventData)
    {
        Controller.OnPickableItemClicked(this);
    }
    public void OnMouseDown()
    {
        Controller.OnPickableItemClicked(this);
    }
}
