using UnityEngine;

public class InputManager : MonoBehaviour
{
    private static InputManager instance;

    private Controls controls;

    public static InputManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<InputManager>();

                if (instance == null)
                {
                    GameObject singletonObject = new GameObject(typeof(InputManager).Name);
                    instance = singletonObject.AddComponent<InputManager>();
                }
            }
            return instance;
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(this.gameObject);
            return;
        }

        controls = new Controls();

        //DontDestroyOnLoad(this.gameObject);
    }

    private void OnEnable()
    {
        controls.Enable();
    }

    private void OnDisable()
    {
        if (instance == this)
            controls.Disable();
    }

    private void OnDestroy()
    {
        instance = null;
    }

    public Controls GetControls()
    {
        return controls;
    }
}

