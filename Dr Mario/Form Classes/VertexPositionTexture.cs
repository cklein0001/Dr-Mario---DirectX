using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SlimDX;
using SlimDX.Direct3D11;
using SlimDX.DXGI;

namespace Dr_Mario
{
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    struct VertexPositionTexture
    {
        public Vector4 Position;
        public Vector2 TexCoord;
        public static readonly InputElement[] inputElements = new[] {
                new InputElement("POSITION", 0, Format.R32G32B32A32_Float, 0, 0),
                new InputElement("TEXCOORD",0,Format.R32G32_Float, 16 ,0)
            };
        public static readonly int SizeInBytes = System.Runtime.InteropServices.Marshal.SizeOf(typeof(VertexPositionTexture));
        public VertexPositionTexture(Vector4 position, Vector2 texCoord)
        {
            Position = position;
            TexCoord = texCoord;
        }
        public VertexPositionTexture(Vector3 position, Vector2 texCoord)
        {
            Position = new Vector4(position, 1);
            TexCoord = texCoord;
        }
    }

}
