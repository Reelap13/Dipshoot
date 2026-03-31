using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class NetworkSingleton<T> : NetworkBehaviour where T: NetworkBehaviour
{
    private static T instance;

    public static T Instance
    {
        get
        {
            if (instance == null)
                instance = FindAnyObjectByType<T>();

            return instance;
        }
    }
}
