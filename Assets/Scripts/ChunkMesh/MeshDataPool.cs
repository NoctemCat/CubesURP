using System.Collections.Concurrent;

public class MeshDataPool
{
    private readonly ConcurrentQueue<MeshDataHolder> _datas;

    public MeshDataPool()
    {
        _datas = new();
    }

    public void Dispose()
    {
        while (_datas.TryDequeue(out MeshDataHolder holder))
        {
            holder.Dispose();
        }
    }

    public void AddToPool(int add = 10)
    {
        for (int i = 0; i < add; i++)
            _datas.Enqueue(Construct());
    }

    private MeshDataHolder Construct()
    {
        MeshDataHolder holder = new();
        holder.Init();
        return holder;
    }

    public MeshDataHolder Get()
    {
        if (_datas.IsEmpty)
            AddToPool();

        if (_datas.TryDequeue(out MeshDataHolder data)) { return data; }
        return Get();
    }

    public void Reclaim(MeshDataHolder data)
    {
        _datas.Enqueue(data);
    }
}