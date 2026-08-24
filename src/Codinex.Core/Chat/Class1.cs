using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Codinex.Core.Chat
{
    public class Class1
    {

        public int FirstDuplicate(List<int> arr)
        {
           // Dictionary<int, int> dict = new Dictionary<int, int>();


           //HashSet<> 

           // for (int i = 0; i < arr.Count; i++)
           // {
           //     if (dict.ContainsKey(arr[i]))
           //         return arr[i];

           //     dict.Add(arr[i], i);
           // }


           // var numbers = new List<int> { 1, 2, 3 };
           // var selected = numbers.Select(x => x * numbers.Count);
           // numbers.Add(4);
           // Console.WriteLine(string.Join(", ", selected));

           // int.TryParse()

           // string text = "A";
           // Change(text);
           // Console.WriteLine(text);
           // double Change(out string value)
           // {
           //     value = "B";
           // }

           List<int> list = new List<int>(){1,2,3,4};

           list.Select(p => p > 2);
        }


        
    }

    public static class Extensions
    {
        public static IEnumerable<T> Select2<T, Tt>(this IEnumerable<T> list, Func<T, Tt> func)
        {
            List<T> result = new List<T>();
            
            foreach (var VARIABLE in list)
            {
                if(bool.Parse(func.Invoke(VARIABLE).ToString()))
                    result.Add(VARIABLE);
            }

            return result;
        }
    }
}
