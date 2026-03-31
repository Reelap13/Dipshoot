using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public static class Utils
{
    public static IEnumerator StartTimer(float time, Action callback)
    {
        yield return new WaitForSeconds(time);
        callback();
    }
}