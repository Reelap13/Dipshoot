using System.Collections;
using UnityEngine;

namespace Scripts.UI
{
    public class ScaleChanger : MonoBehaviour
    {
        [SerializeField] private RectTransform _rect;
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
                float scale = _start_value + (_end_value - _start_value) * t;
                _rect.localScale = Vector3.one * scale;

                yield return null;
            }
        }
    }
}