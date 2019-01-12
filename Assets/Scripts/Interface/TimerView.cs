using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TimerView : MonoBehaviour {

    public HOPAController Controller;

    void Start () {
        UpdateView();
    }

    public void UpdateView()
    {
        int remainTimeAsInt = 0;

        if (Controller.Timer != null)
            remainTimeAsInt = (int)(Controller.Timer.RemainTime + 0.99F); // для корректного округления

        int minutes = remainTimeAsInt / 60;
        int seconds = remainTimeAsInt % 60;

        GetComponent<Text>().text = string.Format("{0:D2}:{1:D2}", minutes, seconds);
    }
}
