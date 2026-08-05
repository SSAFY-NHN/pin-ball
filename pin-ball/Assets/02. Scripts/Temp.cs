using UnityEngine;

public class Temp : MonoBehaviour
{
    public ESceneName _sceneName;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        App.Get<SceneManager>().Load(_sceneName);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
