sampler uImage0 : register(s0);
sampler uImage1 : register(s1); 
sampler uImage2 : register(s2);



float4 MainPS(float2 uv : TEXCOORD0) : COLOR0
{
    float4 color = tex2D(uImage0, uv);
    float4 maskSample = tex2D(uImage1, uv);
    if (maskSample.r <= 0.5f)
    {
        return tex2D(uImage2, uv);
    }
    return color;
    //return color;
}

technique Technique1
{
    pass VisionPass
    {
        PixelShader = compile ps_2_0 MainPS();
    }
}