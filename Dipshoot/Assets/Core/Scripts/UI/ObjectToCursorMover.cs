using UnityEngine;

namespace Scripts.UI
{
    /// <summary>
    /// Надёжный перемещатель UI-объекта по курсору для World Space Canvas.
    /// Вызывать StartDrag(obj) при начале удержания и StopDrag() при отпускании.
    /// </summary>
    public class ObjectToCursorMover : MonoBehaviour
    {
        [SerializeField] private Canvas canvas;           // World Space canvas
        [SerializeField] private float followSpeed = 20f; // <=0 instant, >0 lerp speed

        private Transform target;         // текущий объект, который двигаем
        private Vector3 dragOffset;       // offset = target.position - hitPointAtStart
        private Camera cam;               // камера, используемая для ScreenPoint->Ray
        private bool isDragging;

        private void Start()
        {
            if (canvas == null)
            {
                Debug.LogError("WorldSpaceDragFollower: Canvas is not assigned.");
                enabled = false;
                return;
            }

            // для World Space canvas обычно указывается canvas.worldCamera
            // если он не задан — пробуем Camera.main
            cam = canvas.worldCamera != null ? canvas.worldCamera : Camera.main;

            if (cam == null)
            {
                Debug.LogError("WorldSpaceDragFollower: No camera found (canvas.worldCamera == null and Camera.main == null).");
                enabled = false;
                return;
            }
        }

        private void Update()
        {
            if (!isDragging || target == null) return;

            // Построим луч под курсором
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);

            // Плоскость канваса: нормаль = canvas.transform.forward, проходит через canvas.transform.position
            Plane canvasPlane = new Plane(canvas.transform.forward, canvas.transform.position);

            if (canvasPlane.Raycast(ray, out float enter))
            {
                Vector3 hitPoint = ray.GetPoint(enter);
                Vector3 desired = hitPoint + dragOffset;

                if (followSpeed <= 0f)
                {
                    target.position = desired;
                }
                else
                {
                    target.position = Vector3.Lerp(target.position, desired, 1f - Mathf.Exp(-followSpeed * Time.deltaTime));
                }
            }
            else
            {
                // если луч не пересёк плоскость (маловероятно), ничего не делаем
            }
        }

        /// <summary>
        /// Начать перетаскивание указанного RectTransform.
        /// </summary>
        public void StartDrag(Transform rect)
        {
            if (rect == null) return;
            target = rect.transform;
            isDragging = true;

            // Посчитать initial offset: точка пересечения луча с плоскостью канваса
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            Plane canvasPlane = new Plane(canvas.transform.forward, canvas.transform.position);

            if (canvasPlane.Raycast(ray, out float enter))
            {
                Vector3 hitPoint = ray.GetPoint(enter);
                dragOffset = target.position - hitPoint;
            }
            else
            {
                // На случай, если не пересекается — просто сбросим offset в 0
                dragOffset = Vector3.zero;
            }
        }

        public void StopDrag()
        {
            isDragging = false;
            target = null;
        }
    }
}
