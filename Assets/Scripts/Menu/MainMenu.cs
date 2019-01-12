using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class MainMenu : MonoBehaviour
{
    public Transform HOPAController;
    public FinishMenu FinishMenu;

    public List<TextAsset> Scenes;

    public void OnStartButtonClick()
    {
        gameObject.SetActive(false);
        Transform HOInstance = Instantiate(HOPAController) as Transform;
        HOPAController HOController = HOInstance.GetComponent<HOPAController>();
        HOController.OnFinishCallBack += OnFinish;
        HOController.Init(GetViewType(), GetTimeMode(), GetSceneDataBase());
    }

    public void OnExitButtonClick()
    {
        Application.Quit();
    }

    private bool GetTimeMode()
    {
        Toggle timeMode = GetComponentByName<Toggle>("TimeMode");
        return timeMode.isOn;
    }

    private AvailableItemListView.ViewType GetViewType()
    {
        Dropdown viewType = GetComponentByName<Dropdown>("ViewType");
        switch (viewType.value)
        {
            case 0:
                return AvailableItemListView.ViewType.Text;
            case 1:
                return AvailableItemListView.ViewType.Silhouette;
            default:
                throw new System.ArgumentOutOfRangeException();
        }
    }

    private TextAsset GetSceneDataBase()
    {
        Dropdown viewType = GetComponentByName<Dropdown>("SceneName");
        return Scenes[viewType.value];
    }

    private T GetComponentByName<T>(string name) where T : MonoBehaviour
    {
        T[] components = GetComponentsInChildren<T>();
        return components.FirstOrDefault(x => x.gameObject.name == name);
    }

    private void OnFinish(HOPAController HOController)
    {
        FinishMenu FinishMenuInstance = Instantiate(FinishMenu) as FinishMenu;
        FinishMenuInstance.Init(HOController.CurrentFinishState);
        Destroy(HOController.gameObject);
    }
}
