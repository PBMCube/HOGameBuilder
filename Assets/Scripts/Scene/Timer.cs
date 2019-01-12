using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Timer : MonoBehaviour {

    public HOPAController Controller;
    public float Timeout = 90.0F; // seconds
    public float TimeFactor = 1.0F;

    private float remainTime = 0.0F;

    public float RemainTime
    {
        get { return Mathf.Max(0.0F, remainTime); }
        set { remainTime = Mathf.Max(0.0F, value); }
    }
    
    public void AddRemainTime(float delta)
    {
        RemainTime = RemainTime + delta;
    }

    // Use this for initialization
    void Start () {
        RemainTime = Timeout;
    }
	
	// Update is called once per frame
	void Update () {
        // TODO also if not in pause 
        if (Controller.CurrentState != HOPAController.State.InProgress)
            return;

        RemainTime -= Time.deltaTime * TimeFactor;

        if (RemainTime <= 0.0F)
        {
            RemainTime = 0.0F;
            Controller.OnTimeOut();
        }

        // TODO use checks in Update istead?
        Controller.OnTimeChanged();
	}
}
