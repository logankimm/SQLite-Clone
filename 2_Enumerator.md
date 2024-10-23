## Interfaces basics
- Can inherit from multiple inheritances, but all methods from each inheritance must be implemented within new interfaces

## IEnumerable vs IEnumerator
- Based off of collections: group of objects (can be more than one type and can grow dynamically)
### IEnumerable
- Constructing: IEnumerable<T> name = new IEnumerable<T>
- Constructing: IEnumerable name = new IEnumerable - Used for non-generic collections: stores more than one type of object
- Must have method: IEnumerable<T> GetEnumerator();
```
class DogShelter : IEnumerable<Dog>
{
    public List<Dog> = dogs;
    public DogShelter()
    {
        dogs = new List<Dog>() {
            new Dog("Casper"),
            new Dog("Kate"),
            new Dog("Sally"),
            new Dog("Bob"),
        };
    }
    IEnumerator<Dog> GetEnumerator()
    {
        return dogs.GetEnumerator()
    }
}
class Dog
// Why doesn't this code need to implment GetEnumerator for dogs? - oh becasue it's a list which already has it implemented
```
- Can declare a generic IEnumerable<T> name; - This can be used if the variable can be a list, queue, etc. 
- Uses foreach method while interating through collection of objects
### IEnumerator
- Uses MoveNext() to iterate through collection of objects
#### Difference Summary
- IEnumerator keeps track internally of current position of iterator while IEnumerable just loops through from start to finish
