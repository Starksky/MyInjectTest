public interface ITest1
{
    float test { get; set; }
}
    
public class TTTest1 : ITest1
{
    public float test { get; set; } = 5;
}