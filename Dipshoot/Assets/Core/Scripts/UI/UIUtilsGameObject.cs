using UnityEngine;
using System;
using System.Collections;
using NUnit.Framework;
using TMPro;

namespace Scripts.UI
{
    public class UIUtilsGameObject : Singleton<UIUtilsGameObject>
    {
        public void StartCor(IEnumerator coroutine)
        {
            StartCoroutine(coroutine);
        }
    }
}
