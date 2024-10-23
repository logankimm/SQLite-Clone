public interface IIndex<K, V>
{
    void Insert (K key, V value);
    tuple<K, V> Get (K key);
}