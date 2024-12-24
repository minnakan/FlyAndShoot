using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Noise
{

    public enum NormalizeMode
    {
        Local,
        Global
    };
   public static float[,] GenerateNoiseMap(int mapWidth, int mapHeight,int seed, float scale, int octaves, float persistance, float lacunarity, Vector2 offset, NormalizeMode normalizeMode)
    {
        float[,] noiseMap = new float[mapWidth, mapHeight];

        System.Random rand = new System.Random(seed);
        Vector2[] octaveoffsets = new Vector2[octaves];

        float maxPossibleheight = 0;
        float amplitude = 1;
        float frequency = 1;

        for (int i = 0; i < octaves; i++)
        {
          float offsetX = rand.Next(-100000,100000) + offset.x;
          float offsetY = rand.Next(-100000, 100000)- offset.y;
          octaveoffsets[i] = new Vector2(offsetX, offsetY);

            maxPossibleheight += amplitude;
            amplitude *= persistance;
        }


        if(scale <=0)
        {
            scale = 0.0001f;
        }

        float maxNoise = float.MinValue;
        float minNoise = float.MaxValue;

        float halfWidth = mapWidth / 2f;
        float halfHeight = mapHeight / 2f;

        for(int y  = 0; y < mapHeight; y++)
        {
            for(int x = 0;x < mapWidth; x++)
            {
                amplitude = 1;
                frequency = 1;
                float noiseHeight = 0;

                for(int i =0; i<octaves; i++)
                {
                    float sampleX = (x - halfWidth + octaveoffsets[i].x) / scale * frequency ;
                    float sampleY = (y - halfHeight + octaveoffsets[i].y) / scale * frequency ;

                    //convert from 0-1 range to -1 to 1 range
                    float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 -1;

                    noiseHeight += perlinValue * amplitude;
                    
                    amplitude *= persistance;
                    frequency *= lacunarity;
                }

                noiseMap[x, y] = noiseHeight;

                if(noiseHeight > maxNoise)
                {
                    maxNoise = noiseHeight;
                }
                else if(noiseHeight< minNoise)
                {
                    minNoise = noiseHeight;
                }
            }
                
        }

        //normalise nosie values
        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                if (normalizeMode == NormalizeMode.Local)
                {
                    noiseMap[x, y] = Mathf.InverseLerp(minNoise, maxNoise, noiseMap[x, y]);
                }
                else
                {
                    float normalizedHeight = (noiseMap[x, y] +1)/(2f * maxPossibleheight/2.0f);
                    noiseMap[x, y] = Mathf.Clamp(normalizedHeight,0,int.MaxValue);
                }
                
            }
        }

        return noiseMap;
    }
}
