public static class Extensions
{
    static public string EscapeGremlinString(this string input)
    {
        return input.Replace(@"'", @"\'");
    }
}