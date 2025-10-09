using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Menu : MonoBehaviour
{
    [SerializeField] private GameObject MenuPanel;
    private bool isMenuOpen;

    private void Start()
    {
        MenuPanel.SetActive(false);
        isMenuOpen = false;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            ToggleMenu();
        }
    }

    public void ToggleMenu()
    {
        if (!isMenuOpen)
        {
            OpenMenu();
        }
        else
        {
            CloseMenu();
        }
    }

    public void OpenMenu()
    {
        Time.timeScale = 0;
        MenuPanel.SetActive(true);
        isMenuOpen = true;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.Confined;
    }

    public void CloseMenu()
    {
        Time.timeScale = 1;
        MenuPanel.SetActive(false);
        isMenuOpen = false;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }
}
