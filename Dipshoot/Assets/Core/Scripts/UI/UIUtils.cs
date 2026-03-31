using System;
using System.Collections;
using NUnit.Framework;
using TMPro;
using UnityEngine;

namespace Scripts.UI
{
    public static class UIUtils
    {
        public static void StartCoroutine(IEnumerator coroutine) => UIUtilsGameObject.Instance.StartCor(coroutine);

        public static IEnumerator UpdateLoop(Action<float> func, float duration, Action callback = null)
        {
            float t = 0f;
            while (t < 1)
            {
                t += Time.deltaTime / duration;
                func(t);
                yield return null;
            }

            if (callback != null)
                callback();
        }
    }
}