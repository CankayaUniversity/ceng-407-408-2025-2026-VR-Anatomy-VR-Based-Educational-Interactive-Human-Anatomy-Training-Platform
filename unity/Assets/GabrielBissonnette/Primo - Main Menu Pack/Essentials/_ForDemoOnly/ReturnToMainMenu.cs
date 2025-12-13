using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GabrielBissonnette.Primo
{
    public class ReturnToMainMenu : MonoBehaviour
    {
        public void LoadMainMenu()
        {
            SceneManager.LoadScene("Demo1");
        }
    }
}
