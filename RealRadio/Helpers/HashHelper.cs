using System;
using HashUtility;
using UnityEngine;

namespace RealRadio.Helpers;

public static class HashHelper
{
    private const uint FNVOffsetBasis32 = 0x811C9DC5;
    private const uint FNVPrim32 = 0x01000193;

    public static ushort Get16BitHash(this AssetBundle assetBundle)
    {
        if (assetBundle == null)
            throw new ArgumentNullException(nameof(assetBundle));

        uint hash = FNVOffsetBasis32;

        unchecked
        {
            foreach (string assetName in assetBundle.GetAllAssetNames())
            {
                foreach (char c in assetName)
                {
                    hash ^= c;
                    hash *= FNVPrim32;
                }
            }
        }

        return (ushort)((hash >> 16) ^ (hash & 0xFFFF));
    }
}
