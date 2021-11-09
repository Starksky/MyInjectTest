using UnityEngine;

public class Test : MonoBehaviour
{
    [Inject] public ITest t;
    [InjectNew] public ITest t1;
    
    void Awake()
    {
        Debug.Log("Created Inject");
        Debug.Log(t.test);
        Debug.Log($"[Inject] {t.test1.test}");
        
        Debug.Log("");
        Debug.Log("Created InjectNew");
        t1.test = 8f;
        t1.test1.test = 4f;
        Debug.Log(t1.test);
        Debug.Log($"[Inject] {t1.test1.test}");
        
        Debug.Log("");
        Debug.Log("Check old Inject");
        Debug.Log(t.test);
        Debug.Log($"[Inject] {t.test1.test}");
    }

}
