using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JoystickHelper : Singleton<JoystickHelper>
{
    private Dictionary<string, bool> axisStates = new();
    public bool GetAxisDown(string axisName)
    {
        bool currentlyPrressed = Mathf.Abs(Input.GetAxis(axisName))> 0.1f;
        if(!axisStates.ContainsKey(axisName))
        {
            axisStates.Add(axisName, false);
        }
        bool previoslyPressed = axisStates[axisName];
        axisStates[axisName] = currentlyPrressed;
        if (currentlyPrressed && !previoslyPressed)
        {
            return true;
        }
        return false;
    }
}
