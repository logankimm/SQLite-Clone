namespace DatabaseCore

public interface IIndex<K, V>
{
    void Insert (K key, V value);
    tuple<K, V> Get (K key);
    // Find all instances where key is greater than or equal to
    IEnumerable<tuple<K, V>> LargerThanOrEqualTo(K key);
    IEnumerable<tuple<K, V>> LargerThan(K key);
    IEnumerable<tuple<K, V>> LessThanOrEqualTo(K key);
    IEnumerable<tuple<K, V>> LessThan(K key);
    bool Delete(K Key);
    // Delete all versions of a key when attached to a comparator
    bool Delete(K Key, IComparer<V> valueComparer = null);
}