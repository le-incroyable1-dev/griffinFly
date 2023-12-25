using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class startMenuController : MonoBehaviour
{

    public GameObject gameSound;
    private void Start(){
        DontDestroyOnLoad(gameSound);
    }
    public void loadGame(){
        SceneManager.LoadScene(1);
    }

    public void QuitGame(){
        Application.Quit();
    }
}
