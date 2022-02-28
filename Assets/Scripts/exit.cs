using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class exit : MonoBehaviour
{
    public void doExitGame() {
        print("Exiting application!");

        //.isPlaying = false to be used in editor, will error in built game
        UnityEditor.EditorApplication.isPlaying = false;

        // Application.Quit() to be used in built game, does nothing in editor
        //Application.Quit();
    }
}
