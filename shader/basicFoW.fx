sampler uImage0 : register(s0);
sampler uImage1 : register(s1); 

float uTime;
float2 uMaskSize;
float2 uScreenResolution;
bool BlackOut;


float4 MainPS(float2 uv : TEXCOORD0) : COLOR0
{
    float2 maskUV = (uv * (uScreenResolution / 16.0)) / uMaskSize;
    
    float4 maskSample = tex2D(uImage1, uv);
    bool isSolid = maskSample.rgb > 0.5f;

    float4 color = tex2D(uImage0, uv);
    
    //if (isSolid)
    //{//
    //    // Example: Tint solid tiles red
    //    
     //       maskSample.a = 0.5f;
      //      return maskSample;
      //  }
    
        return color * maskSample;
    return color;
}

technique Technique1
{
    pass VisionPass
    {
        PixelShader = compile ps_2_0 MainPS();
    }
}