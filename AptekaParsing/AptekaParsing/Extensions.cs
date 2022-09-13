using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptekaParsing
{
    public static partial class Extensions
    {
        public static Boolean IsDigit(this String s, Int32 index)
        {
            return Char.IsDigit(s, index);
        }

        public static int CountByCharacter(string word, char character) =>
            word.Count(x => x.Equals(character));
    }
}
