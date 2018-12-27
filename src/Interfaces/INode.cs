namespace ClassicUO.Interfaces
{
    internal interface INode<T>
    {
        T Left { get; set; }
        T Right { get; set; }
    }
}