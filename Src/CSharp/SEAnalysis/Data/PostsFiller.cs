namespace SEA.Data
{
    using System;
    using System.Text;
    using System.Text.RegularExpressions;

    internal class PostsFiller : Filler
    {
        private Html2Text _conv;

        protected override object ProcessAttr(string Name, string Value)
        {
            if (Name.Equals("PostTypeId", StringComparison.OrdinalIgnoreCase))
            {
                byte postTypeId = byte.Parse(Value);

                if (postTypeId == 1)
                {
                    return PostType.Question;
                }
                else if (postTypeId == 2)
                {
                    return PostType.Answer;
                }
                else
                {
                    throw new ArgumentException($"{nameof(Value)} must be 1 or 2.", nameof(Value));
                }
            }
            else if (Name.Equals("Body", StringComparison.OrdinalIgnoreCase))
            {
                return Value.Trim();
            }
            else if (Name.Equals("Title", StringComparison.OrdinalIgnoreCase))
            {
                return Value.Trim();
            }
            else if (Name.Equals("Tags", StringComparison.OrdinalIgnoreCase))
            {
                var allMatches = Regex.Matches(Value, "<(.+?)>");

                int countLessChar = 0;

                for (int i = 0; i < Value.Length; i++)
                {
                    if (Value[i] == '<')
                    {
                        countLessChar++;
                    }
                }


                StringBuilder tags = new StringBuilder(Value.Length - countLessChar);

                int groupSize = 0;

                for (int i = 0; i < allMatches.Count; i++)
                {
                    groupSize = allMatches[i].Groups.Count;

                    if (groupSize > 1)
                    {
                        tags.Append(allMatches[i].Groups[groupSize - 1].Value);
                        tags.Append(";");
                    }
                }


                if(tags.Length > 1)
                {
                    // Исключается знак ';' в конце, потому что он не нужен.
                    return tags.ToString(0, tags.Length - 1);
                }
                else
                {
                    return "";
                }
                
            }
            else
            {
                return base.ProcessAttr(Name, Value);
            }
        }

        public PostsFiller()
        {
            _conv = new Html2Text();
        }
    }
}