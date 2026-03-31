using UnityEngine;

namespace Scripts.UI
{
    public class ContextShower : ImageButtonInteractor
    {
        [SerializeField] private GameObject _context;

        protected override void Awake()
        {
            base.Awake();
            ProcessCursorExit();
        }

        protected override void ProcessLeftMouseClick()
        {
            Debug.Log("PERK LKM");
        }

        protected override void ProcessCursorEnter()
        {
            Debug.Log("PERK ENTER");
            _context.SetActive(true);
        }
        protected override void ProcessCursorExit()
        {
            Debug.Log("PERK EXIT");
            _context.SetActive(false);
        }
    }
}