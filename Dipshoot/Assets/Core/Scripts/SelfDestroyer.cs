using System.Collections;
using UnityEngine;

public class SelfDestroyer : MonoBehaviour
{
    [SerializeField] private float _lifetime = 10f;

    private Coroutine _destroy_coroutine;

    private void Start()
    {
        Restart();
    }

    public void Initialize(float lifetime)
    {
        _lifetime = lifetime;

        if (isActiveAndEnabled)
            Restart();
    }

    private void Restart()
    {
        if (_destroy_coroutine != null)
            StopCoroutine(_destroy_coroutine);

        _destroy_coroutine = StartCoroutine(DestroyYourself());
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
