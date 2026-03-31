using System.Collections;
using UnityEngine;

public class SelfDestroyer : MonoBehaviour
{
    [SerializeField] private float _lifetime = 10f;

    private void Awake()
    {
        StartCoroutine(DestroyYourself());
    }

    private IEnumerator DestroyYourself()
    {
        yield return new WaitForSeconds(_lifetime);
        Destroy(gameObject);
    }

    private void OnDisable()
    {
        Destroy(gameObject);
    }
}
