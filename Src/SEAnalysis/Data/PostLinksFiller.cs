namespace SEA.Data
{
    using System;


    class PostLinksFiller : Filler
    {
        protected override object ProcessAttr(string Name, string Value)
        {
            if (Name.Equals("PostLinkTypeId", StringComparison.OrdinalIgnoreCase))
            {
                byte linkTypeId = byte.Parse(Value);

                if (linkTypeId == 1)
                {
                    return PostLinkType.Linked;
                }
                else if (linkTypeId == 3)
                {
                    return PostLinkType.Duplicate;
                }
                else
                {
                    throw new ArgumentException($"{nameof(Value)} must be 1 or 3.", nameof(Value));
                }
            }
            else
            {
                return base.ProcessAttr(Name, Value);
            }
        }
    }
}