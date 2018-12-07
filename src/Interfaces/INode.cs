namespace ClassicUO.Interfaces
{
    public interface INode<T>
    {
        T Left { get; set; }
        T Right { get; set; }
    }
}