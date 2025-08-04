using System;

namespace DatabaseCore;

public class Test
{
    string message = "asdf";
    public Test()
    {
        Console.WriteLine("Test class has been constructed");
    }

    public void testing123()
    {
        Console.WriteLine(this.message);
    }
}