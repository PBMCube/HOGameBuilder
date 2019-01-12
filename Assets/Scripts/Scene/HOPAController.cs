using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// main controller of HOPA game
// wire up all parts of the game
// parts may have different implementation by subclasses
public class HOPAController : MonoBehaviour {

    public enum LoadingType
    {
        Static,
        Dynamic
    }

    public enum State
    {
        InProgress,
        Finished
    }

    public enum FinishState
    {
        Win,
        Lose
    }

    public LoadingType SceneLoadingType = LoadingType.Dynamic;
    public TextAsset DataBase;
    public Transform Scene;
    public AvailableItemListView.ViewType ViewType;

    public PickableItemsController PickableItemsController;
    public SkillsController SkillsController;


    public AvailableItemListView AvailableItemListView;
    
    public Timer Timer;
    public TimerView TimerView;
    public SkillsView SkillsView;

    public State CurrentState = State.InProgress;
    public FinishState CurrentFinishState = FinishState.Win;

    public Action<HOPAController> OnFinishCallBack;

    public void Init (AvailableItemListView.ViewType viewType, bool timeMode, TextAsset dataBase) {
        ViewType = viewType;
        DataBase = dataBase; 

        if (!timeMode)
        {
            if (TimerView != null)
                Destroy(TimerView.gameObject);

            if (Timer != null)
                Destroy(Timer);
        }

        AvailableItemListView.Init();
        InitPickabbleItemsController();       
    }

    private void InitPickabbleItemsController()
    {
        switch (SceneLoadingType)
        {
            case LoadingType.Static:
                PickableItemsController.Init(Scene);
                break;
            case LoadingType.Dynamic:
                PickableItemsController.Init(DataBase);
                break;
            default:
                break;
        }
    }

    public void OnPickableItemClicked(PickableItem item)
	{
		// pickup if element is in current pickable items list
		if (PickableItemsController.IsCanBePickedNow(item))
			item.Pickup ();
	}

	public void OnPickableItemPicked(PickableItem item)
	{
		// remove from list of pickable items
		PickableItemsController.OnPickableItemPickup(item);
	}

	public void OnAvailableItemListChanged()
	{
        if (AvailableItemListView != null)
            AvailableItemListView.UpdateView(PickableItemsController.AvailableItemList);
    }

    public void OnTimeOut()
    {
        if (TimerView != null)
            TimerView.UpdateView();

        OnLose();
    }

    public void OnTimeChanged()
    {
        if (TimerView != null)
            TimerView.UpdateView();
    }

    public void OnWin()
    {
        CurrentState = State.Finished;
        CurrentFinishState = FinishState.Win;

        if (TimerView != null)
            TimerView.UpdateView();
        
        if (OnFinishCallBack != null)
        {
            OnFinishCallBack(this);
        }
    }

    public void OnLose()
    {
        CurrentState = State.Finished;
        CurrentFinishState = FinishState.Lose;

        if (TimerView != null)
            TimerView.UpdateView();
        
        if (OnFinishCallBack != null)
        {
            OnFinishCallBack(this);
        }
    }
}
