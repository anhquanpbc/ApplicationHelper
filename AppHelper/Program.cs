
class Program
{
    static void Main(string[] args)
    {
        IDictionary<int, string> arguments = new Dictionary<int, string>();
        // Check if any arguments are passed
        if (args.Length == 0)
        {
            Console.WriteLine("No arguments provided.");
        }
        else
        {
            // Iterate through each argument
            for (int i = 0; i < args.Length; i++)
            {
                arguments.Add(i, args[i]);
            }

            foreach (var item in arguments)
            {
                Console.WriteLine($"{item.Key} - {item.Value}");
            }
        }
    }
}