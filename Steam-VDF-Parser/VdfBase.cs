using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VdfConverter
{
    public class VdfBase
    {
        protected bool IsCollection(Type type)
        {
            bool isCollection = type
                .GetInterfaces()
                .Any(x =>
                    x.IsGenericType &&
                    x.GetGenericTypeDefinition() == typeof(ICollection<>)
                 );

            return isCollection;
        }
    }
}
