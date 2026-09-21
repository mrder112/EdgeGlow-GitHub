using System.Numerics;
using System.Runtime.InteropServices;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;
using Vortice.D3DCompiler;
namespace EdgeGlow;

internal sealed class Capture : IDisposable
{
    private ID3D11Device device=null!;
    private ID3D11DeviceContext context=null!;
    private IDXGIOutputDuplication duplication=null!;
    private ID3D11ComputeShader shader=null!;
    private ID3D11Texture2D small=null!,readback=null!,copy=null!;
    private ID3D11ShaderResourceView? sourceView;
    private ID3D11UnorderedAccessView outputView=null!;
    private ID3D11SamplerState sampler=null!;
    private ID3D11Buffer constants=null!;
    private readonly Display display;
    private int rotation;
    private readonly int samples;
    internal Vector3[][] Colors {get;}
    [StructLayout(LayoutKind.Sequential)] private struct Parameters { public Vector4 Crop; public Vector4 Info; }
    private const string Shader="""
        Texture2D<float4> Source : register(t0);
        RWTexture2D<float4> Result : register(u0);
        SamplerState Linear : register(s0);
        cbuffer Params : register(b0) { float4 Crop; float4 Info; }
        float2 rotateUV(float2 p) {
            if(Info.y==2) return float2(p.y,1-p.x);
            if(Info.y==3) return 1-p;
            if(Info.y==4) return float2(1-p.y,p.x);
            return p;
        }
        [numthreads(64,1,1)]
        void main(uint3 id:SV_DispatchThreadID) {
            if(id.x>=Info.z||id.y>=4)return;
            float3 color=0;
            // Stratified area sampling removes fine detail before the 256-element CPU blur.
            for(int a=0;a<8;a++) for(int d=0;d<16;d++) {
                float u=(id.x+(a+.5)/8.0)/Info.z;
                float v=(d+.5)/16.0*Info.x;
                float2 p=id.y==0?float2(u,v):id.y==1?float2(u,1-v):id.y==2?float2(v,u):float2(1-v,u);
                p=lerp(Crop.xy,Crop.zw,p);
                color+=Source.SampleLevel(Linear,rotateUV(p),0).rgb;
            }
            Result[id.xy]=float4(color/128,1);
        }
        """;
    internal Capture(Display display,int samples=256)
    {
        this.display=display;this.samples=samples;Colors=Enumerable.Range(0,4).Select(_=>new Vector3[samples]).ToArray();
        try { Initialize(); } catch {Dispose();throw;}
    }
    private void Initialize()
    {
        using var factory=DXGI.CreateDXGIFactory1<IDXGIFactory1>();
        for(uint ai=0;factory.EnumAdapters1(ai,out var adapter).Success;ai++)
        using(adapter)
        {
            for(uint oi=0;adapter.EnumOutputs(oi,out var output).Success;oi++)
            using(output)
            {
                if(output.Description.DeviceName!=display.Id)continue;
                using(var advanced=output.QueryInterfaceOrNull<IDXGIOutput6>())
                    if(advanced!=null&&(int)advanced.Description1.ColorSpace!=0)
                        throw new InvalidOperationException("Источник работает в HDR/расширенном цветовом пространстве. Переключите его в SDR.");
                D3D11.D3D11CreateDevice(adapter,DriverType.Unknown,DeviceCreationFlags.BgraSupport,[FeatureLevel.Level_11_0],out device,out context).CheckError();
                using var o1=output.QueryInterface<IDXGIOutput1>();
                duplication=o1.DuplicateOutput(device);
                rotation=(int)duplication.Description.Rotation;
                InitializePipeline();
                return;
            }
        }
        throw new InvalidOperationException("Монитор источника отключён или недоступен для DXGI.");
    }
    private void InitializePipeline()
    {
                shader=device.CreateComputeShader(Compiler.Compile(Shader,"main","EdgeStrip","cs_5_0",ShaderFlags.OptimizationLevel3).Span);
                small=device.CreateTexture2D(new Texture2DDescription(Format.R32G32B32A32_Float,(uint)samples,4,mipLevels:1,bindFlags:BindFlags.UnorderedAccess));
                readback=device.CreateTexture2D(new Texture2DDescription(Format.R32G32B32A32_Float,(uint)samples,4,mipLevels:1,bindFlags:BindFlags.None,usage:ResourceUsage.Staging,cpuAccessFlags:CpuAccessFlags.Read));
                outputView=device.CreateUnorderedAccessView(small);
                sampler=device.CreateSamplerState(new SamplerDescription(Filter.MinMagMipLinear,TextureAddressMode.Clamp,TextureAddressMode.Clamp,TextureAddressMode.Clamp));
                constants=device.CreateBuffer(new BufferDescription(32,BindFlags.ConstantBuffer,ResourceUsage.Default));
    }
    internal unsafe bool Read(Settings s)
    {
        if(s.CropLeft+s.CropRight>=display.Bounds.Width-2||s.CropTop+s.CropBottom>=display.Bounds.Height-2)
            throw new InvalidOperationException("Обрезка исключает всю область захвата. Уменьшите отступы.");
        var result=duplication.AcquireNextFrame(0,out _,out var resource);
        if(result.Code==unchecked((int)0x887A0027))return false; // timeout: keep last colors, not an error or a black scene.
        result.CheckError();
        try {
            using(resource)
            using(var texture=resource.QueryInterface<ID3D11Texture2D>())
                ProcessTexture(texture,s);
        } finally {duplication.ReleaseFrame().CheckError();}
        return true;
    }
    private unsafe void ProcessTexture(ID3D11Texture2D texture,Settings s)
    {
                var desc=texture.Description;
                if(copy==null||copy.Description.Width!=desc.Width||copy.Description.Height!=desc.Height){
                    sourceView?.Dispose();copy?.Dispose();
                    desc.BindFlags=BindFlags.ShaderResource;desc.Usage=ResourceUsage.Default;desc.CPUAccessFlags=CpuAccessFlags.None;desc.MiscFlags=ResourceOptionFlags.None;
                    copy=device.CreateTexture2D(desc);sourceView=device.CreateShaderResourceView(copy);
                }
                // GPU-to-GPU copy only. No full-size image is ever mapped onto the CPU.
                context.CopyResource(copy,texture);
                var p=new Parameters{Crop=new(s.CropLeft/(float)display.Bounds.Width,s.CropTop/(float)display.Bounds.Height,1-s.CropRight/(float)display.Bounds.Width,1-s.CropBottom/(float)display.Bounds.Height),Info=new(s.Strip,rotation,samples,0)};
                context.UpdateSubresource(in p,constants);
                context.CSSetShader(shader);context.CSSetConstantBuffer(0,constants);context.CSSetShaderResource(0,sourceView);context.CSSetSampler(0,sampler);context.CSSetUnorderedAccessView(0,outputView);
                context.Dispatch((uint)((samples+63)/64),4,1);
                context.CSSetShaderResource(0,null);context.CSSetUnorderedAccessView(0,null);
                context.CopyResource(readback,small);
                var mapped=context.Map(readback,0,MapMode.Read);
                try { for(int e=0;e<4;e++) { float* row=(float*)((byte*)mapped.DataPointer+e*mapped.RowPitch); for(int i=0;i<samples;i++)Colors[e][i]=new(row[i*4],row[i*4+1],row[i*4+2]); } }
                finally {context.Unmap(readback,0);}
    }
    // Test-only texture path exercises the SAME compiled shader and GPU readback, without capturing a desktop.
    internal Capture(int samples=256) {this.samples=samples;Colors=Enumerable.Range(0,4).Select(_=>new Vector3[samples]).ToArray();display=new Display("synthetic",new Rectangle(0,0,256,256));rotation=1;try {D3D11.D3D11CreateDevice(null,DriverType.Hardware,DeviceCreationFlags.BgraSupport,[FeatureLevel.Level_11_0],out device,out context).CheckError();InitializePipeline();}catch{Dispose();throw;}}
    internal unsafe Vector3[][] ProcessSynthetic(byte[] bgra,int rotate=1,Settings? settings=null)
    {
        if(bgra.Length!=256*256*4)throw new ArgumentException("Expected 256 × 256 BGRA");
        rotation=rotate;
        using var texture=device.CreateTexture2D(new Texture2DDescription(Format.B8G8R8A8_UNorm,256,256,mipLevels:1));
        fixed(byte* p=bgra)context.UpdateSubresource(texture,0,null,(nint)p,256*4,0);
        ProcessTexture(texture,settings??new Settings());return Colors;
    }
    public void Dispose(){context?.ClearState();sourceView?.Dispose();copy?.Dispose();constants?.Dispose();sampler?.Dispose();outputView?.Dispose();readback?.Dispose();small?.Dispose();shader?.Dispose();duplication?.Dispose();context?.Dispose();device?.Dispose();}
}
