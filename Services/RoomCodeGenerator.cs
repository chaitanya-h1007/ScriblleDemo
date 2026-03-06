using Microsoft.Identity.Client;

namespace ScriblleDemo.Services
{
    public static class RoomCodeGenerator
    {
        public static string Generate()
        {
            const string code_Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890abcdefghijklmnopqrstuvwxyz";
            var random = new Random();
            return new string(Enumerable.Range(0, 6)
                .Select(_ => code_Chars[random.Next(code_Chars.Length)])
                .ToArray());
            
        }
    }
}
