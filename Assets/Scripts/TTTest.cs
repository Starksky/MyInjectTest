using UnityEngine;

public interface ITest
{
    ITest1 test1 { get; set; }
    float test { get; set; }
}

public class TTTest : MonoBehaviour, ITest
{
    [Inject] public ITest1 test1 { get; set; }
    public float test { get; set; } = 3;
}