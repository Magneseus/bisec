public class ActiveNode<T>
{
    public ActiveNode<T> prevNode { get; set; }
    public ActiveNode<T> nextNode { get; set; }
    public ActiveNode<T> prevActiveNode { get; set; }
    public ActiveNode<T> nextActiveNode { get; set; }
    public T data { get; set; }
    public int activeIndex { get; set; }
    public bool isRootNode { get; set; }

    public ActiveNode()
    {

    }

    public ActiveNode(T _data)
    {
        data = _data;
    }
}
