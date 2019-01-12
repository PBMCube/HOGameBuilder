using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkillAddTime : Skill {
    public float TimeToAdd = 10.0F;

    public void OnClick()
    {
        if (Controller.Timer != null)
            Controller.Timer.AddRemainTime(TimeToAdd);
        Destroy(gameObject);
    }
    public void OnMouseDown()
    {
        OnClick();
    }
}
