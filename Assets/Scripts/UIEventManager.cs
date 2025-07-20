using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIEventManager : MonoBehaviour
{
    // Functions for the various bottons on homescreen. Can be selected via dropdown menu
    
    public void ActivateDefault()
    {
        Debug.Log("ContentActivated: Default");
    }
    public void ActivateHome()
    {
        Debug.Log("ContentActivated: Home");
    }

    public void ActivateShop()
    {
        Debug.Log("ContentActivated: Shop");
    }

    public void ActivateStory()
    {
        Debug.Log("ContentActivated: Story");
    }

    public void ActivateSettings()
    {
        Debug.Log("ContentActivated: Settings");
    }
    
}