namespace SEA.Data
{
    using System;

    internal class UsersFiller : Filler
    {
        protected override object ProcessAttr(string Name, string Value)
        {
            if (Name.Equals("DisplayName", StringComparison.OrdinalIgnoreCase))
            {
                return Value.Trim();
            }
            else if (Name.Equals("Location", StringComparison.OrdinalIgnoreCase))
            {
                return Value.Trim();
            }
            else if (Name.Equals("AboutMe", StringComparison.OrdinalIgnoreCase))
            {
                return Value.Trim();
            }
            else
            {
                return base.ProcessAttr(Name, Value);
            }
        }
    }
}