using UnityEngine;
using UnityEngine.SceneManagement;   // Needed to change scenes
#if UNITY_EDITOR
using UnityEditor;
#endif

public class LevelMenuController : MonoBehaviour
{
    // Return to Main Menu scene
    public void QuitGame()
    {
        Debug.Log("Returning to Main Menu...");
        SceneManager.LoadScene("MainMenu");   // Load your main menu scene
    }
}
