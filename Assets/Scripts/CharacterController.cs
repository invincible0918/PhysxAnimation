using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CharacterController : MonoBehaviour
{
    public Text text;

    private PhysxAnimation[] _physxAnimations;

    private bool _active = true;

    public void OnClick()
    {
        _active = !_active;

        if (_active)
            text.text = "Physx Animation On";
        else
            text.text = "Physx Animation Off";

        SetActivePhysxAnimation();
    }

    void Start ()
    {
        _physxAnimations = GetComponentsInChildren<PhysxAnimation>();
    }
	
	void SetActivePhysxAnimation()
    {
        foreach(PhysxAnimation pa in _physxAnimations)
        {
            if (_active)
                pa.UpdateMode = PhysxAnimation.AnimationUpdateMode.Update;
            else
                pa.UpdateMode = PhysxAnimation.AnimationUpdateMode.None;
        }
    }
}
