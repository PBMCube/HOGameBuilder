using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkillsView : MonoBehaviour {
    public HOPAController Controller;

    public List<Skill> CurrentSkills = new List<Skill>();

	void Start () {
        var skillsController = Controller.SkillsController;
        if (skillsController == null)
            return;

        var rectTransform = GetComponent<RectTransform>();
        float width = rectTransform.rect.width;

        const float heightSpacing = 10.0F;
        float startHeight = 0.0F;
        
		foreach(var skillPrefab in skillsController.AvailableSkills)
        {
            if (skillPrefab == null)
                continue;
            
            var skillObj  = Instantiate(skillPrefab, this.transform) as Skill;
            skillObj.Controller = Controller;

            RectTransform itemTransform = skillObj.GetComponent<RectTransform>();

            startHeight -= itemTransform.rect.height + heightSpacing;

            itemTransform.anchoredPosition = new Vector2(0, startHeight);
            itemTransform.sizeDelta = new Vector2(width, itemTransform.sizeDelta.y);
        }
	}
}
