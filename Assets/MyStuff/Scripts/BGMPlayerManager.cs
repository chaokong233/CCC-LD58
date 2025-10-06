using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BGMPlayerManager : MonoBehaviour
{
    private void Awake()
    {
        if(FindObjectsByType<BGMPlayerManager>(FindObjectsSortMode.None).Length > 1)
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);
    }
}
