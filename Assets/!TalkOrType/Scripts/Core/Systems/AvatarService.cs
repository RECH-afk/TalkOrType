using Steamworks;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class AvatarService
{
    private Dictionary<SteamId, Texture2D> _cache = new();

    public async Task<Texture2D> GetAvatar(Friend player)
    {
        if (_cache.TryGetValue(player.Id, out var cached))
            return cached;

        var avatar = await player.GetLargeAvatarAsync();

        if (!avatar.HasValue)
            return null;

        var texture = ConvertToTexture(avatar.Value);

        _cache[player.Id] = texture;

        return texture;
    }

    private Texture2D ConvertToTexture(Steamworks.Data.Image image)
    {
        int width = (int)image.Width;
        int height = (int)image.Height;

        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);

        byte[] flipped = FlipY(image.Data, width, height);

        tex.LoadRawTextureData(flipped);
        tex.Apply();

        return tex;
    }

    private byte[] FlipY(byte[] data, int width, int height)
    {
        int rowSize = width * 4;
        byte[] flipped = new byte[data.Length];

        for (int y = 0; y < height; y++)
        {
            int srcIndex = y * rowSize;
            int dstIndex = (height - y - 1) * rowSize;

            System.Buffer.BlockCopy(data, srcIndex, flipped, dstIndex, rowSize);
        }

        return flipped;
    }
}