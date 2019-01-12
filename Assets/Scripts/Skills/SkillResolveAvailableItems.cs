using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkillResolveAvailableItems : Skill {
    public int ResolveItemsCount = 1;

    public void OnClick()
    {
        var pickController = Controller.PickableItemsController;

        // resolve random item
        for (int i = 0; i < ResolveItemsCount; ++i)
        {
            int index = Random.Range(0, pickController.AvailableItemList.Count);

            var pickedItem = pickController.AvailableItemList[index].GetComponent<PickableItem>();
            pickedItem.Pickup();
        }

        Destroy(gameObject);
    }
    public void OnMouseDown()
    {
        OnClick();
    }
}
