using System.Collections;
using Mirror.BouncyCastle.Security;
using UnityEngine;
using UnityEngine.UI;

namespace Scripts.UI
{
    public class ImageAlphaChanger : MonoBehaviour
    {
        [SerializeField] private Image _image;
        [SerializeField] private float _start_time;
        [SerializeField] private float _anim_time;
        [SerializeField] private float _start_value;
        [SerializeField] private float _end_value;

        private void Awake()
        {
            StartCoroutine(ShowAnim());    
        }

        private IEnumerator ShowAnim()
        {
            if (_start_time > 0.1f)
                yield return new WaitForSeconds(_start_time);

            float t = 0;
            while (t < 1)
            {
                t += Time.deltaTime / _anim_time;
                Color color = _image.color;
                color.a = _start_value + (_end_value - _start_value) * t;
                _image.color = color;

                yield return null;
            }
        }
    }
}