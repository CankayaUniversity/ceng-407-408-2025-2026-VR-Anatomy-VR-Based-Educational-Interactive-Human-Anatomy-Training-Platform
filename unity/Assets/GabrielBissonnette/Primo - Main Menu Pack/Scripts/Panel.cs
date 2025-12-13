using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GabrielBissonnette.Primo
{
    public class Panel : MonoBehaviour
    {
        public int index;
        public GameObject panel;
        public GameObject firstButton;
        public Panel previousPanel;
        [SerializeField] MainMenuManager mainMenuManager;

        private void OnEnable()
        {
            mainMenuManager.RegisterPanel(index, this);
        }
    }
}
