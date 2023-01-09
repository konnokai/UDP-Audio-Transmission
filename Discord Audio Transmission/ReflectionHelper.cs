using System;
using System.Collections.Generic;
using System.Linq;

namespace Discord_Audio_Transmission.Utils
{
    static class ReflectionHelper
    {
        public static IEnumerable<T> CreateAllInstancesOf<T>()
        {
#pragma warning disable CS8619 // 值中參考型別的可 Null 性與目標型別不符合。
#pragma warning disable CS8600 // 正在將 Null 常值或可能的 Null 值轉換為不可為 Null 的型別。
            return typeof (ReflectionHelper).Assembly.GetTypes()
                .Where(t => typeof (T).IsAssignableFrom(t))
                .Where(t => !t.IsAbstract && t.IsClass)
                .Select(t => (T) Activator.CreateInstance(t));
#pragma warning restore CS8600 // 正在將 Null 常值或可能的 Null 值轉換為不可為 Null 的型別。
#pragma warning restore CS8619 // 值中參考型別的可 Null 性與目標型別不符合。
        }
    }
}
