using UnityEngine;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] private GameObject mainMenu;
    [SerializeField] private GameObject modePanel;

    public void HostGame()
    {
        Debug.Log("HOST GAME clicked");

        mainMenu.SetActive(false);
        modePanel.SetActive(true);
    }

    public void JoinGame()
    {
        Debug.Log("JOIN GAME clicked");
    }
}