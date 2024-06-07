using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public GameObject charPanel;
    public GameObject smokeButton;
    public GameObject arrowButton;

    public void SmokerChosen()
    {
        charPanel.SetActive(false);
        smokeButton.SetActive(true);
    }
    public void SovaChosen()
    {
        charPanel.SetActive(false);
        arrowButton.SetActive(true);
    }
}
