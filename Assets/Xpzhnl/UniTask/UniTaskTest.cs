using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace LearnUnity
{
    public class UniTaskTest : MonoBehaviour
    {
        [SerializeField] private Button loadTextBtn;
        [SerializeField] private Text loadText;
        [SerializeField] private Slider progressBar;

        void Start()
        {
            loadTextBtn.onClick.AddListener(() =>
            {

            });
        }
    }
}
