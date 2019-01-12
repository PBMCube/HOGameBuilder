using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class FinishMenu : MonoBehaviour {

    public void Init(HOPAController.FinishState state)
    {
        Text resultText = GetComponentByName<Text>("ResultText");
        switch (state)
        {
            case HOPAController.FinishState.Win:
                resultText.text = "YOU  WIN";
                break;

            case HOPAController.FinishState.Lose:
                resultText.text = "YOU  LOSE";
                break;

            default:
                throw new System.ArgumentOutOfRangeException();
        }
    }

    public void OnRestartButtonClick()
    {
        Scene scene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(scene.name);
    }

    public void OnExitButtonClick()
    {
        Application.Quit();
    }

    private T GetComponentByName<T>(string name) where T : MonoBehaviour
    {
        T[] components = GetComponentsInChildren<T>();
        return components.FirstOrDefault(x => x.gameObject.name == name);
    }
}
