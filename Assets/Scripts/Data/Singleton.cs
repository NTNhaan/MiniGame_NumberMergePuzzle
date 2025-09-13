using UnityEngine;
namespace Data
{
    public class Singleton<T> : MonoBehaviour where T : Component
    {
        private static T instance;
        [SerializeField] private bool dontDestroyOnLoad;

        public static T Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<T>();

                    if (instance == null)
                    {
                        GameObject existingController = GameObject.Find("Controller");
                        if (existingController != null)
                        {
                            instance = existingController.GetComponent<T>();
                        }
                        else
                        {
                            GameObject g = new GameObject("Controller");
                            instance = g.AddComponent<T>();
                        }
                    }
                }
                return instance;
            }
        }

        void Awake()
        {
            if (instance == null)
            {
                instance = this as T;

                if (dontDestroyOnLoad)
                {
                    if (transform.parent == null)
                    {
                        DontDestroyOnLoad(gameObject);
                    }
                }
            }
            else
            {
                if (instance != this)
                {
                    Destroy(gameObject);
                }
            }

            CustomAwake();
        }

        protected virtual void CustomAwake() { }

        protected virtual void OnDestroy()
        {
            instance = null;
        }
    }
}