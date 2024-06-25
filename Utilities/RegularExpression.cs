namespace web_groupware.Utilities
{
    public class RegularExpression
    {
        public const string EMAIL = @"^[a-zA-Z0-9_+-]+(.[a-zA-Z0-9_+-]+)*@([a-zA-Z0-9][a-zA-Z0-9-]*[a-zA-Z0-9]*\.)+[a-zA-Z]{2,}$";
        public const string POSTCODE = @"^[0-9]{3}-[0-9]{4}$|[0-9]{7}";
        public const string TEL = @"^[-0-9]+$";


        public RegularExpression()
        {

        }

    }
}
