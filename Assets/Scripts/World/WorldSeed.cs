using System;
using System.Globalization;
using System.Security.Cryptography;
namespace Aevoria.World
{
    public static class WorldSeed
    {
        public static string Resolve(string entered)
        {
            if(!string.IsNullOrWhiteSpace(entered))return entered;
            var bytes=new byte[8];using(var random=RandomNumberGenerator.Create())random.GetBytes(bytes);
            ulong value=BitConverter.ToUInt64(bytes,0)&0x7fffffffffffffffUL;
            return (value==0?1:value).ToString(CultureInfo.InvariantCulture);
        }
    }
}
