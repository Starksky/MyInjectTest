using UnityEngine;

public class Test : MonoBehaviour
{
    [Inject] public ITest t;
    
    void Start()
    {
        Debug.Log(t.test);
        Debug.Log(t.test1.test);
    }

}
