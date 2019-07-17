using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiDARCupDetection
{
    public static class Extensions
    {
        public static string ToJSON<T>(this T obj, bool indented = true)
        {
            return JsonConvert.SerializeObject(obj, indented ? Formatting.Indented : Formatting.None);
        }

        public static T NotNull<T>(this T obj)
        {
            if (obj != null)
            {
                return obj;
            }

            throw new NullReferenceException("Object is null");
        }
    }
}
