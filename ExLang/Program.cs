namespace ExLang
{
    class Token
    {

    }

    class Keyword : Token
    {

    }

    class Identifier : Token
    {

    }

    class Operator : Token
    {
        private Operator(string value) => Value = value;
        public string Value { get; }
        public static Operator Plus = new("+");
        public static Operator Minus = new("-");
        public static Operator Slash = new("/");
        public static Operator Star = new("*");
    }

    class Literal : Token
    {

    }

    class Delimeter : Token
    {

    }

    class Whitespace : Token
    {

    }

    class Comment : Token
    {

    }

    class Lexer(string text)
    {
        private readonly string text = text;
        private int position;

        private char Current
        {
            get
            {
                if (this.position >= this.text.Length)
                {
                    return '\0';
                }
                return this.text[this.position];
            }
        }
    }


    internal class Program
    {
        static void Main(string[] args)
        {

        }
    }
}
