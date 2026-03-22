sampler2D PrimaryTexture : register(s0);
sampler2D TileTargetTexture : register(s1);

float2 TileOffset;
float2 TileScale;
float2 PlayerScreenPos; 

float4 MainPS(float2 uv : TEXCOORD0) : COLOR0
{
    float4 screenColor = tex2D(PrimaryTexture, uv);
    
    float2 startUV = (PlayerScreenPos * TileScale) + TileOffset;
    float2 endUV = (uv * TileScale) + TileOffset;
    float2 diff = (endUV - startUV) / 5.0;
    
    float hit = 0;
    hit = max(hit, tex2D(TileTargetTexture, startUV).a);
    hit = max(hit, tex2D(TileTargetTexture, startUV + diff).a);
    hit = max(hit, tex2D(TileTargetTexture, startUV + diff * 2.0).a);
    hit = max(hit, tex2D(TileTargetTexture, startUV + diff * 3.0).a);
    hit = max(hit, tex2D(TileTargetTexture, startUV + diff * 4.0).a);
    hit = max(hit, tex2D(TileTargetTexture, endUV).a);

    float isBlocked = step(0.001, hit);
    
    float4 isolatedTile = tex2D(TileTargetTexture, endUV);
    float4 tintedColor = (isolatedTile.a > 0.1) ? (screenColor * float4(1, 0, 0, 1)) : screenColor;
    
    return lerp(tintedColor, float4(0, 0, 0, 1), isBlocked);
}
technique Technique1
{
    pass VisionPass
    {
        PixelShader = compile ps_2_0 MainPS();
    }
}